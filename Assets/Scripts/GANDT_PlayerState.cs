using System;
using UnityEngine;

public class GANDT_PlayerState : MonoBehaviour
{
    [Header("Player Actions")]
    [SerializeField] private GANDT_EyeClosing eyeClosing;
    [SerializeField] private GANDT_Crying crying;

    public bool IsEyesClosed => 
        eyeClosing != null && eyeClosing.IsActive;

    public float EyeClosedDuration =>
        eyeClosing != null ? eyeClosing.Duration : 0f;

    public bool IsEyeClosingUnsafe =>
        eyeClosing != null && eyeClosing.IsUnsafe;

    public bool IsCrying =>
        crying != null && crying.IsActive;

    public float CryingProgressNormalized =>
        crying != null ? crying.ProgressNormalized : 0f;

    public bool IsCryingOnCooldown =>
        crying != null && crying.IsOnCooldown;

    public event Action OnCryingStarted
    {
        add
        { 
            crying.OnStarted += value;
        }
        remove
        {
            crying.OnStarted -= value;
        }
    }

    public event Action OnCryingCancelled
    {
        add
        {
            crying.OnCancelled += value;
        }
        remove
        {
            crying.OnCancelled -= value;
        }
    }

    public event Action OnCryingCompleted
    {
        add
        {
            crying.OnCompleted += value;
        }
        remove
        {
            crying.OnCompleted -= value;            
        }
    }

    private void Reset()
    {
        FindComponents();
    }

    private void Awake()
    {
        FindComponents();
    }

    private void FindComponents()
    {
        if (eyeClosing == null)
        {
            eyeClosing = GetComponent<GANDT_EyeClosing>();
        }

        if (crying == null)
        {
            crying = GetComponent<GANDT_Crying>();
        }
    }
}