Shader "Custom/RenderGrass_HeightPaintBlend"
{
    Properties
    {
        _LengthMap ("Length Map", 2D) = "black" {}
        _ColorMap ("Color Map", 2D) = "white" {}
        _LengthMulty ("Length Multiplier", Float) = 10
        _BaseTint ("Base Tint", Color) = (1,1,1,1)
        _HeightLowColor ("Height Low Color", Color) = (0.08,0.12,0.22,1)
        _HeightHighColor ("Height High Color", Color) = (1,0.86,0.24,1)
        _PaintBlendStrength ("Paint Blend Strength", Range(0,1)) = 0.85
        _NormalStrength ("Normal Strength", Float) = 0.13
        _AmbientStrength ("Ambient Strength", Float) = 1
        _Smoothness ("Smoothness", Range(0,1)) = 0.18
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_LengthMap);
            SAMPLER(sampler_LengthMap);
            TEXTURE2D(_ColorMap);
            SAMPLER(sampler_ColorMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _LengthMap_ST;
                float4 _LengthMap_TexelSize;
                float4 _ColorMap_ST;
                float _LengthMulty;
                float4 _BaseTint;
                float4 _HeightLowColor;
                float4 _HeightHighColor;
                float _PaintBlendStrength;
                float _NormalStrength;
                float _AmbientStrength;
                float _Smoothness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };

            float SampleHeight(float2 uv)
            {
                return SAMPLE_TEXTURE2D_LOD(_LengthMap, sampler_LengthMap, uv, 0).r;
            }

            float3 HeightNormalOS(float2 uv)
            {
                float2 texel = _LengthMap_TexelSize.xy;
                float left = SampleHeight(uv + float2(-texel.x, 0.0));
                float right = SampleHeight(uv + float2(texel.x, 0.0));
                float down = SampleHeight(uv + float2(0.0, -texel.y));
                float up = SampleHeight(uv + float2(0.0, texel.y));
                float strength = _NormalStrength * max(_LengthMulty, 0.0);

                return normalize(float3((left - right) * strength, 1.0, (down - up) * strength));
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float2 uv = TRANSFORM_TEX(IN.uv, _LengthMap);
                float height = SampleHeight(uv) * max(_LengthMulty, 0.0);
                float3 positionOS = IN.positionOS.xyz + float3(0.0, height, 0.0);

                OUT.positionWS = TransformObjectToWorld(positionOS);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                OUT.uv = IN.uv;
                OUT.normalWS = TransformObjectToWorldNormal(HeightNormalOS(uv));
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 lengthUV = TRANSFORM_TEX(IN.uv, _LengthMap);
                float2 colorUV = TRANSFORM_TEX(IN.uv, _ColorMap);
                float height = SampleHeight(lengthUV);

                float3 normalWS = normalize(IN.normalWS);
                Light mainLight = GetMainLight();
                float diffuse = saturate(dot(normalWS, mainLight.direction));
                float3 ambient = SampleSH(normalWS) * _AmbientStrength;
                float3 lighting = ambient + mainLight.color * diffuse;

                float4 paintSample = SAMPLE_TEXTURE2D(_ColorMap, sampler_ColorMap, colorUV);
                float3 paintedColor = paintSample.rgb * _BaseTint.rgb;
                float3 heightColor = lerp(_HeightLowColor.rgb, _HeightHighColor.rgb, saturate(height));
                float paintBlend = saturate(paintSample.a * _PaintBlendStrength);
                float3 surfaceColor = lerp(heightColor, paintedColor, paintBlend);

                return half4(surfaceColor * max(lighting, 0.2), 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
