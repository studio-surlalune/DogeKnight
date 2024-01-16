using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class DynamicResController : MonoBehaviour
{
    /// Exponential Moving Average (EMA) on the last 8 frames.
    private const int kEMAFrameCount = 8;
    private const int kTileSize = 32;
    /// Target frame time in milliseconds.
    private static float targetFrameTimeMS = GlobalConfig.TargetFrameTimeMS;
    /// delta time to wiggle around in milliseconds.
    private static float targetFrameDeltaMS = GlobalConfig.TargetFrameDeltaMS;
    /// EMA gpu time (without present).
    private static float emaGpuTimeMS;
    /// We may skip some frame to decide dynamic resolution in order to slow-down the logic.
    private static int skipFrameCounter = 0;
    /// Dynamic resolution width in pixel. Always a multiple of 32.
    /// To simplify calculation, it is not clamped to the display buffer min/max resolutions.
    private static int scaledWidth;
    /// Dynamic resolution height in pixel. Always a multiple of 32.
    /// To simplify calculation, it is not clamped to the display buffer min/max resolutions.
    private static int scaledHeight;
    /// Avoid dynamic allocation every frame.
    private static FrameTiming[] timings = new FrameTiming[1];

    // Start is called before the first frame update
    void Start()
    {
        emaGpuTimeMS = targetFrameTimeMS;
        scaledWidth = ((Screen.width + kTileSize - 1) / kTileSize) * kTileSize;
        scaledHeight = ((Screen.height + kTileSize - 1) / kTileSize) * kTileSize;
    }

    // Update is called once per frame
    void Update()
    {
        if (skipFrameCounter > 0)
        {
            --skipFrameCounter;
            return;
        }

        int width = Screen.width;
        int height = Screen.height;

        if (FrameTimingManager.GetLatestTimings(1, timings) == 1
         // some platforms may give garbage times at start-up...
         && timings[0].gpuFrameTime >= 0.0 && timings[0].gpuFrameTime <= 1000.0)
        {
            float k = 2.0f / (kEMAFrameCount + 1.0f);
            emaGpuTimeMS = (float)timings[0].gpuFrameTime*k + emaGpuTimeMS*(1-k);

            if (emaGpuTimeMS > targetFrameTimeMS + 3.0f * targetFrameDeltaMS)
            {
                scaledWidth -= kTileSize;
                scaledHeight -= kTileSize;
                skipFrameCounter = 0;
            }
            else if (emaGpuTimeMS > targetFrameTimeMS + targetFrameDeltaMS)
            {
                scaledWidth -= kTileSize;
                scaledHeight -= kTileSize;
                skipFrameCounter = 3;
            }
            else if (emaGpuTimeMS < targetFrameTimeMS - targetFrameDeltaMS)
            {
                scaledWidth += kTileSize;
                scaledHeight += kTileSize;
                skipFrameCounter = 3;
            }
            else if (emaGpuTimeMS < targetFrameTimeMS - 3.0f * targetFrameDeltaMS)
            {
                scaledWidth += kTileSize;
                scaledHeight += kTileSize;
                skipFrameCounter = 0;
            }
        }
        
        float w = Mathf.Clamp(scaledWidth, width * 0.5f, width * 1.0f);
        float h = Mathf.Clamp(scaledHeight, height * 0.5f, height * 1.0f);

        ScalableBufferManager.ResizeBuffers(w/width, h/height);
        //Debug.Log($"scaledWidth={scaledWidth} scaledHeight={scaledHeight} ema_gpuTime={emaGpuTimeMS}\n");
    }
}
