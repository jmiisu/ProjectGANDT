Shader "Hidden/GANDT/ForAssignment"
{
    Properties
    {
        // Edge Only 디버그 모드
        [Header(Debug Options)]
        [Space(5)]
        _DebugEdgeOnly ("Debug Edge Only", Range(0, 1)) = 0

        // Sobel 스타일 필터 
        [Header(Sobel Filter Settings)]
        [Space(5)]
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _Threshold ("Edge Threshold", Range(0, 1)) = 0.15
        _Thickness ("Thickness", Range(0.5, 5)) = 1.0
        _EdgeIntensity ("Edge Intensity", Range(0, 5)) = 1.0
        _Blend ("Blend", Range(0, 1)) = 1.0

        // Pixelation 옵션
        [Header(Pixelation Settings)]
        [Space(5)]
        _PixelResolution ("Pixel Resolution", Range(64, 720)) = 240

        // Depth, Normal, Color
        [Header(Buffer Sensitivity Settings)]
        [Space(5)]
        _DepthSensitivity ("Depth Sensitivity", Range(0, 20)) = 5
        _NormalSensitivity ("Normal Sensitivity", Range(0, 20)) = 4
        _ColorSensitivity ("Color Sensitivity", Range(0, 10)) = 1

        // Grayscale 옵션
        // 흑백 기억 영상 효과를 위해 추가한 옵션
        [Header(Grayscale Settings)]
        [Space(5)]
        _UseGrayScale ("Use Grayscale", Range(0, 1)) = 1
        _GrayContrast ("Gray Contrast", Range(0.2, 3.0)) = 1.0

        // 흑백 양자화
        // 손상된 기억 영상 느낌을 위해 추가한 옵션
        [Header(BW Quantize Settings)]
        [Space(5)]
        _UseBWQuantize ("Use BW Quantize", Range(0, 1)) = 0
        _BWThreshold ("BW Threshold", Range(0, 1)) = 0.5

        // Ordered Dithering 옵션
        // 양자화된 이미지의 계조를 개선하기 위해 추가한 옵션
        [Header(Ordered Dithering Settings)]
        [Space(5)]
        _UseDithering ("Use Dithering", Range(0, 1)) = 0
        _DitherScale ("Dither Scale", Range(0.25, 8.0)) = 1.0
        _DitherContrast ("Dither Contrast", Range(0.2, 3.0)) = 1.0
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
            Name "ForAssignment"

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

            // Grayscale 옵션
            float _UseGrayScale;
            float _GrayContrast;

            // 흑백 양자화 옵션
            float _UseBWQuantize;
            float _BWThreshold;

            // Ordered Dithering 옵션
            float _UseDithering;
            float _DitherScale;
            float _DitherContrast;

            // GANDT 전용 luminance 계산 함수
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

            // Ordered Dithering 함수
            float Bayer4x4(float2 pixelPos)
            {
                int x = (int)fmod(pixelPos.x, 4);
                int y = (int)fmod(pixelPos.y, 4);
                int index = y + x * 4;

                float bayer[16] = {
                    0.0,  8.0,  2.0, 10.0,
                    12.0, 4.0, 14.0,  6.0,
                    3.0, 11.0,  1.0,  9.0,
                    15.0, 7.0, 13.0,  5.0
                };

                return (bayer[index] + 0.5) / 16.0;
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


                // 4. Grayscale 옵션 적용
                float luminance = GANDT_Luminance(originalColor);
                luminance = pow(saturate(luminance), _GrayContrast); // 콘트라스트 조절

                float3 grayscaleColor = luminance.xxx;
                float3 baseColor = lerp(originalColor, grayscaleColor, _UseGrayScale);

                // 5. 흑백 양자화 옵션 적용
                float bw = step(_BWThreshold, luminance);
                float3 bwColor = bw.xxx;

                baseColor = lerp(baseColor, bwColor, _UseBWQuantize);

                // 6. Ordered Dithering 적용
                float2 pixelPos = floor(input.positionCS.xy / _DitherScale);
                float threshold = Bayer4x4(pixelPos);

                float ditherLum = pow(saturate(luminance), _DitherContrast);
                float dither = step(threshold, ditherLum);
                float3 ditherColor = dither.xxx;

                baseColor = lerp(baseColor, ditherColor, _UseDithering);

                // 6. Depth + Normal + Color Sobel edge
                float edge = ComputeSobelEdge(pixelUV);


                // 7. Edge 합성
                /*
                
                // float3 outlinedColor = lerp(originalColor, _OutlineColor.rgb, edge * _OutlineColor.a);
                float3 outlinedColor = lerp(posterizedColor, _OutlineColor.rgb, edge * _OutlineColor.a);
                // float3 finalColor = lerp(originalColor, outlinedColor, _Blend);
                // float3 normalModeColor = lerp(originalColor, outlinedColor, _Blend);
                float3 normalModeColor = lerp(posterizedColor, outlinedColor, _Blend);
                
                */

                // 7-1. 수정된 Edge 합성
                
                // float edgeMask = saturate(edge * _Blend * _OutlineColor.a);
                // float3 finalBaseColor = posterizedColor;
                // float3 outlinedColor = lerp(posterizedColor, _OutlineColor.rgb, edgeMask * 0.65);
               
                // 7-2. 260611 Edge 합성
                float3 outlinedColor = lerp(baseColor, _OutlineColor.rgb, edge * _OutlineColor.a);
                float3 normalModeColor = lerp(baseColor, outlinedColor, _Blend);


                // 8. Edge Only 디버그 모드에서는 윤곽선 색상만 출력
                float3 edgeOnlyColor = edge.xxx;

                // _DebugEdgeOnly = 0이면 일반 외곽선 모드
                // _DebugEdgeOnly = 1이면 Edge Only 디버그 모드  

                // 7번 방법
                // float3 finalColor = lerp(normalModeColor, edgeOnlyColor, _DebugEdgeOnly); 
                // 7-1번 방법
                float3 finalColor = lerp(normalModeColor, edgeOnlyColor, _DebugEdgeOnly);


                // half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}
