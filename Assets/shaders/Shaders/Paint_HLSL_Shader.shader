Shader "Custom/Paint_HLSL_Shader"
{
    Properties
    {
        _BrushUV ("Brush UV", Vector) = (0.5, 0.5, 0, 0)
        _BrushRadius ("Brush Radius", Float) = 0.05
        _BrushStrength ("Brush Strength", Float) = 0.1
        _BrushFalloff ("Brush Falloff", Range(0,1)) = 0.5
        _HeightSmoothness ("Height Smoothness", Range(0,1)) = 0.5
        _BrushColor ("Brush Color", Color) = (1,0,0,1)
        _Mode ("Mode", Float) = 0
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
            float _BrushFalloff;
            float _HeightSmoothness;
            float4 _BrushColor;
            float _Mode;
            float4 _MainTex_TexelSize;

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

            float BrushMask(float2 uv, float2 center, float radius, float falloff)
            {
                float d = distance(uv, center);
                float t = saturate(d / max(radius, 0.0001));

                // A falloff of 0 produces a hard edge, while 1 fades from the center.
                if (falloff <= 0.0001)
                {
                    return 1.0 - step(1.0, t);
                }

                // Use a gaussian-like profile so the mask looks like a soft airbrush:
                // bright center, smooth radial falloff, and a clean fade at the edge.
                float softness = lerp(48.0, 3.0, saturate(falloff));
                float radialFade = exp(-softness * t * t);
                float edgeFade = 1.0 - smoothstep(0.85, 1.0, t);
                return saturate(radialFade * edgeFade);
            }

            float SampleHeightAverage(float2 uv)
            {
                float2 texel = _MainTex_TexelSize.xy;

                float center = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).r;
                float left = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-texel.x, 0.0)).r;
                float right = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texel.x, 0.0)).r;
                float down = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0.0, -texel.y)).r;
                float up = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0.0, texel.y)).r;

                return (center * 4.0 + left + right + down + up) / 8.0;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                half4 current = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                float mask = BrushMask(uv, _BrushUV.xy, _BrushRadius, _BrushFalloff);
                float amount = saturate(mask * _BrushStrength);

                half4 result = current;

                if (_Mode < 0.5)
                {
                    float smoothed = lerp(current.r, SampleHeightAverage(uv), _HeightSmoothness);
                    float raised = saturate(smoothed + amount);
                    result.r = lerp(current.r, raised, mask);
                }
                else if (_Mode < 1.5)
                {
                    float smoothed = lerp(current.r, SampleHeightAverage(uv), _HeightSmoothness);
                    float cut = saturate(smoothed - amount);
                    result.r = lerp(current.r, cut, mask);
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
