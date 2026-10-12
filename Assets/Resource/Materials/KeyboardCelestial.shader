Shader "Keyboard/Celestial"
{
    Properties
    {
        [MainColor] _BaseColor ("Tint", Color) = (1,1,1,1)
        _SpaceColor ("Deep Space", Color) = (0.002,0.004,0.015,1)
        _BlueColor ("Blue Nebula", Color) = (0.025,0.18,0.4,1)
        _VioletColor ("Violet Nebula", Color) = (0.24,0.035,0.38,1)
        _StarColor ("Star Light", Color) = (1,0.86,0.5,1)
        _NebulaScale ("Nebula Scale", Range(0.1,8)) = 1.8
        _FlowSpeed ("Flow Speed", Range(0,1)) = 0.12
        _StarDensity ("Star Density", Range(2,30)) = 9
        _Glow ("Glow", Range(0,3)) = 0.5
        _RimStrength ("Edge Light", Range(0,2)) = 0.22
        _ParallaxDepth ("Inner Space Depth", Range(0,2)) = 0.65
        _DepthContrast ("Depth Contrast", Range(0,2)) = 1.1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            ZWrite On
            ZTest LEqual
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor, _SpaceColor, _BlueColor, _VioletColor, _StarColor;
                float _NebulaScale, _FlowSpeed, _StarDensity, _Glow, _RimStrength;
                float _ParallaxDepth, _DepthContrast;
            CBUFFER_END
            struct Input { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Output { float4 positionCS : SV_POSITION; float3 positionOS : TEXCOORD0; float3 normalWS : TEXCOORD1; float3 positionWS : TEXCOORD2; float3 normalOS : TEXCOORD3; };
            float Hash3(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }
            float Noise3(float3 p)
            {
                float3 i = floor(p), f = frac(p);
                f = f * f * (3 - 2 * f);
                float a = lerp(lerp(Hash3(i), Hash3(i + float3(1,0,0)), f.x), lerp(Hash3(i + float3(0,1,0)), Hash3(i + float3(1,1,0)), f.x), f.y);
                float b = lerp(lerp(Hash3(i + float3(0,0,1)), Hash3(i + float3(1,0,1)), f.x), lerp(Hash3(i + float3(0,1,1)), Hash3(i + 1), f.x), f.y);
                return lerp(a, b, f.z);
            }
            float Cloud3(float3 p)
            {
                return Noise3(p) * 0.6 + Noise3(p * 2.03 + 13.7) * 0.28 + Noise3(p * 4.11 + 7.4) * 0.12;
            }
            float Hash(float2 p)
            {
                float3 q = frac(float3(p.xyx) * 0.1031);
                q += dot(q, q.yzx + 33.33);
                return frac((q.x + q.y) * q.z);
            }
            float Stars(float2 uv, float density, float time)
            {
                float2 cell = uv * density;
                float2 id = floor(cell);
                float seed = Hash(id);
                float2 center = float2(Hash(id + 19.3), Hash(id + 37.1)) * 0.6 + 0.2;
                float2 d = frac(cell) - center;
                float pixelWidth = max(length(fwidth(cell)), 0.015);
                float radius = 0.09;
                float variance = max(radius * radius, pixelWidth * pixelWidth * 0.2);
                float starCore = exp(-dot(d,d) / variance) * saturate(radius * radius / variance);
                float glimmer = exp(-length(d) * 20) * 0.14;
                float twinkle = 0.7 + 0.3 * sin(time * 1.6 + seed * 31);
                return (starCore + glimmer) * step(0.965, seed) * twinkle;
            }
            Output Vert(Input input)
            {
                Output output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionOS = input.positionOS.xyz;
                output.normalOS = input.normalOS;
                return output;
            }
            half4 Frag(Output input) : SV_Target
            {
                float time = _Time.y * _FlowSpeed;
                half3 normal = normalize(input.normalWS);
                half3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float3 viewOS = normalize(mul((float3x3)GetWorldToObjectMatrix(), view));
                float3 normalOS = normalize(input.normalOS);
                float3 weights = pow(abs(normalOS), 4);
                weights /= max(weights.x + weights.y + weights.z, 0.0001);
                float incidence = max(abs(dot(normalOS, viewOS)), 0.3);
                half3 space = _SpaceColor.rgb;
                float transmission = 1;
                [unroll]
                for (int layer = 0; layer < 4; layer++)
                {
                    float depth = (layer + 1) * 0.25 * _ParallaxDepth;
                    float3 samplePosition = input.positionOS - viewOS * depth / incidence;
                    float3 p = samplePosition * _NebulaScale + float3(3.7, 8.3, 1.9);
                    float warp = Noise3(p * 0.7 + float3(time, 1.7, -time * 0.5));
                    float cloud = Cloud3(p + warp * 1.7 + float3(time * 0.2, 0, -time * 0.15));
                    float veil = smoothstep(0.34, 0.76, cloud);
                    float colorMix = Noise3(p * 0.9 + float3(8.4, 2.1, 5));
                    half3 nebula = lerp(_BlueColor.rgb, _VioletColor.rgb, smoothstep(0.25, 0.72, colorMix));
                    float density = _StarDensity * (1 + layer * 0.2);
                    float3 starPosition = samplePosition + layer * 19.7;
                    float star = Stars(starPosition.yz, density, _Time.y) * weights.x
                        + Stars(starPosition.xz, density, _Time.y) * weights.y
                        + Stars(starPosition.xy, density, _Time.y) * weights.z;
                    float attenuation = exp(-depth * _DepthContrast);
                    space += nebula * veil * transmission * attenuation * (0.45 + _Glow);
                    space += _StarColor.rgb * star * transmission * attenuation * (1.8 + _Glow);
                    transmission *= 1 - veil * 0.32;
                }
                Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float diffuse = saturate(dot(normal, light.direction));
                float rim = pow(1 - saturate(dot(normal, view)), 3);
                float specular = pow(saturate(dot(normal, normalize(light.direction + view))), 48) * 0.28;
                half3 color = space * (0.8 + diffuse * 0.2 * light.shadowAttenuation);
                color += lerp(_BlueColor.rgb, _StarColor.rgb, 0.25) * rim * _RimStrength;
                color += light.color * specular;
                return half4(color * _BaseColor.rgb, 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            float3 _LightPosition;
            struct Input { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            float4 ShadowVert(Input input) : SV_POSITION
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 direction = normalize(_LightPosition - positionWS);
                #else
                    float3 direction = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, direction));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE * positionCS.w);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE * positionCS.w);
                #endif
                return positionCS;
            }
            half4 ShadowFrag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}







