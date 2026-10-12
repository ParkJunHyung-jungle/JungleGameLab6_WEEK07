Shader "Keyboard/KeycapGlass"
{
    Properties
    {
        _Tint ("Glass Tint", Color) = (0.65, 0.88, 0.95, 1)
        _Opacity ("Center Opacity", Range(0, 1)) = 0.12
        _EdgeOpacity ("Edge Opacity", Range(0, 1)) = 0.7
        _Smoothness ("Smoothness", Range(0, 1)) = 0.92
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "Glass"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half _Opacity;
                half _EdgeOpacity;
                half _Smoothness;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half3 normalWS : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half3 normal = normalize(input.normalWS);
                half3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half fresnel = pow(1 - saturate(dot(normal, view)), 3);
                Light light = GetMainLight();
                half diffuse = saturate(dot(normal, light.direction));
                half3 halfway = normalize(light.direction + view);
                half specular = pow(saturate(dot(normal, halfway)), lerp(32, 256, _Smoothness));
                half3 reflection = GlossyEnvironmentReflection(reflect(-view, normal), 1 - _Smoothness, 1);
                half3 color = _Tint.rgb * (0.45 + diffuse * 0.3)
                    + reflection * (0.15 + fresnel * 0.65)
                    + light.color * specular * 1.5
                    + fresnel * half3(0.45, 0.6, 0.7);
                return half4(color, saturate(lerp(_Opacity, _EdgeOpacity, fresnel) + specular * 0.3));
            }
            ENDHLSL
        }
    }
}
