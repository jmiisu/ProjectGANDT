using TMPro;
using UnityEngine;

/// <summary>
/// GANDT 시연용 디버그 UI.
///
/// 역할:
/// - 현재 셰이더 디버그 뷰 모드(Final, Edge Only, Base Color, Flow UV)를 표시한다.
/// - Memory Influence, Sanity, Enemy Danger 값을 실시간으로 보여준다.
/// - 보고서/시연 영상에서 후처리 효과가 어떤 게임 상태값과 연결되는지 설명하기 위한 UI이다.
/// </summary>
public class GANDT_DebugStatusUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("후처리 셰이더에 값을 전달하는 컨트롤러.")]
    [SerializeField] private GANDT_PostProcessController postProcessController;

    [Header("UI Texts")]
    [Tooltip("현재 디버그 뷰 모드 표시용 텍스트.")]
    [SerializeField] private TMP_Text debugViewModeText;

    [Tooltip("기억 오브젝트 영향도 표시용 텍스트.")]
    [SerializeField] private TMP_Text memoryInfluenceText;

    [Tooltip("현재 이성 수치 표시용 텍스트.")]
    [SerializeField] private TMP_Text sanityText;

    [Tooltip("적 접근 위험도 표시용 텍스트.")]
    [SerializeField] private TMP_Text enemyDangerText;

    [Header("Display Option")]
    [Tooltip("true이면 0~1 값을 0~100 정수 형태로 표시한다. false이면 0.00 형식으로 표시한다.")]
    [SerializeField] private bool showAsPercent = false;

    [Tooltip("컨트롤러를 찾지 못했을 때 UI에 표시할 기본 문자열.")]
    [SerializeField] private string emptyValueText = "-";

    private void Reset()
    {
        // 컴포넌트를 처음 붙였을 때 씬에 존재하는 컨트롤러를 자동 연결한다.
        postProcessController = FindFirstObjectByType<GANDT_PostProcessController>();
    }

    private void Awake()
    {
        // Inspector에서 직접 연결하지 않은 경우 런타임에 한 번 더 탐색한다.
        if (postProcessController == null)
        {
            postProcessController = FindFirstObjectByType<GANDT_PostProcessController>();
        }
    }

    private void Update()
    {
        if (postProcessController == null)
        {
            SetEmptyTexts();
            return;
        }

        UpdateDebugStatusText();
    }

    private void UpdateDebugStatusText()
    {
        // Debug View Mode는 숫자 enum 대신 사람이 읽기 쉬운 이름으로 표시한다.
        if (debugViewModeText != null)
        {
            debugViewModeText.text = postProcessController.GetDebugViewModeName();
        }

        // 기억 오브젝트 영향도: 기억에 가까울수록 1에 가까워진다.
        if (memoryInfluenceText != null)
        {
            memoryInfluenceText.text = FormatValue(postProcessController.GetMemoryInfluence());
        }

        // 이성 수치: 낮아질수록 셰이더에서 에지 흔들림이 강해진다.
        if (sanityText != null)
        {
            sanityText.text = FormatValue(postProcessController.GetSanity());
        }

        // 적 위험도: 적 반경 안에서 가까울수록 1에 가까워진다.
        if (enemyDangerText != null)
        {
            enemyDangerText.text = FormatValue(postProcessController.GetEnemyDangerInfluence());
        }
    }

    private void SetEmptyTexts()
    {
        // 컨트롤러 연결이 끊긴 상태에서도 UI가 이전 값을 계속 보여주지 않도록 비운다.
        if (debugViewModeText != null)
        {
            debugViewModeText.text = emptyValueText;
        }

        if (memoryInfluenceText != null)
        {
            memoryInfluenceText.text = emptyValueText;
        }

        if (sanityText != null)
        {
            sanityText.text = emptyValueText;
        }

        if (enemyDangerText != null)
        {
            enemyDangerText.text = emptyValueText;
        }
    }

    private string FormatValue(float value)
    {
        // 셰이더에 전달되는 주요 상태값은 0~1 범위로 통일되어 있으므로 표시 전에도 한 번 제한한다.
        value = Mathf.Clamp01(value);

        if (showAsPercent)
        {
            return $"{Mathf.RoundToInt(value * 100f)}";
        }

        return value.ToString("0.00");
    }
}
