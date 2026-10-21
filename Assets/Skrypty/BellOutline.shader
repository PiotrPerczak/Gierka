Shader "Interaction/BellOutline"
{
    Properties
    {
        _OutlineColor ("Color", Color) = (1,1,1,0.65)
        _OutlineWidth ("Width in world units", Float) = 0.012
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            float4 Vert(Attributes input) : SV_POSITION
            {
                float3 position = TransformObjectToWorld(input.positionOS.xyz);
                float3 normal = TransformObjectToWorldNormal(input.normalOS);
                return TransformWorldToHClip(position + normal * _OutlineWidth);
            }
            half4 Frag() : SV_Target { return _OutlineColor; }
            ENDHLSL
        }
    }
}
