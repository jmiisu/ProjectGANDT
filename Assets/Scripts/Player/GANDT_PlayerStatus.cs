using System;
using UnityEngine;

public class GANDT_PlayerStatus : MonoBehaviour
{
    [Header("Status")]
    [Range(0f, 1f)]
    [SerializeField] private float mentalPower = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float sanity = 1f;

    public float MentalPower => mentalPower;
    public float Sanity => sanity;
    
    public bool IsMentalPowerDepleted => mentalPower <= 0f;
    public bool IsSanityDepleted => sanity <= 0f;

    public event Action<float> OnMentalPowerChanged;
    public event Action<float> OnSanityChanged;

    public event Action OnMentalPowerDepleted;
    public event Action OnSanityDepleted;

    private void OnValidate()
    {
        mentalPower = Mathf.Clamp01(mentalPower);
        sanity = Mathf.Clamp01(sanity);
    }


    public void AddMentalPower(float amount)
    {
        SetMentalPower(mentalPower + amount);
    }

    public void AddSanity(float amount)
    {
        SetSanity(sanity + amount);
    }

    public void SetMentalPower(float value)
    {
        mentalPower = Mathf.Clamp01(value);
        OnMentalPowerChanged?.Invoke(mentalPower);
        if (IsMentalPowerDepleted)
        {
            OnMentalPowerDepleted?.Invoke();
        }
    }

    public void SetSanity(float value)
    {
        sanity = Mathf.Clamp01(value);
        OnSanityChanged?.Invoke(sanity);
        if (IsSanityDepleted)
        {
            OnSanityDepleted?.Invoke();
        }
    }

    public void ResetStatus()
    {
        SetMentalPower(1f);
        SetSanity(1f);
    }
}
