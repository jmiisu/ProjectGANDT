Shader "Hidden/GANDT/SobelOutlineFullScreen"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _Threshold ("Edge Threshold", Range(0, 1)) = 0.15
        _Thickness ("Thickness", Range(0.5, 5)) = 1.0
        _EdgeIntensity ("Edge Intensity", Range(0, 5)) = 1.0
        _Blend ("Blend", Range(0, 1)) = 1.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderPipeline" = "UniversalPipeline" 
            "RenderType" = "Opaque" 
        }

        ZWrite Off
        Cull Off
        ZTest Always

        Pass
        {
            Name "SobelOutlineFullScreen"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _OutlineColor;
            float _Threshold;
            float _Thickness;
            float _EdgeIntensity;
            float _Blend;

            float GANDT_Luminance(float3 color)
            {
                return dot(color, float3(0.299, 0.587, 0.114));
            }

            float SampleLuminance(float2 uv, float2 offset)
            {
                float2 texelSize = _BlitTexture_TexelSize.xy;
                float2 sampleUV = uv + offset * texelSize * _Thickness;

                float3 color = SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    sampleUV
                ).rgb;
                return GANDT_Luminance(color);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;

                float3 originalColor = SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv
                ).rgb;
                

                // Sample the luminance of the surrounding pixels
                float tl = SampleLuminance(uv, float2(-1,  1));
                float  t = SampleLuminance(uv, float2( 0,  1));
                float tr = SampleLuminance(uv, float2( 1,  1));

                float  l = SampleLuminance(uv, float2(-1,  0));
                float  r = SampleLuminance(uv, float2( 1,  0));

                float bl = SampleLuminance(uv, float2(-1, -1));
                float  b = SampleLuminance(uv, float2( 0, -1));
                float br = SampleLuminance(uv, float2( 1, -1));

                float gx = (-tl - 2.0 * l - bl) + ( tr + 2.0 * r + br);
                float gy = ( tl + 2.0 * t + tr) + (-bl - 2.0 * b - br);
                // float gy = ( tl + 2.0 * t - tr) + (-bl - 2.0 * b - br);

                float edge = sqrt(gx * gx + gy * gy);
                edge *= _EdgeIntensity;

                edge = smoothstep(_Threshold, _Threshold + 0.05, edge);

                float3 outlinedColor = lerp(originalColor, _OutlineColor.rgb, edge * _OutlineColor.a);
                float3 finalColor = lerp(originalColor, outlinedColor, _Blend);

                // half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}
