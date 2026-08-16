Shader "MyGameWorld/Procedural World/Celestial Sky"
{
    Properties
    {
        _Exposure("Exposure", Range(0, 4)) = 1
        [HideInInspector] _CelestialSunDirection("Sun Direction", Vector) = (0, 1, 0, 0)
        [HideInInspector] _CelestialMoonDirection("Moon Direction", Vector) = (0, -1, 0, 0)
        [HideInInspector] _CelestialTime("Time Weights", Vector) = (1, 0, 0, 0)
        [HideInInspector] _CelestialDayColor("Day Color", Color) = (0.28, 0.62, 0.94, 1)
        [HideInInspector] _CelestialNightColor("Night Color", Color) = (0.055, 0.095, 0.24, 1)
        [HideInInspector] _CelestialHorizonColor("Horizon Color", Color) = (0.08, 0.12, 0.22, 1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float _Exposure; float4 _CelestialSunDirection; float4 _CelestialMoonDirection;
            float4 _CelestialTime; float4 _CelestialDayColor; float4 _CelestialNightColor; float4 _CelestialHorizonColor;
            struct Attributes { float4 positionOS : POSITION; }; struct Varyings { float4 positionCS : SV_POSITION; float3 direction : TEXCOORD0; };
            Varyings Vert(Attributes input) { Varyings o; o.positionCS = TransformObjectToHClip(input.positionOS.xyz); o.direction = normalize(input.positionOS.xyz); return o; }
            half4 Frag(Varyings input) : SV_Target
            {
                float3 d = normalize(input.direction); float horizon = saturate(1.0 - abs(d.y) * 3.2);
                float3 sky = lerp(_CelestialNightColor.rgb, _CelestialDayColor.rgb, _CelestialTime.x);
                sky = lerp(sky, _CelestialHorizonColor.rgb, horizon * (0.34 + 0.42 * max(_CelestialTime.z, _CelestialTime.w)));
                float sunDot = dot(d, normalize(_CelestialSunDirection.xyz));
                float sunDisk = smoothstep(0.9991, 0.99965, sunDot); float sunGlow = pow(saturate(sunDot), 180.0);
                sky += float3(1.0, 0.63, 0.22) * (sunDisk * 2.4 + sunGlow * 0.65) * _CelestialTime.x;
                float moonDot = dot(d, normalize(_CelestialMoonDirection.xyz));
                float moonDisk = smoothstep(0.9986, 0.99935, moonDot);
                float moonCut = smoothstep(0.9983, 0.99925, dot(d, normalize(_CelestialMoonDirection.xyz + float3(0.018, 0.01, 0))));
                sky += float3(0.68, 0.78, 1.0) * saturate(moonDisk - moonCut * 0.72) * 2.2 * _CelestialTime.y;
                return half4(sky * _Exposure, 1);
            }
            ENDHLSL
        }
    }
}
