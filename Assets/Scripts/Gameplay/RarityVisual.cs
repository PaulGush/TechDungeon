using System;
using UnityEngine;

public class RarityVisual : MonoBehaviour
{
    private static readonly int OutlineThickness = Shader.PropertyToID("_OutlineThickness");
    private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");

    [Header("References")] 
    [SerializeField] private Lootable m_lootable;
    [SerializeField] private Renderer m_renderer;
    [SerializeField] private TrailRenderer m_trail;

    private LootableRarity.Rarity m_rarity;

    private void OnEnable()
    {
        m_lootable.OnRarityChanged += ChangeRarity;
    }

    private void OnDisable()
    {
        m_lootable.OnRarityChanged -= ChangeRarity;
    }

    private void ChangeRarity(LootableRarity.Rarity newValue)
    {
        m_rarity = newValue;
        Color newColor = LootableRarity.RarityColors[m_rarity];
        SetOutlineColor(newColor);
        SetTrailColor(newColor);
    }

    private void SetOutlineColor(Color newColor)
    {
        m_renderer.material.SetColor(OutlineColor ,newColor);
    }

    public void SetOutlineThickness(float newValue)
    {
        m_renderer.material.SetFloat(OutlineThickness, newValue);
    }

    public void SetTrailColor(Color newColor)
    {
        m_trail.startColor = newColor;
        m_trail.endColor = newColor;
    }
}