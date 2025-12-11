using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider _slider;
    [SerializeField] private EntityHealth _health;
    
    private void OnEnable()
    {
        _health.OnHealthChanged += OnHealthChanged;
    }

    private void OnDisable()
    {
        _health.OnHealthChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(int currentHealth)
    {
        _slider.maxValue = _health.MaxHealth;
        _slider.value = currentHealth;
        _slider.minValue = 0;
    }
}