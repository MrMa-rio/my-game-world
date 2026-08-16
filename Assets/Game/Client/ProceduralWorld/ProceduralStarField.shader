Shader "MyGameWorld/Procedural World/Star Field"
{
    Properties { _Brightness("Brightness", Range(0, 8)) = 2.4 }
    SubShader
    {
        // Skybox is drawn after opaque/background geometry in URP. Stars must be
        // transparent so they compose after the sky while terrain depth occludes them.
        Tags { "Queue"="Transparent-100" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend One One
        Cull Off ZWrite Off ZTest LEqual
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float _Brightness;
            float4 _StarFieldVisibility; // x: reveal, y: magnitude cutoff, z: density threshold
            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; float2 data : TEXCOORD1; float2 density : TEXCOORD2; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; float phase : TEXCOORD1; float densityRank : TEXCOORD2; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float2 corner = input.uv * 2.0 - 1.0;
                float2 pixelOffset = corner * input.data.y * 2.0 / _ScreenParams.xy;
                output.positionCS.xy += pixelOffset * output.positionCS.w;
                output.color = input.color; output.uv = input.uv; output.phase = input.data.x; output.densityRank = input.density.x;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 centered = input.uv * 2.0 - 1.0;
                float starShape = saturate(1.0 - dot(centered, centered));
                starShape = pow(starShape, 3.2);
                float magnitudeVisibility = smoothstep(_StarFieldVisibility.y, _StarFieldVisibility.y + 0.075, input.color.a);
                float densityVisibility = 1.0 - step(_StarFieldVisibility.z, input.densityRank);
                float twinkle = 0.84 + 0.16 * sin(_Time.y * (0.7 + input.phase * 1.6) + input.phase * 37.0);
                half3 color = input.color.rgb * starShape * magnitudeVisibility * densityVisibility * twinkle * _Brightness;
                return half4(color, starShape * magnitudeVisibility * densityVisibility);
            }
            ENDHLSL
        }
    }
}
