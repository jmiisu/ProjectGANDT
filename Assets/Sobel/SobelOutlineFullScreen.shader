Shader "Hidden/GANDT/SobelOutlineFullScreen"
{
    Properties
    {
        // Edge Only 디버그 모드
        _DebugEdgeOnly ("Debug Edge Only", Range(0, 1)) = 0

        // Sobel 스타일 필터 
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _Threshold ("Edge Threshold", Range(0, 1)) = 0.15
        _Thickness ("Thickness", Range(0.5, 5)) = 1.0
        _EdgeIntensity ("Edge Intensity", Range(0, 5)) = 1.0
        _Blend ("Blend", Range(0, 1)) = 1.0

        _PixelResolution ("Pixel Resolution", Range(64, 720)) = 240

        // Depth, Normal, Color
        _DepthSensitivity ("Depth Sensitivity", Range(0, 20)) = 5
        _NormalSensitivity ("Normal Sensitivity", Range(0, 20)) = 4
        _ColorSensitivity ("Color Sensitivity", Range(0, 10)) = 1
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

            // Depth / Normal 텍스처 include 추가
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"


            float4 _OutlineColor;
            float _Threshold;
            float _Thickness;
            float _EdgeIntensity;
            float _Blend;

            // 디버그 변수
            float _DebugEdgeOnly;

            float _PixelResolution;

            float _DepthSensitivity;
            float _NormalSensitivity;
            float _ColorSensitivity;

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

            // Pixelation 함수
            // _PixelResolution이 낮을수록 더 뭉게짐
            float2 PixelateUV(float2 uv)
            {
                float aspect = _ScreenParams.x / _ScreenParams.y;

                float2 pixelCount = float2(_PixelResolution * aspect, _PixelResolution);

                return (floor(uv * pixelCount) + 0.5) / pixelCount;
            }

            // Depth + Normal Sobel 샘플링 함수
            // 현재는 Color Luminance만 Sobel로 보고 있음
            // 여기서 핵심은 Depth 차이와 Normal 차이를 같이 보는 것
            float SampleLinearDepth(float2 uv)
            {
                float rawDepth = SampleSceneDepth(uv);
                return LinearEyeDepth(rawDepth, _ZBufferParams);
            }

            float3 SampleNormal(float2 uv)
            {
                return normalize(SampleSceneNormals(uv));
            }

            float SampleColorLuminance(float2 uv)
            {
                float3 color = SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv
                ).rgb;
                return GANDT_Luminance(color);
            }

            // Sobel 계산 함수
            float ComputeSobelEdge(float2 uv)
            {
                float2 texel = _BlitTexture_TexelSize.xy * _Thickness;

                float2 uv_tl = uv + texel * float2(-1,  1);
                float2 uv_t  = uv + texel * float2( 0,  1);
                float2 uv_tr = uv + texel * float2( 1,  1);

                float2 uv_l  = uv + texel * float2(-1,  0);
                float2 uv_r  = uv + texel * float2( 1,  0);

                float2 uv_bl = uv + texel * float2(-1, -1);
                float2 uv_b  = uv + texel * float2( 0, -1);
                float2 uv_br = uv + texel * float2( 1, -1);

                // Depth Sobel
                float d_tl = SampleLinearDepth(uv_tl);
                float d_t  = SampleLinearDepth(uv_t);
                float d_tr = SampleLinearDepth(uv_tr);
                float d_l  = SampleLinearDepth(uv_l);
                float d_r  = SampleLinearDepth(uv_r);
                float d_bl = SampleLinearDepth(uv_bl);
                float d_b  = SampleLinearDepth(uv_b);
                float d_br = SampleLinearDepth(uv_br);

                float depthGx = (-d_tl - 2.0 * d_l - d_bl) + (d_tr + 2.0 * d_r + d_br);
                float depthGy = ( d_tl + 2.0 * d_t + d_tr) + (-d_bl - 2.0 * d_b - d_br);

                float depthEdge = sqrt(depthGx * depthGx + depthGy * depthGy);

                // Normal Sobel
                float3 n_tl = SampleNormal(uv_tl);
                float3 n_t  = SampleNormal(uv_t);
                float3 n_tr = SampleNormal(uv_tr);
                float3 n_l  = SampleNormal(uv_l);
                float3 n_r  = SampleNormal(uv_r);
                float3 n_bl = SampleNormal(uv_bl);
                float3 n_b  = SampleNormal(uv_b);
                float3 n_br = SampleNormal(uv_br);

                float3 normalGx = (-n_tl - 2.0 * n_l - n_bl) + (n_tr + 2.0 * n_r + n_br);
                float3 normalGy = ( n_tl + 2.0 * n_t + n_tr) + (-n_bl - 2.0 * n_b - n_br);

                float normalEdge = length(normalGx) + length(normalGy);

                // Color Sobel, 선택적 보조용
                float c_tl = SampleColorLuminance(uv_tl);
                float c_t  = SampleColorLuminance(uv_t);
                float c_tr = SampleColorLuminance(uv_tr);
                float c_l  = SampleColorLuminance(uv_l);
                float c_r  = SampleColorLuminance(uv_r);
                float c_bl = SampleColorLuminance(uv_bl);
                float c_b  = SampleColorLuminance(uv_b);
                float c_br = SampleColorLuminance(uv_br);

                float colorGx = (-c_tl - 2.0 * c_l - c_bl) + (c_tr + 2.0 * c_r + c_br);
                float colorGy = ( c_tl + 2.0 * c_t + c_tr) + (-c_bl - 2.0 * c_b - c_br);

                float colorEdge = sqrt(colorGx * colorGx + colorGy * colorGy);

                float edge =
                    depthEdge * _DepthSensitivity +
                    normalEdge * _NormalSensitivity +
                    colorEdge * _ColorSensitivity;

                edge *= _EdgeIntensity;

                edge = smoothstep(_Threshold, _Threshold + 0.05, edge);

                return saturate(edge);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;

                // 2. Pixelation
                float2 pixelUV = PixelateUV(uv);

                // 3. 원본 컬러 샘플
                float3 originalColor = SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    // uv
                    pixelUV
                ).rgb;

                // 6. Depth + Normal + Color Sobel edge
                float edge = ComputeSobelEdge(pixelUV);
               
                // 7-2. 260611 Edge 합성
                float3 outlinedColor = lerp(originalColor, _OutlineColor.rgb, edge * _OutlineColor.a);
                float3 normalModeColor = lerp(originalColor, outlinedColor, _Blend);


                // 8. Edge Only 디버그 모드에서는 윤곽선 색상만 출력
                float3 edgeOnlyColor = edge.xxx;

                float3 finalColor = lerp(outlinedColor, edgeOnlyColor, _DebugEdgeOnly);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}
