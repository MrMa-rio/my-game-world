Shader "MyGameWorld/Procedural World/Nebula Field"
{
    Properties { _Visibility("Visibility", Range(0, 1)) = 0 }
    SubShader
    {
        Tags { "Queue"="Transparent-110" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend One One
        Cull Off ZWrite Off ZTest LEqual
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float _Visibility;
            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; float2 data : TEXCOORD1; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; float phase : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output; output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float2 corner = input.uv * 2.0 - 1.0; float2 pixelOffset = corner * input.data.y * 2.0 / _ScreenParams.xy;
                output.positionCS.xy += pixelOffset * output.positionCS.w;
                output.color = input.color; output.uv = input.uv; output.phase = input.data.x; return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2.0 - 1.0; float radial = saturate(1.0 - dot(p, p));
                float cloudA = sin(p.x * 5.2 + input.phase * 17.0) * sin(p.y * 4.1 - input.phase * 11.0);
                float cloudB = sin((p.x + p.y) * 9.3 + input.phase * 29.0);
                float cloudC = sin(p.x * 15.7 - p.y * 7.9 + input.phase * 41.0) * 0.5 + 0.5;
                float structure = saturate(0.50 + cloudA * 0.20 + cloudB * 0.11 + cloudC * 0.12);
                float mask = pow(radial, 3.1) * smoothstep(0.24, 0.76, structure) * _Visibility;
                half3 color = input.color.rgb * mask * input.color.a;
                return half4(color, mask);
            }
            ENDHLSL
        }
    }
}
