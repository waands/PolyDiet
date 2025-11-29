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
        _SideBySide("SideBySide Mode", Float) = 0
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
            float _SideBySide;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f     { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };

            v2f vert(appdata v){ v2f o; o.pos = TransformObjectToHClip(v.vertex.xyz); o.uv = v.uv; o.color = v.color; return o; }

            half4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // Remap UVs when in side-by-side mode so each texture fills its half
                float2 uvA = uv;
                float2 uvB = uv;
                if (_SideBySide > 0.5)
                {
                    float split = max(_Split, 0.0001);
                    float invSplit = max(1.0 - _Split, 0.0001);
                    if (uv.x <= _Split)
                        uvA.x = uv.x / split;
                    else
                        uvB.x = (uv.x - _Split) / invSplit;
                }

                half4 a = SAMPLE_TEXTURE2D(_TexA, sampler_TexA, uvA);
                half4 b = SAMPLE_TEXTURE2D(_TexB, sampler_TexB, uvB);

                if (_SideBySide > 0.5)
                {
                    return (uv.x <= _Split ? a : b) * i.color;
                }

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
