using System;
using UnityEngine;

public class GANDT_EyeClosing : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode closeEyesKey = KeyCode.H;

    [Header("Eye Closing")]
    [SerializeField] private float safeDuration = 2.0f;
    [SerializeField] private float mentalnessDrainPerSecond = 0.08f;

    [Header("Player Status")]
    [SerializeField] private GANDT_PlayerStatus playerStatus;

    public bool IsActive { get; private set; }
    public float Duration { get; private set; }
    public bool IsUnsafe => IsActive && Duration >= safeDuration;

    public event Action OnStarted;
    public event Action OnEnded;
    public event Action OnUnsafeStarted;

    private bool wasUnsafe;

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
        UpdateInput();
        UpdateDuration();
        ApplyCost();
        UpdateUnsafeState();
    }

    private void UpdateInput()
    {
        bool nextActive = Input.GetKey(closeEyesKey);
        if (nextActive == IsActive)
        {
            return;
        }

        IsActive = nextActive;

        if (IsActive)
        {
            OnStarted?.Invoke();
        }
        else
        {
            Duration = 0f;
            wasUnsafe = false;
            OnEnded?.Invoke();
        }
    }

    private void UpdateDuration()
    {
        if (!IsActive)
        {
            return;
        }
        
        Duration += Time.deltaTime;
    }

    private void ApplyCost()
    {
        if (!IsUnsafe || playerStatus == null)
        {
            return;
        }

        playerStatus.AddMentalPower(-mentalnessDrainPerSecond * Time.deltaTime);
    }

    private void UpdateUnsafeState()
    {
        bool isUnsafeNow = IsUnsafe;

        if (isUnsafeNow && !wasUnsafe)
        {
            OnUnsafeStarted?.Invoke();
        }

        wasUnsafe = isUnsafeNow;
    }
}
