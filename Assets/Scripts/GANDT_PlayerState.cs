using System;
using UnityEngine;

public class GANDT_PlayerState : MonoBehaviour
{
    [Header("Eye Closing")]
    [SerializeField] private KeyCode closeEyesKey = KeyCode.H;
    [SerializeField] private float safeEyeCloseDuration = 2.0f;

    [Header("Crying")]
    [SerializeField] private KeyCode cryingKey = KeyCode.G;
    [SerializeField] private float cryingRequiredDuration = 5.0f;
    [SerializeField] private float cryingCooldownDuration = 8.0f;

    public bool IsEyesClosed { get; private set; }

    public bool IsCrying { get; private set; }

    public float EyeClosedDuration { get; private set; }
    public float CryingProgress { get; private set; }
    public float CryingProgressNormalized => Mathf.Clamp01(CryingProgress / cryingRequiredDuration);

    public bool IsEyeClosingUnsafe => IsEyesClosed && EyeClosedDuration >= safeEyeCloseDuration;


    public event Action OnCryingStarted;
    public event Action OnCryingCancelled;
    public event Action OnCryingCompleted;

    private float cooldownRemaining;
    private bool cryingCompleted;

    private void Update()
    {
        UpdateCooldown();
        UpdateEyeClosing();
        UpdateCrying();
    }

    private void UpdateEyeClosing()
    {
        IsEyesClosed = Input.GetKey(closeEyesKey);
        if (IsEyesClosed)
        {
            EyeClosedDuration += Time.deltaTime;
        }
        else
        {
            EyeClosedDuration = 0f;
        }
    }

    private void UpdateCrying()
    {
        bool canCry = cooldownRemaining <= 0f;
        bool cryingInput = Input.GetKey(cryingKey);

        if (!canCry || cryingCompleted)
        {
            IsCrying = false;
            return;
        }

        if (cryingInput)
        {
            if (!IsCrying)
            {
                IsCrying = true;
                OnCryingStarted?.Invoke();
            }
            
            CryingProgress += Time.deltaTime;

            if (CryingProgress >= cryingRequiredDuration)
            {
                CompleteCrying();
            }
        }
        else if (IsCrying)
        {
            CancelCrying();
        }
    }

    private void CompleteCrying()
    {
        IsCrying = false;
        cryingCompleted = true;
        CryingProgress = cryingRequiredDuration;
        cooldownRemaining = cryingCooldownDuration;

        OnCryingCompleted?.Invoke();
    }

    private void CancelCrying()
    {
        IsCrying = false;
        CryingProgress = 0f;
        
        OnCryingCancelled?.Invoke();
    }

    private void UpdateCooldown()
    {
        if (cooldownRemaining <= 0f)
        {
            if (cryingCompleted)
            {
                cryingCompleted = false;
                CryingProgress = 0f;
            }
            return;
        }

        cooldownRemaining -= Time.deltaTime;
    }
}
