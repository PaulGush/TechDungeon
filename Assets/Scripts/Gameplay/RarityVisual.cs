using UnityEngine;

public class RarityVisual : MonoBehaviour
{
    private static readonly int OutlineThickness = Shader.PropertyToID("_OutlineThickness");
    private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");
    private static readonly int PixelSizeID = Shader.PropertyToID("_PixelSize");

    [Header("References")]
    [SerializeField] private Lootable m_lootable;
    [SerializeField] private SpriteRenderer m_renderer;
    [SerializeField] private TrailRenderer m_trail;

    private LootableRarity.Rarity m_rarity;

    private void OnEnable()
    {
        m_lootable.OnRarityChanged += ChangeRarity;
        m_lootable.OnSpawnComplete += SpawnComplete;
    }

    private void OnDisable()
    {
        m_lootable.OnRarityChanged -= ChangeRarity;
        m_lootable.OnSpawnComplete -= SpawnComplete;
    }

    private void ChangeRarity(LootableRarity.Rarity newValue)
    {
        m_rarity = newValue;
        Color newColor = LootableRarity.RarityColors[m_rarity];
        SetOutlineColor(newColor);
        SetTrailColor(newColor);
        UpdateTexelSize();
    }

    private void UpdateTexelSize()
    {
        Texture tex = m_renderer.sprite != null ? m_renderer.sprite.texture : null;
        if (tex == null) return;
        m_renderer.material.SetVector(PixelSizeID, new Vector4(1f / tex.width, 1f / tex.height, tex.width, tex.height));
    }

    private void SetOutlineColor(Color newColor)
    {
        m_renderer.material.SetColor(OutlineColor, newColor);
    }

    public void SetOutlineThickness(float newValue)
    {
        m_renderer.material.SetFloat(OutlineThickness, newValue);
    }

    private void SetTrailColor(Color newColor)
    {
        m_trail.startColor = newColor;
        m_trail.endColor = newColor;
    }

    private void SpawnComplete()
    {
        m_trail.emitting = false;
    }
}