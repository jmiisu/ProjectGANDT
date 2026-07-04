using TMPro;
using UnityEngine;

public class GANDT_DebugStatusUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private GANDT_PlayerStatus playerStatus;

    [SerializeField]
    private GANDT_PostProcessController postProcessController;

    [Header("UI Texts")]
    [SerializeField] private TMP_Text debugViewModeText;

    [SerializeField] private TMP_Text mentalnessText;

    [SerializeField] private TMP_Text sanityText;

    [SerializeField] private TMP_Text memoryInfluenceText;

    [SerializeField] private TMP_Text enemyDangerText;

    [Header("Display")]
    [SerializeField] private bool showAsPercent;
    [SerializeField] private string emptyValueText = "-";

    private void Reset()
    {
        FindReferences();
    }

    private void Awake()
    {
        FindReferences();
    }

    private void Update()
    {
        UpdatePlayerStatusTexts();
        UpdatePostProcessTexts();
    }

    private void FindReferences()
    {
        if (playerStatus == null)
        {
            playerStatus = FindFirstObjectByType<GANDT_PlayerStatus>();
        }

        if (postProcessController == null)
        {
            postProcessController = FindFirstObjectByType<GANDT_PostProcessController>();
        }
    }

    private void UpdatePlayerStatusTexts()
    {
        if (playerStatus == null)
        {
            SetText(mentalnessText, emptyValueText);
            SetText(sanityText, emptyValueText);
            return;
        }

        SetText(
            mentalnessText,
            FormatValue(playerStatus.MentalPower)
        );

        SetText(
            sanityText,
            FormatValue(playerStatus.Sanity)
        );
    }

    private void UpdatePostProcessTexts()
    {
        if (postProcessController == null)
        {
            SetText(debugViewModeText, emptyValueText);
            SetText(memoryInfluenceText, emptyValueText);
            SetText(enemyDangerText, emptyValueText);
            return;
        }

        SetText(
            debugViewModeText,
            postProcessController.GetDebugViewModeName()
        );

        SetText(
            memoryInfluenceText,
            FormatValue(postProcessController.MemoryInfluence)
        );

        SetText(
            enemyDangerText,
            FormatValue(postProcessController.EnemyDangerInfluence)
        );
    }

    private void SetText(TMP_Text targetText, string value)
    {
        if (targetText != null)
        {
            targetText.text = value;
        }
    }

    private string FormatValue(float value)
    {
        value = Mathf.Clamp01(value);

        if (showAsPercent)
        {
            return $"{Mathf.RoundToInt(value * 100f)}";
        }

        return value.ToString("0.00");
    }
}