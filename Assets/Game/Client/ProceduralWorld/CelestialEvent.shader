Shader "MyGameWorld/Procedural World/Celestial Event"
{
    Properties { _BaseColor("Color", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One ZWrite Off Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 _BaseColor; struct A { float4 positionOS:POSITION; float4 color:COLOR; }; struct V { float4 positionCS:SV_POSITION; float4 color:COLOR; };
            V Vert(A i) { V o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.color=i.color*_BaseColor; return o; }
            half4 Frag(V i):SV_Target { return i.color; }
            ENDHLSL
        }
    }
}
