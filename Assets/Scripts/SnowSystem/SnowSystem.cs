using UnityEngine;
using UnityEngine.Rendering;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

// TODO improve: snow spawn box mechanism not great

//[ExecuteAlways]
[DisallowMultipleComponent, ImageEffectAllowedInSceneView] // ImageEffectAllowedInSceneView: call OnPostRender() even when the scene camera is selected!
[RequireComponent(typeof(Camera))]
public class SnowSystem : MonoBehaviour
{
    public GameObject trackingTarget;
    public Material snowMat;

    // This singleton is needed to circumvent IJob limitations.
    private static SnowSystem s_Instance;

    private CommandBuffer commandBuffer;
    private ComputeBuffer instanceBuffer;
    public int maxInstanceCount = 1024*8;
    public float flakePerSecond = 900.0f;
    // A flake fall speed multiplier.
    public float flakeSpeed = 0.5f;
    // Flakes are spawn above the target at a specific height.
    private float ceilingRelativeToTarget = 7.0f;
    // Flakes are always killed if they reach this world space height.
    public float absoluteFloor = 0.0f;
    // Extents of the spawn box in X an Z axis.
    public float spawnBoxExtent = 15.0f;
    // Minimum flake radius in world unit.
    private float flakeMinRadius = 0.01f;
    // Maximum flake radius in world unit.
    private float flakeMaxRadius = 0.06f;
    // Precalculation for 1.0f / (flakeMaxRadius - flakeMinRadius);
    private float flakeRadiusRangeInv;
    // A multiplier for the tubulence vector
    private float turbulenceStrength = 0.8f;

    // Turbulences are currently not animated.
    // This is a wrapping 3D grid.
    private Vector3[] turbulences;
    private int turbulenceWidth = 16;
    private int turbulenceHeight = 16;
    private int turbulenceDepth = 16;
    // Precalculation for turbulenceWidth * turbulenceHeight.
    private int turbulenceStride;

    private Snowflake[] snowflakes;
    // We must hold these it in order to run raycast queries asynchronously.
    private NativeArray<RaycastCommand> flakeQueries;
    private NativeArray<RaycastHit> flakeResults;
    // Sync this job handle to wait for the completion of the snowflake simulation.
    private JobHandle simJobHandle;

    // Number of live snowflake instances.
    // This is a floating point value so we can accurately track how many discreet instances are spawn per second.
    private float instanceCountf = 0.0f;

    // Random seed generator.
    Unity.Mathematics.Random random;

    private struct Snowflake
    {
        public Vector3 posWS;
        public float radius;
        public Vector3 velWS;
        public float extinctLife;
        public int raycastResultIndex;

        public bool isAlive { get { return radius > 0.0f; } }
        public bool isExtinct { get { return extinctLife > 0.0f; } }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct InstanceData
    {
        public Vector3 posWS;
        public float radius;
        public Vector3 velWS;
        public float intensity;
    }
    private const int kInstanceDataSize = 32;

    // Start is called before the first frame update
    void Start()
    {
        #if UNITY_EDITOR
        // Ignore scene camera.
        if (GetComponent<Camera>()?.gameObject.scene.name == null)
            return;
        #endif
        
        s_Instance = this;

        flakeRadiusRangeInv = 1.0f / (flakeMaxRadius - flakeMinRadius);
        turbulenceStride = turbulenceWidth * turbulenceHeight;

        random = new Unity.Mathematics.Random((uint)533723);

        turbulences = new Vector3[turbulenceWidth * turbulenceHeight * turbulenceDepth];
        InitTurbulence();

        snowflakes = new Snowflake[maxInstanceCount];
        for (int i = 0; i < maxInstanceCount; ++i)
            snowflakes[i].radius = -1.0f;

        instanceBuffer = new ComputeBuffer(maxInstanceCount, kInstanceDataSize, ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);
        commandBuffer = new CommandBuffer();
        commandBuffer.name = "SnowSystem commandBuffer";

    }

    void OnDestroy()
    {
        #if UNITY_EDITOR
        // Ignore scene camera.
        if (GetComponent<Camera>()?.gameObject.scene.name == null)
            return;
        #endif

        if (commandBuffer != null)
        {
            commandBuffer.Release();
            commandBuffer = null;
        }

        if (instanceBuffer != null)
        {
            instanceBuffer.Release();
            instanceBuffer = null;
        }

        turbulences = null;
        snowflakes = null;
    }

    // Update is called once per frame
    void Update()
    {
        #if UNITY_EDITOR
        // Ignore scene camera.
        if (GetComponent<Camera>()?.gameObject.scene.name == null)
            return;
        #endif

        float deltaTime = Time.deltaTime;
        JobHandle handleSpawnFlakes = MakeSpawnFlakeJob(deltaTime);
        // Must sync because next task PrepareFlakeRaycastJob=>RaycastCommand.Schedule() cannot be called from worker thread :/
        handleSpawnFlakes.Complete();
        JobHandle handlePrepareRaycasts = MakeFlakeRaycastJob(deltaTime, handleSpawnFlakes);
        JobHandle handleUpdateFlakes = MakeUpdateFlakeJob(deltaTime, handlePrepareRaycasts);
        JobHandle handleClearFlakes = MakeClearFlakeJob(handleUpdateFlakes);
    
        // We will sync this job as late as possible.
        simJobHandle = handleClearFlakes;
    }

    void LateUpdate()
    {
        #if UNITY_EDITOR
        // Ignore scene camera.
        if (GetComponent<Camera>()?.gameObject.scene.name == null)
            return;
        #endif

        // Normally, we should call this in OnPostRender() but the later will not be called if the camera is not selected.
        simJobHandle.Complete();
        if (flakeQueries.IsCreated)
            flakeQueries.Dispose();
        if (flakeResults.IsCreated)
            flakeResults.Dispose();
    }

    // Must be attached to a Camera. Unity will not call the method otherwise.
    // This is only called for the active camera.
    public void OnPostRender()
    {
        SnowSystem instance = this;
        #if UNITY_EDITOR
        // Scene camera render the main camera snow!!!
        if (GetComponent<Camera>()?.gameObject.scene.name == null)
            instance = s_Instance;
        #endif

        instance.RenderSnow();
    }

    private void RenderSnow()
    {
        const float kMaxIntensity = 5.0f;
        int instanceCount = (int)instanceCountf;

        // Fill GPU instancing data.
        // Normally we should double-buffer the compute buffer... but who cares!
        NativeArray<InstanceData> instData = instanceBuffer.BeginWrite<InstanceData>(0, instanceCount);
        for (int i = 0; i < instanceCount; ++i)
        {
            ref Snowflake flake = ref snowflakes[i];

            InstanceData data;
            data.posWS = flake.posWS;
            data.radius = flake.radius;
            data.velWS = flake.velWS;
            // make smaller flake brighter.
            float intensity = Mathf.Lerp(1.0f, 0.1f, (flake.radius - flakeMinRadius) * flakeRadiusRangeInv);
            intensity = intensity * intensity * kMaxIntensity;
            data.intensity = flake.extinctLife <= 0.0f ? intensity : Mathf.Clamp(flake.extinctLife, 0.0f, 1.0f) * intensity;
            instData[i] = data;
        }
        instanceBuffer.EndWrite<InstanceData>(instanceCount);

        commandBuffer.Clear();
        // TODO quad support
        commandBuffer.SetGlobalBuffer("_SnowInstanceData", instanceBuffer);
        commandBuffer.DrawProcedural(Matrix4x4.identity, snowMat, 0, MeshTopology.Triangles, 6, instanceCount);

        Graphics.ExecuteCommandBuffer(commandBuffer);
    }

    private void InitTurbulence()
    {
        // Turbulence vectors always point down as to avoid snowflakes clumping in particular spots.
        for (int k = 0; k < turbulenceDepth; ++k)
        for (int j = 0; j < turbulenceHeight; ++j)
        for (int i = 0; i < turbulenceWidth; ++i)
        {
            Vector3 t;
            t.x = random.NextFloat(-1.0f, 1.0f);
            t.y = random.NextFloat(-1.0f, 0.0f);
            t.z = random.NextFloat(-1.0f, 1.0f);
            SetTurbulence(i, j, k, t);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetTurbulence(int i, int j, int k, Vector3 t)
    {
        turbulences[i + j*turbulenceWidth + k*turbulenceStride] = t;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Vector3 GetTurbulence(int i, int j, int k)
    {
        return turbulences[i + j*turbulenceWidth + k*turbulenceStride];
    }

    private Vector3 SampleTurbulence(Vector3 posWS)
    {
        // Sample 8 values and trilinearly interpolate.

        // Technically there is a symetry around (0,0,0) ... who cares?!
        float i = Math.Abs(posWS.x * (turbulenceWidth - 1.0f) / (float)turbulenceWidth);
        float j = Math.Abs(posWS.y * (turbulenceHeight - 1.0f) / (float)turbulenceHeight);
        float k = Math.Abs(posWS.z * (turbulenceDepth - 1.0f) / (float)turbulenceDepth);
        int i0 = (int)i % turbulenceWidth;
        int j0 = (int)j % turbulenceHeight;
        int k0 = (int)k % turbulenceDepth;
        int i1 = (i0 + 1) % turbulenceWidth;
        int j1 = (j0 + 1) % turbulenceHeight;
        int k1 = (k0 + 1) % turbulenceDepth;
        float dx = i - i0;
        float dy = j - j0;
        float dz = k - k0;

        Vector3 t000 = GetTurbulence(i0, j0, k0);
        Vector3 t100 = GetTurbulence(i1, j0, k0);
        Vector3 t010 = GetTurbulence(i0, j1, k0);
        Vector3 t110 = GetTurbulence(i1, j1, k0);
        Vector3 t001 = GetTurbulence(i0, j0, k1);
        Vector3 t101 = GetTurbulence(i1, j0, k1);
        Vector3 t011 = GetTurbulence(i0, j1, k1);
        Vector3 t111 = GetTurbulence(i1, j1, k1);

        Vector3 A = Vector3.Lerp(t000, t100, dx);
        Vector3 B = Vector3.Lerp(t010, t110, dx);
        Vector3 C = Vector3.Lerp(t001, t101, dx);
        Vector3 D = Vector3.Lerp(t011, t111, dx);

        Vector3 E = Vector3.Lerp(A, B, dy);
        Vector3 F = Vector3.Lerp(C, D, dy);

        return Vector3.Lerp(E, F, dz);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Snowflake SpawnFlake(Vector3 posWS)
    {
        Snowflake flake;
        flake.posWS = posWS;
        flake.posWS.x += random.NextFloat(-spawnBoxExtent, spawnBoxExtent);
        flake.posWS.y += ceilingRelativeToTarget + random.NextFloat(-1.0f, -0.1f);
        flake.posWS.z += random.NextFloat(-spawnBoxExtent, spawnBoxExtent);
        flake.radius = random.NextFloat(flakeMinRadius, flakeMaxRadius); // > 0 means it is alive
        float speed = flakeSpeed * random.NextFloat(0.8f, 1.2f) * Mathf.Lerp(0.1f, 1.0f, (flake.radius - flakeMinRadius) * flakeRadiusRangeInv);
        Vector3 velWS = new Vector3(random.NextFloat(-0.5f, 0.5f), -1.0f, random.NextFloat(-0.5f, 0.5f));
        velWS.Normalize();
        velWS *= 9.8f * speed;
        flake.velWS = velWS;
        flake.extinctLife = -1.0f; // <=0 means it is not extincting
        flake.raycastResultIndex = -1;
        return flake;
    }

    private void SpawnFlakes(float deltaTime, Vector3 posWS)
    {
        if (instanceCountf >= maxInstanceCount)
            return;

        // Spawn new instances
        float newInstanceCountf = Math.Min(instanceCountf + flakePerSecond * deltaTime, (float)maxInstanceCount);
        int newInstanceCount = (int)newInstanceCountf;

        // Spawn around this area.
        for (int i = (int)instanceCountf; i < newInstanceCount; ++i)
            snowflakes[i] = SpawnFlake(posWS);

        instanceCountf = newInstanceCountf;
    }

    private void UpdateFlakes(float deltaTime, int beginInstance, int endInstance, NativeArray<RaycastHit> raycastResults)
    {
        int instanceCount = (int)instanceCountf;
        for (int i = beginInstance; i < endInstance; ++i)
        {
            ref Snowflake flake = ref snowflakes[i];

            if (!flake.isExtinct)
            {
                // Add turbulence to the flake motion
                Vector3 t = SampleTurbulence(flake.posWS);

                flake.posWS += deltaTime * (flake.velWS + t * turbulenceStrength);

                if (flake.posWS.y < absoluteFloor)
                {
                    flake.extinctLife = random.NextFloat(2.5f, 5.0f);
                    continue;
                }

                float velLength = flake.velWS.magnitude;
                if (raycastResults[ flake.raycastResultIndex ].distance > 0)
                {
                    flake.extinctLife = random.NextFloat(1.5f, 3.0f);
                    continue;
                }
            }
            else
            {
                flake.extinctLife -= deltaTime;
                if (flake.extinctLife <= 0.0f)
                    flake.radius = -1.0f; // kill the flake
            }
        }
    }
    private void ClearFlakes()
    {
        int instanceCount = (int)instanceCountf;

        // Cleanup dead instances.
        for (int i = 0; i < instanceCount;)
        {
            ref Snowflake flake = ref snowflakes[i];
            if (flake.radius <= 0.0f)
            {
                snowflakes[i] = snowflakes[instanceCount - 1];
                --instanceCount;
                instanceCountf -= 1.0f;
            }
            else
                ++i;
        }
    }

    private JobHandle MakeSpawnFlakeJob(float deltaTime)
    {
        SpawnFlakeJob job = new SpawnFlakeJob();
        job.deltaTime = deltaTime;
        job.posWS = trackingTarget.transform.localToWorldMatrix.GetColumn(3);
        return job.Schedule();
    }

    private JobHandle MakeFlakeRaycastJob(float deltaTime, JobHandle dependency)
    {
        int raycastCount = 0;
        int instanceCount = (int)instanceCountf;

        // Because RaycastCommand.ScheduleBatch() does not accept NativeSlice or NativeArray range,
        // we must temporarily accumulate the raycast commands in a temporary array.
        Span<RaycastCommand> queries = stackalloc RaycastCommand[maxInstanceCount];

        for (int i = 0; i < instanceCount; ++i)
        {
            ref Snowflake flake = ref snowflakes[i];

            if (flake.isAlive && !flake.isExtinct)
            {
                float velLength = flake.velWS.magnitude;
                Vector3 n = flake.velWS / velLength; // Bug UUM-41893" we shouldn't need to normalize
                // *1.5 to make it less likely to miss collision with terrain
                #pragma warning disable 0618
                queries[raycastCount] = new RaycastCommand(flake.posWS, n, velLength * 1.5f * deltaTime);
                #pragma warning restore 0618
                flake.raycastResultIndex = raycastCount++;
            }
            float ppp = flake.posWS.y;
        }

        flakeQueries = new NativeArray<RaycastCommand>(raycastCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        flakeResults = new NativeArray<RaycastHit>(raycastCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        for (int i = 0; i < raycastCount; ++i)
            flakeQueries[i] = queries[i];

        return RaycastCommand.ScheduleBatch(flakeQueries, flakeResults, 100, dependency);
    }

    private JobHandle MakeUpdateFlakeJob(float deltaTime, JobHandle dependency)
    {
        const int kJobGranularity = 100;

        int instanceCount = (int)instanceCountf;
        int subUpdateFlakeJobCount = (instanceCount + kJobGranularity - 1) / kJobGranularity;
        NativeArray<JobHandle> handleSubUpdateFlakes = new NativeArray<JobHandle>(subUpdateFlakeJobCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        for (int i = 0, j = 0; i < instanceCount; i += kJobGranularity)
        {
            UpdateFlakeJob job = new UpdateFlakeJob();
            job.deltaTime = deltaTime;
            job.beginInstance = i;
            job.endInstance = Math.Min(i + kJobGranularity, instanceCount);
            job.raycastResults = flakeResults;
            handleSubUpdateFlakes[j++] = job.Schedule(dependency);
        }
        JobHandle handleUpdateFlakes = JobHandle.CombineDependencies(handleSubUpdateFlakes);
        handleSubUpdateFlakes.Dispose();
        return handleUpdateFlakes;
    }

    private JobHandle MakeClearFlakeJob(JobHandle dependency)
    {
        ClearFlakeJob job = new ClearFlakeJob();
        return job.Schedule(dependency);
    }

    public struct SpawnFlakeJob : IJob
    {
        public float deltaTime;
        public Vector3 posWS;

        public void Execute()
        {
            SnowSystem.s_Instance.SpawnFlakes(deltaTime, posWS);
        }
    }

    public struct UpdateFlakeJob : IJob
    {
        public float deltaTime;
        public int beginInstance;
        public int endInstance;
        [NativeDisableContainerSafetyRestriction] public NativeArray<RaycastHit> raycastResults;

        public void Execute()
        {
            SnowSystem.s_Instance.UpdateFlakes(deltaTime, beginInstance, endInstance, raycastResults);
        }
    }

    public struct ClearFlakeJob : IJob
    {
        public void Execute()
        {
            SnowSystem.s_Instance.ClearFlakes();
        }
    }

}
