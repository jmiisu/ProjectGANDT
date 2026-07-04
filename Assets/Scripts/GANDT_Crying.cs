using System;
using UnityEngine;

public class GANDT_Crying : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode cryingKey = KeyCode.G;
    
    [Header("Crying")]
    [SerializeField] private float requiredDuration = 5.0f;
    [SerializeField] private float cooldownDuration = 8.0f;

    [Header("Cost")]
    [SerializeField] private float startSanityCost = 0.2f;
    [SerializeField] private float sanityDrainPerSecond = 0.1f;

    [Header("Player Status")]
    [SerializeField] private GANDT_PlayerStatus playerStatus;

    public bool IsActive { get; private set; }
    public float Progress { get; private set; }
    public float CooldownRemaining { get; private set; }

    public float ProgressNormalized 
        => Mathf.Clamp01(Progress / requiredDuration);

    public float CooldownNormalized 
        => cooldownDuration <= 0f 
            ? 0f 
            : Mathf.Clamp01(CooldownRemaining / cooldownDuration);

    public bool IsOnCooldown => CooldownRemaining > 0f;
    public bool CanStart => !IsActive && !IsOnCooldown;

    public event Action OnStarted;
    public event Action OnCancelled;
    public event Action OnCompleted;
    public event Action OnCooldownEnded;

    private void Reset()
    {
        playerStatus = GetComponent<GANDT_PlayerStatus>();
    }

    private void Awake()
    {
        if (playerStatus == null)
        {
            playerStatus = GetComponent<GANDT_PlayerStatus>();
        }
    }

    private void Update()
    {
        UpdateCooldown();
        UpdateInput();

        if (IsActive)
        {
            UpdateCrying();
            ApplyCost();
        }
    }

    private void UpdateInput()
    {
        if (Input.GetKeyDown(cryingKey))
        {
            StartCrying();
        }
        else if (Input.GetKeyUp(cryingKey) && IsActive)
        {
            CancelCrying();
        }
    }

    private void StartCrying()
    {
        if (!CanStart)
        {
            return;
        }

        IsActive = true;
        Progress = 0f;

        if (playerStatus != null)
        {
            playerStatus.AddSanity(-startSanityCost);
        }

        OnStarted?.Invoke();
    }

    private void CancelCrying()
    {
        IsActive = false;
        Progress = 0f;

        OnCancelled?.Invoke();
    }

    private void UpdateCrying()
    {
        Progress += Time.deltaTime;
        if (Progress >= requiredDuration)
        {
            CompleteCrying();
        }
    }

    private void ApplyCost()
    {
        if (playerStatus != null)
        {
            playerStatus.AddSanity(-sanityDrainPerSecond * Time.deltaTime);
        }
    }

    private void CompleteCrying()
    {
        IsActive = false;
        Progress = requiredDuration;
        CooldownRemaining = cooldownDuration;

        OnCompleted?.Invoke();
    }

    private void UpdateCooldown()
    {
        if (CooldownRemaining <= 0f)
        {
            return;
        }

        CooldownRemaining = Mathf.Max(0f, CooldownRemaining - Time.deltaTime);

        if (CooldownRemaining <= 0f)
        {
            Progress = 0f;
            OnCooldownEnded?.Invoke();
        }
    }
}
