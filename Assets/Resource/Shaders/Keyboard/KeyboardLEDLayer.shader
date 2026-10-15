Shader "Keyboard/LED Layer"
{
    Properties
    {
        [HDR] _LEDColor ("LED Color", Color) = (0, 0.8, 1, 1)
        _Intensity ("Intensity", Float) = 3
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "LED"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _LEDColor;
                float _Intensity;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 edge = abs(input.uv * 2 - 1);
                half glow = 1 - smoothstep(0.72, 1.0, max(edge.x, edge.y));
                return half4(_LEDColor.rgb * _Intensity, _LEDColor.a * glow);
            }
            ENDHLSL
        }
    }
}
