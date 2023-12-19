Shader "Custom/SnowShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            Cull Off
            Blend 0 SrcAlpha OneMinusSrcAlpha, Zero One
            ZWrite Off

            CGPROGRAM
            //#pragma enable_d3d11_debug_symbols
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                uint vertexID : SV_VertexID;
                uint instanceID : SV_InstanceID;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 uvAndIntensity : TEXCOORD0;
            };

            struct InstanceData
            {
                float3 posWS;
                float radius;
                float3 velWS;
                float intensity;
            };

            StructuredBuffer<InstanceData> _SnowInstanceData;

            UNITY_DECLARE_SHADOWMAP(_ShadowMapTexture);
            #define GET_CASCADE_WEIGHTS(wpos, z)    getCascadeWeights( wpos, z )
            #define GET_SHADOW_COORDINATES(wpos,cascadeWeights) getShadowCoord(wpos,cascadeWeights)

            inline fixed4 getCascadeWeights(float3 wpos, float z)
            {
                fixed4 zNear = float4( z >= _LightSplitsNear );
                fixed4 zFar = float4( z < _LightSplitsFar );
                fixed4 weights = zNear * zFar;
                return weights;
            }

            inline float4 getShadowCoord( float4 wpos, fixed4 cascadeWeights )
            {
                float3 sc0 = mul (unity_WorldToShadow[0], wpos).xyz;
                float3 sc1 = mul (unity_WorldToShadow[1], wpos).xyz;
                float3 sc2 = mul (unity_WorldToShadow[2], wpos).xyz;
                float3 sc3 = mul (unity_WorldToShadow[3], wpos).xyz;
                float4 shadowMapCoordinate = float4(sc0 * cascadeWeights[0] + sc1 * cascadeWeights[1] + sc2 * cascadeWeights[2] + sc3 * cascadeWeights[3], 1);
            #if defined(UNITY_REVERSED_Z)
                float  noCascadeWeights = 1 - dot(cascadeWeights, float4(1, 1, 1, 1));
                shadowMapCoordinate.z += noCascadeWeights;
            #endif
                return shadowMapCoordinate;
            }

            // 0 - 0,1
            // 1 - 0,0
            // 2 - 1,0
            // 3 - 1,1
            float4 GetQuadVertexPosition(uint vertexID, float z = UNITY_NEAR_CLIP_VALUE)
            {
                uint topBit = vertexID >> 1;
                uint botBit = (vertexID & 1);
                float x = topBit;
                float y = 1 - (topBit + botBit) & 1; // produces 1 for indices 0,3 and 0 for 1,2
                float4 pos = float4(x, y, z, 1.0);
                return pos;
            }

            v2f vert (appdata v)
            {
                v2f o;

                InstanceData data = _SnowInstanceData[v.instanceID];
                float3 posWS = data.posWS;

                // This handles both "real quad" and "2 triangles" cases: remaps {0, 1, 2, 3, 4, 5} into {0, 1, 2, 3, 0, 2}.
                uint quadIndex = (v.vertexID & 0x03) + (v.vertexID >> 2) * (v.vertexID & 0x01);
                float3 pp = GetQuadVertexPosition(quadIndex, 0.0).xyz;
                o.uvAndIntensity.xy = float2(pp.x, 1.0 - pp.y);

                pp.xy = pp.xy * 2.0 - 1.0;
                pp *= data.radius;
                //posWS+= mul(pp, float3x3(UNITY_MATRIX_V[0].xyz, UNITY_MATRIX_V[1].xyz, UNITY_MATRIX_V[2].xyz));
                posWS += pp.x * UNITY_MATRIX_V[0]
                       + pp.y * UNITY_MATRIX_V[1]
                       + pp.z * UNITY_MATRIX_V[2];
                o.vertex = mul(UNITY_MATRIX_VP, float4(posWS, 1.0));

                // Directional light cascade shadows modulate intensity.
                float3 vpos = mul(unity_WorldToCamera, float4(posWS, 1.0)).xyz;
                half4 cascadeWeights = GET_CASCADE_WEIGHTS(posWS, vpos.z);
                float4 shadowCoord = GET_SHADOW_COORDINATES(float4(posWS, 1.0f), cascadeWeights);
                half shadow = UNITY_SAMPLE_SHADOW(_ShadowMapTexture, shadowCoord);
                o.uvAndIntensity.z = data.intensity * lerp(0.3, 1.0, shadow);

                return o;
            }

            sampler2D _MainTex;
            float4 _Color;

            float4 _MainTex_ST;

            half4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uvAndIntensity.xy;
                float intensity = i.uvAndIntensity.z;

                float alpha = tex2D(_MainTex, uv).r;
                fixed4 col = half4(_Color.rgb, alpha * intensity);
                return col;
            }

            ENDCG
        }
    }
}
