// =====================================================
// GANDT Assignment Shader
// 실시간 영상처리 기반 공포 게임 화면 파이프라인
//
// 핵심 의도:
// - Unity 카메라가 렌더링한 화면을 입력으로 받아 후처리한다.
// - Sobel Edge를 통해 세계의 윤곽을 강조한다.
// - Memory Flow는 실제 프레임 간 옵티컬 플로우 계산이 아니라,
//   기억 오브젝트의 화면 좌표 방향으로 UV를 미세하게 변위시키는 연출이다.
// - Sanity와 Enemy Danger 값은 게임 시스템 상태를 화면 흔들림/붉은 에지로 변환한다.
//
// 현재 제출용 파이프라인:
// 1. Memory Flow UV Distortion
// 2. Grayscale / BW Quantization / Ordered Dithering
// 3. Sobel Edge Detection
//    - 현재 장면에서는 Color 기반 Edge가 가장 안정적이므로 기본값은 Color 중심으로 둔다.
//    - Depth/Normal Edge는 비교 실험 및 향후 개선용으로 남겨둔다.
// 4. Sanity / Enemy Danger Edge Instability
// 5. Final Composition / Debug View
//
// 참고:
// - PixelateUV 함수는 초기 실험 흔적으로 남겨두었지만,
//   현재 기본 파이프라인에서는 사용하지 않는다.
// =====================================================

Shader "Hidden/GANDT/ForAssignment"
{
    Properties
    {
        [Header(Debug Options)]
        [Space(5)]
        // 0: 최종 화면, 1: 에지만 출력, 2: 기본 색상 처리 결과, 3: Memory Flow 방향 확인
        [Enum(Final,0,Edge Only,1,Base Color,2,Flow UV,3)]
        _DebugViewMode ("Debug View Mode", Float) = 0

        [Header(Sobel Filter Settings)]
        [Space(5)]
        // 에지의 기본 색상. Enemy Danger 상태가 되면 _DangerEdgeColor로 보간된다.
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        // Sobel 결과가 이 값보다 작으면 에지로 보지 않는다.
        _Threshold ("Edge Threshold", Range(0, 1)) = 0.15
        // 주변 픽셀을 샘플링할 간격. 값이 커질수록 에지가 두꺼워지거나 넓게 잡힌다.
        _Thickness ("Thickness", Range(0.5, 5)) = 1.0
        // 최종 에지 강도 보정값.
        _EdgeIntensity ("Edge Intensity", Range(0, 5)) = 1.0
        // 원본/기본 화면과 에지 합성 결과 사이의 블렌딩 비율.
        _Blend ("Blend", Range(0, 1)) = 1.0

        [Header(Pixelation Settings Experimental and Currently Not Used)]
        [Space(5)]
        // 초기에는 픽셀화도 고려했으나 현재 제출용 파이프라인에서는 사용하지 않는다.
        // 필요할 경우 BuildPipelineUV에서 PixelateUV를 다시 적용하면 된다.
        _PixelResolution ("Pixel Resolution", Range(64, 720)) = 240

        [Header(Buffer Sensitivity Settings)]
        [Space(5)]
        // 현재 프로젝트에서는 Color Edge가 가장 안정적이어서 Depth/Normal 기본값을 낮게 둔다.
        // Depth/Normal 결과를 비교하고 싶으면 Inspector에서 값을 올려 확인한다.
        _DepthSensitivity ("Depth Sensitivity", Range(0, 20)) = 0
        _NormalSensitivity ("Normal Sensitivity", Range(0, 20)) = 0
        _ColorSensitivity ("Color Sensitivity", Range(0, 10)) = 1.5

        [Header(Grayscale Settings)]
        [Space(5)]
        _UseGrayScale ("Use Grayscale", Range(0, 1)) = 1
        _GrayContrast ("Gray Contrast", Range(0.2, 3.0)) = 1.0

        [Header(BW Quantize Settings)]
        [Space(5)]
        _UseBWQuantize ("Use BW Quantize", Range(0, 1)) = 0
        _BWThreshold ("BW Threshold", Range(0, 1)) = 0.5

        [Header(Ordered Dithering Settings)]
        [Space(5)]
        _UseDithering ("Use Dithering", Range(0, 1)) = 0
        _DitherScale ("Dither Scale", Range(0.25, 8.0)) = 1.0
        _DitherContrast ("Dither Contrast", Range(0.2, 3.0)) = 1.0

        [Header(Memory Flow Distortion)]
        [Space(5)]
        [Toggle] _UseMemoryFlow ("Use Memory Flow", Float) = 0
        // C# 컨트롤러에서 WorldToViewportPoint로 계산해 전달하는 기억 오브젝트의 화면 좌표.
        _MemoryScreenPos ("Memory Screen Pos", Vector) = (0.5, 0.5, 0, 0)
        // 플레이어와 기억 오브젝트의 거리에 따라 0~1로 변하는 영향도.
        _MemoryInfluence ("Memory Influence", Range(0, 1)) = 0
        // UV가 기억 오브젝트 방향으로 당겨지는 최대 강도.
        _MemoryPullStrength ("Memory Pull Strength", Range(0, 0.05)) = 0.005
        // 화면상에서 Memory Flow가 영향을 주는 반경.
        _MemoryFlowRadius ("Memory Flow Radius", Range(0.05, 2.0)) = 0.8

        [Header(Sanity Edge Instability)]
        [Space(5)]
        // 1이면 안정, 0에 가까울수록 흔들림이 강해진다.
        _Sanity ("Sanity", Range(0, 1)) = 1
        _EdgeJitterStrength ("Edge Jitter Strength", Range(0, 0.02)) = 0.002
        _EdgeJitterSpeed ("Edge Jitter Speed", Range(0, 20)) = 5

        [Header(Enemy Danger)]
        [Space(5)]
        // 적과 가까울수록 C# EnemyComponent에서 0~1로 전달한다.
        _EnemyDangerInfluence ("Enemy Danger Influence", Range(0, 1)) = 0
        _DangerEdgeColor ("Danger Edge Color", Color) = (1, 0, 0, 1)
        _DangerEdgeBoost ("Danger Edge Boost", Range(0, 5)) = 1.5
        _DangerJitterBoost ("Danger Jitter Boost", Range(0, 3)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        // Full Screen Pass 후처리이므로 깊이 쓰기와 컬링은 사용하지 않는다.
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            float4 _OutlineColor;
            float _Threshold;
            float _Thickness;
            float _EdgeIntensity;
            float _Blend;

            float _DebugViewMode;

            float _PixelResolution;

            float _DepthSensitivity;
            float _NormalSensitivity;
            float _ColorSensitivity;

            float _UseGrayScale;
            float _GrayContrast;

            float _UseBWQuantize;
            float _BWThreshold;

            float _UseDithering;
            float _DitherScale;
            float _DitherContrast;

            float _UseMemoryFlow;
            float4 _MemoryScreenPos;
            float _MemoryInfluence;
            float _MemoryPullStrength;
            float _MemoryFlowRadius;

            float _Sanity;
            float _EdgeJitterStrength;
            float _EdgeJitterSpeed;

            float _EnemyDangerInfluence;
            float4 _DangerEdgeColor;
            float _DangerEdgeBoost;
            float _DangerJitterBoost;

            // =====================================================
            // 1. Utility
            // =====================================================

            float GANDT_Luminance(float3 color)
            {
                // 인간 시각은 G 채널에 가장 민감하므로 표준 가중치를 사용해 명도를 계산한다.
                // Color 기반 Sobel은 이 명도값의 변화량을 기준으로 에지를 찾는다.
                return dot(color, float3(0.299, 0.587, 0.114));
            }

            float Hash21(float2 p)
            {
                // Sanity Jitter용 간단한 해시 노이즈.
                // 텍스처를 추가로 쓰지 않고 화면 좌표 기반 흔들림을 만들기 위해 사용한다.
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Bayer4x4(float2 pixelPos)
            {
                // 4x4 Bayer 행렬을 이용한 Ordered Dithering 패턴.
                // 제한된 흑백/명암 표현 안에서 중간 밝기를 패턴으로 표현한다.
                int x = (int)fmod(pixelPos.x, 4);
                int y = (int)fmod(pixelPos.y, 4);
                int index = y * 4 + x;

                float bayer[16] =
                {
                     0.0,  8.0,  2.0, 10.0,
                    12.0,  4.0, 14.0,  6.0,
                     3.0, 11.0,  1.0,  9.0,
                    15.0,  7.0, 13.0,  5.0
                };

                return (bayer[index] + 0.5) / 16.0;
            }

            float2 ClampUV(float2 uv)
            {
                // UV 변위 과정에서 0~1 범위를 벗어난 샘플링을 막는다.
                return saturate(uv);
            }

            // =====================================================
            // 2. UV Processing
            // =====================================================

            float2 PixelateUV(float2 uv)
            {
                // 초기 실험용 픽셀화 함수.
                // 현재는 가독성 문제 때문에 BuildPipelineUV에서 호출하지 않는다.
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 pixelCount = float2(_PixelResolution * aspect, _PixelResolution);

                return (floor(uv * pixelCount) + 0.5) / pixelCount;
            }

            float2 ApplyMemoryFlow(float2 uv)
            {
                if (_UseMemoryFlow < 0.5)
                {
                    return uv;
                }

                float2 memoryPos = _MemoryScreenPos.xy;

                // 현재 픽셀에서 기억 오브젝트의 화면 좌표까지 향하는 벡터.
                float2 toMemory = memoryPos - uv;
                float dist = length(toMemory);

                // dist가 0에 가까울 때 나눗셈 오류가 나지 않도록 max를 사용한다.
                float2 dir = toMemory / max(dist, 1e-5);

                // 기억 오브젝트 주변에서 강하고, 멀어질수록 0에 가까워지는 지역 영향도.
                float localInfluence = 1.0 - smoothstep(0.0, _MemoryFlowRadius, dist);
                float pull = localInfluence * _MemoryInfluence * _MemoryPullStrength;

                // 실제 옵티컬 플로우 계산은 아니지만, 방향 벡터를 이용해 픽셀 샘플링 위치를 이동시킨다.
                return uv + dir * pull;
            }

            float2 ApplySanityJitter(float2 uv)
            {
                float insanity = 1.0 - saturate(_Sanity);
                float danger = saturate(_EnemyDangerInfluence);

                // 이성이 낮거나 적이 가까우면 화면/에지의 불안정성이 증가한다.
                float instability = saturate(insanity + danger * _DangerJitterBoost);

                if (instability <= 0.001)
                {
                    return uv;
                }

                float t = _Time.y * _EdgeJitterSpeed;

                // 픽셀 단위가 아니라 블록 단위로 흔들림을 만들어, 화면이 미세하게 깨지는 듯한 느낌을 준다.
                float2 blockPos = floor(uv * _ScreenParams.xy * 0.25);

                float n1 = Hash21(blockPos + t);
                float n2 = Hash21(blockPos - t);

                float2 jitter = float2(n1 - 0.5, n2 - 0.5);

                return uv + jitter * _EdgeJitterStrength * instability;
            }

            float2 BuildPipelineUV(float2 uv)
            {
                // 현재 제출용 흐름에서는 Memory Flow만 기본 UV 변형으로 사용한다.
                float2 flowUV = ApplyMemoryFlow(uv);

                // 픽셀화를 다시 사용하고 싶다면 아래 두 줄로 교체한다.
                // float2 pixelUV = PixelateUV(flowUV);
                // return ClampUV(pixelUV);

                return ClampUV(flowUV);
            }

            // =====================================================
            // 3. Scene Sampling
            // =====================================================

            float3 SampleSceneColor(float2 uv)
            {
                uv = ClampUV(uv);

                // Full Screen Pass의 입력 화면 색상.
                return SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv
                ).rgb;
            }

            float SampleColorLuminance(float2 uv)
            {
                return GANDT_Luminance(SampleSceneColor(uv));
            }

            float SampleLinearDepth(float2 uv)
            {
                uv = ClampUV(uv);

                // Depth Texture를 선형 Eye Depth로 변환한다.
                // 다만 현재 장면에서는 결과가 뿌옇게 보일 수 있어 기본 가중치는 낮게 둔다.
                float rawDepth = SampleSceneDepth(uv);
                return LinearEyeDepth(rawDepth, _ZBufferParams);
            }

            float3 SampleNormal(float2 uv)
            {
                uv = ClampUV(uv);

                // URP의 Camera Normals Texture에서 월드 노멀을 샘플링한다.
                // 평평한 구조가 많은 장면에서는 변화량이 약해 에지가 잘 안 보일 수 있다.
                float3 normalWS = SampleSceneNormals(uv);
                return normalize(normalWS);
            }

            // =====================================================
            // 4. Base Image Processing
            // =====================================================

            float3 BuildBaseColor(float3 originalColor, float luminance, float2 uvForDither)
            {
                // Grayscale: 색상보다 명암과 윤곽 중심으로 화면을 재구성한다.
                float adjustedLum = pow(saturate(luminance), _GrayContrast);
                float3 grayscaleColor = adjustedLum.xxx;

                float3 baseColor = lerp(originalColor, grayscaleColor, _UseGrayScale);

                // BW Quantize: 명도 기준으로 흑/백을 나누는 제한 팔레트 실험.
                float bw = step(_BWThreshold, adjustedLum);
                float3 bwColor = bw.xxx;

                baseColor = lerp(baseColor, bwColor, _UseBWQuantize);

                // Ordered Dithering: 연속 명암을 4x4 패턴으로 바꿔 손상된 기록 같은 질감을 만든다.
                float2 pixelPos = floor(uvForDither * _ScreenParams.xy / max(_DitherScale, 0.001));
                float threshold = Bayer4x4(pixelPos);

                float ditherLum = pow(saturate(luminance), _DitherContrast);
                float dither = step(threshold, ditherLum);
                float3 ditherColor = dither.xxx;

                baseColor = lerp(baseColor, ditherColor, _UseDithering);

                return baseColor;
            }

            // =====================================================
            // 5. Edge Detection
            // =====================================================

            float ComputeSobelEdge(float2 uv)
            {
                // Sanity와 Enemy Danger에 따른 흔들림은 에지 샘플링 위치에 적용한다.
                // 즉, 원본 화면 전체가 크게 흔들리는 것이 아니라 윤곽선이 불안정하게 떨리는 효과를 만든다.
                uv = ApplySanityJitter(uv);
                uv = ClampUV(uv);

                float2 texel = _BlitTexture_TexelSize.xy * _Thickness;

                // 3x3 Sobel 커널을 적용하기 위한 주변 8방향 UV.
                float2 uv_tl = uv + texel * float2(-1,  1);
                float2 uv_t  = uv + texel * float2( 0,  1);
                float2 uv_tr = uv + texel * float2( 1,  1);

                float2 uv_l  = uv + texel * float2(-1,  0);
                float2 uv_r  = uv + texel * float2( 1,  0);

                float2 uv_bl = uv + texel * float2(-1, -1);
                float2 uv_b  = uv + texel * float2( 0, -1);
                float2 uv_br = uv + texel * float2( 1, -1);

                // -----------------------------
                // Depth Sobel
                // -----------------------------
                float d_center = SampleLinearDepth(uv);

                float d_tl = SampleLinearDepth(uv_tl);
                float d_t  = SampleLinearDepth(uv_t);
                float d_tr = SampleLinearDepth(uv_tr);
                float d_l  = SampleLinearDepth(uv_l);
                float d_r  = SampleLinearDepth(uv_r);
                float d_bl = SampleLinearDepth(uv_bl);
                float d_b  = SampleLinearDepth(uv_b);
                float d_br = SampleLinearDepth(uv_br);

                // Sobel X/Y 커널. X는 좌우 변화량, Y는 상하 변화량을 계산한다.
                float depthGx = (-d_tl - 2.0 * d_l - d_bl) + (d_tr + 2.0 * d_r + d_br);
                float depthGy = ( d_tl + 2.0 * d_t + d_tr) + (-d_bl - 2.0 * d_b - d_br);

                float depthEdge = sqrt(depthGx * depthGx + depthGy * depthGy);

                // 깊이값은 거리 영향을 크게 받으므로 중심 깊이로 보정한다.
                // 이 보정에도 불구하고 현재 장면에서는 흐릿하게 나와 기본 가중치를 낮춰두었다.
                depthEdge = depthEdge / max(d_center, 0.01);

                // -----------------------------
                // Normal Sobel
                // -----------------------------
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

                float normalEdge = sqrt(dot(normalGx, normalGx) + dot(normalGy, normalGy));

                // -----------------------------
                // Color Sobel
                // -----------------------------
                // 현재 프로젝트에서는 플레이어가 실제로 보는 명암 경계와 가장 잘 맞아 Color Edge를 중심으로 사용한다.
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

                // 세 입력을 가중합한다.
                // 보고서에서는 Color/Depth/Normal을 비교했고, 최종적으로 Color 중심으로 판단했다고 설명하면 된다.
                float edge =
                    depthEdge * _DepthSensitivity +
                    normalEdge * _NormalSensitivity +
                    colorEdge * _ColorSensitivity;

                // 적이 가까울수록 에지 강도를 더 키워 위험 상태를 강조한다.
                float dangerBoost = saturate(_EnemyDangerInfluence) * _DangerEdgeBoost;
                edge *= (_EdgeIntensity + dangerBoost);

                // Threshold 주변을 smoothstep으로 부드럽게 정리한다.
                edge = smoothstep(_Threshold, _Threshold + 0.05, edge);

                return saturate(edge);
            }

            // =====================================================
            // 6. Final Composition
            // =====================================================

            float3 ComposeFinalColor(float3 baseColor, float edge)
            {
                float danger = saturate(_EnemyDangerInfluence);

                // 평상시에는 흰 에지, 위험 시에는 붉은 에지로 보간한다.
                float3 edgeColor = lerp(_OutlineColor.rgb, _DangerEdgeColor.rgb, danger);
                float edgeAlpha = lerp(_OutlineColor.a, _DangerEdgeColor.a, danger);

                float edgeMask = saturate(edge * edgeAlpha);

                // 에지 부분만 edgeColor로 덮고, 나머지는 baseColor를 유지한다.
                float3 outlinedColor = lerp(baseColor, edgeColor, edgeMask);
                float3 finalColor = lerp(baseColor, outlinedColor, _Blend);

                return finalColor;
            }

            float3 BuildFlowDebugColor(float2 originalUV)
            {
                float2 flowUV = ApplyMemoryFlow(originalUV);
                float2 displacement = flowUV - originalUV;

                // R/G: UV가 밀리는 방향, B: 기억 영향도.
                // 실제 최종 화면이 아니라 Memory Flow 계산을 확인하기 위한 디버그 출력이다.
                float2 visualDirection = 0.5 + displacement * 80.0;
                return float3(visualDirection, _MemoryInfluence);
            }

            float3 ApplyDebugView(float3 finalColor, float edge, float3 baseColor, float2 originalUV)
            {
                if (_DebugViewMode < 0.5)
                {
                    return finalColor;
                }

                if (_DebugViewMode < 1.5)
                {
                    return edge.xxx;
                }

                if (_DebugViewMode < 2.5)
                {
                    return baseColor;
                }

                return BuildFlowDebugColor(originalUV);
            }

            // =====================================================
            // 7. Fragment
            // =====================================================

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;

                // Memory Flow가 반영된 UV. 현재는 Pixelation을 적용하지 않는다.
                float2 pipelineUV = BuildPipelineUV(uv);

                // Scene Sampling
                float3 sceneColor = SampleSceneColor(pipelineUV);
                float luminance = GANDT_Luminance(sceneColor);

                // Base Image
                // 디더링을 켠 경우 Memory Flow의 영향을 받은 UV 기준으로 패턴을 계산한다.
                float3 baseColor = BuildBaseColor(sceneColor, luminance, pipelineUV);

                // Sobel Edge
                float edge = ComputeSobelEdge(pipelineUV);

                // Final Composition
                float3 finalColor = ComposeFinalColor(baseColor, edge);

                // Debug View
                finalColor = ApplyDebugView(finalColor, edge, baseColor, uv);

                return half4(finalColor, 1.0);
            }

            ENDHLSL
        }
    }
}
