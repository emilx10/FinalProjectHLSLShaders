Shader "HairChallenge/ChallengeMapPainter"
{
    Properties
    {
        _BrushUV ("Brush UV", Vector) = (0.5, 0.5, 0, 0)
        _BrushRadius ("Brush Radius", Float) = 0.05
        _BrushStrength ("Brush Strength", Float) = 0.1
        _BrushFalloff ("Brush Falloff", Range(0,1)) = 0.5
        _HeightSmoothness ("Height Smoothness", Range(0,1)) = 0.5
        _HeightStepLimit ("Height Step Limit", Range(0.001,1)) = 0.08
        _BrushColor ("Brush Color", Color) = (1,0,0,1)
        _SecondaryBrushColor ("Secondary Brush Color", Color) = (1,1,1,1)
        _PaintBlendMode ("Paint Blend Mode", Float) = 0
        _PatternScale ("Pattern Scale", Range(2,40)) = 16
        _PatternSoftness ("Pattern Softness", Range(0.001,0.45)) = 0.08
        _OmbreAngle ("Ombre Angle", Range(0,360)) = 0
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
            float _HeightStepLimit;
            float4 _BrushColor;
            float4 _SecondaryBrushColor;
            float _PaintBlendMode;
            float _PatternScale;
            float _PatternSoftness;
            float _OmbreAngle;
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
                float insideBrush = 1.0 - step(1.0, t);
                float pencilFalloff = saturate(1.0 - t);
                return insideBrush * lerp(1.0, pencilFalloff, saturate(falloff));
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

            void SampleHeightNeighborhood(float2 uv, out float average, out float minValue, out float maxValue)
            {
                float2 texel = _MainTex_TexelSize.xy;

                float center = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).r;
                float left = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-texel.x, 0.0)).r;
                float right = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texel.x, 0.0)).r;
                float down = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0.0, -texel.y)).r;
                float up = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0.0, texel.y)).r;
                float downLeft = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-texel.x, -texel.y)).r;
                float downRight = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texel.x, -texel.y)).r;
                float upLeft = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-texel.x, texel.y)).r;
                float upRight = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texel.x, texel.y)).r;

                average = (center * 4.0 + left + right + down + up + downLeft + downRight + upLeft + upRight) / 12.0;
                minValue = min(min(min(left, right), min(down, up)), min(min(downLeft, downRight), min(upLeft, upRight)));
                maxValue = max(max(max(left, right), max(down, up)), max(max(downLeft, downRight), max(upLeft, upRight)));
            }

            float StableHeightPaint(float currentHeight, float2 uv, float amount, float mask, bool grow)
            {
                float neighborhoodAverage;
                float neighborhoodMin;
                float neighborhoodMax;
                SampleHeightNeighborhood(uv, neighborhoodAverage, neighborhoodMin, neighborhoodMax);

                float rawTarget = grow
                    ? saturate(currentHeight + amount)
                    : saturate(currentHeight - amount);

                float smoothness = saturate(_HeightSmoothness);
                float relaxedBase = lerp(currentHeight, neighborhoodAverage, smoothness * mask);
                float relaxedTarget = grow
                    ? saturate(relaxedBase + amount)
                    : saturate(relaxedBase - amount);

                float target = lerp(rawTarget, relaxedTarget, smoothness);
                float stepLimit = max(_HeightStepLimit, amount * 1.5 + 0.01);
                float minAllowed = max(0.0, neighborhoodMin - stepLimit);
                float maxAllowed = min(1.0, neighborhoodMax + stepLimit);

                return clamp(target, minAllowed, maxAllowed);
            }

            float StripeMask(float2 uv)
            {
                float stripe = abs(frac((uv.x + uv.y) * _PatternScale) - 0.5) * 2.0;
                return smoothstep(0.5 - _PatternSoftness, 0.5 + _PatternSoftness, stripe);
            }

            float OmbreMask(float2 uv)
            {
                float angle = radians(_OmbreAngle);
                float2 direction = float2(cos(angle), sin(angle));
                return saturate(dot(uv - 0.5, direction) + 0.5);
            }

            float3 PatternPaintColor(float2 uv)
            {
                if (_PaintBlendMode < 0.5)
                {
                    return _BrushColor.rgb;
                }

                if (_PaintBlendMode < 1.5)
                {
                    return lerp(_BrushColor.rgb, _SecondaryBrushColor.rgb, StripeMask(uv));
                }

                return lerp(_SecondaryBrushColor.rgb, _BrushColor.rgb, OmbreMask(uv));
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
                    float raised = StableHeightPaint(current.r, uv, amount, mask, true);
                    result.r = lerp(current.r, raised, mask);
                }
                else if (_Mode < 1.5)
                {
                    float cut = StableHeightPaint(current.r, uv, amount, mask, false);
                    result.r = lerp(current.r, cut, mask);
                }
                else
                {
                    result.rgb = lerp(current.rgb, PatternPaintColor(uv), amount);
                    result.a = saturate(current.a + amount);
                }

                return result;
            }
            ENDHLSL
        }
    }
}
