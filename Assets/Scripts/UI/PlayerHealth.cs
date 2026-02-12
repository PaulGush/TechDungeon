using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider m_slider;
    [SerializeField] private EntityHealth m_health;
    
    private void OnEnable()
    {
        m_health.OnHealthChanged += OnHealthChanged;
    }

    private void OnDisable()
    {
        m_health.OnHealthChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(int currentHealth)
    {
        m_slider.maxValue = m_health.MaxHealth;
        m_slider.value = currentHealth;
        m_slider.minValue = 0;
    }
}