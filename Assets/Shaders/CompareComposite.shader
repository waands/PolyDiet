Shader "Hidden/CompareComposite"
{
    Properties
    {
        // UI espera essa prop mesmo que a gente não use.
        [NoScaleOffset]_MainTex("UI MainTex (unused)", 2D) = "white" {}
        _TexA("Tex A", 2D) = "white" {}
        _TexB("Tex B", 2D) = "black" {}
        _Split("Split", Range(0,1)) = 0.5
        _Feather("Feather", Range(0,0.05)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_TexA); SAMPLER(sampler_TexA);
            TEXTURE2D(_TexB); SAMPLER(sampler_TexB);
            float _Split;
            float _Feather;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f     { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };

            v2f vert(appdata v){ v2f o; o.pos = TransformObjectToHClip(v.vertex.xyz); o.uv = v.uv; o.color = v.color; return o; }

            half4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                half4 a = SAMPLE_TEXTURE2D(_TexA, sampler_TexA, uv);
                half4 b = SAMPLE_TEXTURE2D(_TexB, sampler_TexB, uv);

                if (_Feather > 0)
                {
                    float edge = smoothstep(_Split - _Feather, _Split + _Feather, uv.x);
                    return lerp(a, b, edge) * i.color;
                }
                return (uv.x <= _Split ? a : b) * i.color;
            }
            ENDHLSL
        }
    }
}