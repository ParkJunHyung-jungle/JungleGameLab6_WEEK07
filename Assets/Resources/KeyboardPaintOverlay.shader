Shader "Keyboard/Paint Overlay"
{
    Properties
    {
        [MainTexture] _PaintMap("Paint", 2D) = "black" {}
        [PerRendererData] _PaintBoundsMin("Bounds Min", Vector) = (0, 0, 0, 0)
        [PerRendererData] _PaintBoundsSize("Bounds Size", Vector) = (1, 1, 1, 0)
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent" }
        Pass
        {
            Name "PaintOverlay"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_PaintMap);
            SAMPLER(sampler_PaintMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _PaintBoundsMin;
                float4 _PaintBoundsSize;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 normalOS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionOS = input.positionOS.xyz;
                output.normalOS = input.normalOS;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz + input.normalOS * 0.0015);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normalized = saturate((input.positionOS - _PaintBoundsMin.xyz) / max(_PaintBoundsSize.xyz, 0.0001));
                float3 normal = normalize(input.normalOS);
                float3 axis = abs(normal);
                int face;
                float2 faceUV;
                if (axis.x >= axis.y && axis.x >= axis.z)
                {
                    face = normal.x >= 0 ? 0 : 1;
                    faceUV = normalized.zy;
                }
                else if (axis.y >= axis.z)
                {
                    face = normal.y >= 0 ? 2 : 3;
                    faceUV = normalized.xz;
                }
                else
                {
                    face = normal.z >= 0 ? 4 : 5;
                    faceUV = normalized.xy;
                }
                float2 uv = (float2(face % 3, face / 3) + faceUV) / float2(3, 2);
                half4 color = SAMPLE_TEXTURE2D(_PaintMap, sampler_PaintMap, uv);
                clip(color.a - 0.01);
                return color;
            }
            ENDHLSL
        }
    }
}
