Shader "Keyboard/ScrewPositionMarker"
{
    Properties
    {
        _Color ("Color", Color) = (1,0.65,0.2,0.8)
        _BorderWidth ("Border Width Pixels", Float) = 0
        _MarkerCenter ("Marker Center", Vector) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Overlay" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _BorderWidth;
                float4 _MarkerCenter;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float4 centerCS = TransformWorldToHClip(_MarkerCenter.xyz);
                float2 radial = (output.positionCS.xy / max(output.positionCS.w, 0.0001)
                    - centerCS.xy / max(centerCS.w, 0.0001)) * _ScreenParams.xy;
                float2 direction = radial / max(length(radial), 0.0001);
                output.positionCS.xy += direction * (_BorderWidth * 2.0 / _ScreenParams.xy) * output.positionCS.w;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target { return _Color; }
            ENDHLSL
        }
    }
}
