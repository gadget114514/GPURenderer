Shader "Instanced/Lit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        [ToggleUI]_AlphaClip("Alpha Clipping", Int) = 0
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5

    }
    SubShader
    {
        Tags {
            "RenderType"="TransparentCutout"
            "Queue"="AlphaTest"
            "RenderPipeline"="UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl" 

            int _AlphaClip;
            half _Cutoff;
            float4 _Color;

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            CBUFFER_END

            StructuredBuffer<float4x4> transformBuffer;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL; 
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float fogFactor: TEXCOORD1;
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                float3 position : TEXCOORD2;
            };


            v2f vert (appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;

                float4x4 t = transformBuffer[instanceID];
                float4 worldPosition = mul(t, v.vertex); 

                o.vertex = mul(UNITY_MATRIX_VP, worldPosition);

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.fogFactor = ComputeFogFactor(o.vertex.z);
                o.normal = normalize(mul((float3x3)t, v.normal));
                o.position = mul(unity_ObjectToWorld, v.vertex).xyz; 
                return o;
            }

            half3 GetAmbientLight(half3 normal)
            {
                half3 ambientLight = dot(normal, unity_SHAr) + dot(normal, unity_SHAg) + dot(normal, unity_SHAb);
                return ambientLight;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                col *= _Color;

                if(_AlphaClip == 1){
                    clip(col.a - _Cutoff);
                }

                Light light = GetMainLight();

                float t = dot(i.normal, light.direction);
                t = max(0, t);
                t = min(t, 1);
                float3 diffuseLight = light.color  * t;

                float3 additionalLights = float3(0, 0, 0);

                #if defined(_ADDITIONAL_LIGHTS)
                    uint additionalLightCount = GetAdditionalLightsCount();
                    for (uint lightIndex = 0; lightIndex < additionalLightCount; ++lightIndex)
                    {
                        Light additionalLight = GetAdditionalLight(lightIndex, i.position);
                        float t = dot(i.normal, additionalLight.direction);
                        t = max(0, t);
                        additionalLights += additionalLight.color * t;
                    }
                    diffuseLight += additionalLights; 
                #endif

                half3 ambient = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);

                col.rgb *= (diffuseLight + ambient);

                col.rgb = MixFog(col.rgb, i.fogFactor);

                return col;
            }

            ENDHLSL
        }
    }
}
