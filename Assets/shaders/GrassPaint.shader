Shader "Custom/GrassPaint"
{
    Properties
    {
        _BrushUV ("Brush UV", Vector) = (0.5, 0.5, 0, 0)
        _BrushRadius ("Brush Radius", Float) = 0.05
        _BrushStrength ("Brush Strength", Float) = 0.1
        _BrushColor ("Brush Color", Color) = (1,0,0,1)
        _Mode ("Mode", Float) = 0
        _Hardness ("Brush Hardness", Range(0,1)) = 0.5
        _MainTex ("MainTex", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _BrushUV;
            float _BrushRadius;
            float _BrushStrength;
            float4 _BrushColor;
            float _Mode;
            float _Hardness;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float BrushMask(float2 uv, float2 center, float radius, float hardness)
            {
                float d = distance(uv, center);
                float t = saturate(d / max(radius, 0.0001));
                return 1.0 - smoothstep(hardness, 1.0, t);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                half4 current = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                float mask = BrushMask(uv, _BrushUV.xy, _BrushRadius, _Hardness);
                float amount = saturate(mask * _BrushStrength);

                half4 result = current;

                if (_Mode < 0.5)
                {
                    result.r = saturate(current.r + amount);
                }
                else if (_Mode < 1.5)
                {
                    result.r = saturate(current.r - amount);
                }
                else
                {
                    result.rgb = lerp(current.rgb, _BrushColor.rgb, amount);
                }

                return result;
            }
            ENDHLSL
        }
    }
}