Shader "Keyboard/NeonBackgroundBand"
{
    Properties
    {
        _PinkColor ("Left Color", Color) = (1, 0.045, 0.58, 1)
        _PurpleColor ("Left Center Color", Color) = (0.47, 0.08, 1, 1)
        _CyanColor ("Right Center Color", Color) = (0.035, 0.72, 1, 1)
        _MintColor ("Right Color", Color) = (0.04, 1, 0.68, 1)
        _GradientStart ("Gradient Start", Float) = 0
        _GradientEnd ("Gradient End", Float) = 18.5
        [HideInInspector] _UseKeyboardSpace ("Use Keyboard Space", Float) = 0
        _GlowStrength ("Glow Strength", Range(0, 5)) = 1.3
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "NeonBand"
            Tags { "LightMode"="UniversalForward" }
            ZWrite On
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _PinkColor;
                half4 _PurpleColor;
                half4 _CyanColor;
                half4 _MintColor;
                float _GradientStart;
                float _GradientEnd;
                float _UseKeyboardSpace;
                half _GlowStrength;
                float4x4 _KeyboardWorldToLocal;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float gradientPosition : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float keyboardX = mul(_KeyboardWorldToLocal, float4(positionWS, 1)).x;
                output.gradientPosition = input.positionOS.x;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float amount = saturate((input.gradientPosition - _GradientStart) / max(_GradientEnd - _GradientStart, 0.0001)) * 3;
                half3 color = amount < 1 ? lerp(_PinkColor.rgb, _PurpleColor.rgb, amount)
                    : amount < 2 ? lerp(_PurpleColor.rgb, _CyanColor.rgb, amount - 1)
                    : lerp(_CyanColor.rgb, _MintColor.rgb, amount - 2);
                return half4(color * _GlowStrength, 1);
            }
            ENDHLSL
        }
    }
}
