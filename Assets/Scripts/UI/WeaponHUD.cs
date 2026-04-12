using PlayerObject;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponHolder m_weaponHolder;
    [SerializeField] private TextMeshProUGUI m_weaponNameText;
    [SerializeField] private Image m_weaponIcon;

    [Header("Settings")]
    [SerializeField] private string m_noWeaponText = "UNARMED";

    private void OnEnable()
    {
        m_weaponHolder.OnWeaponChanged += OnWeaponChanged;
        UpdateDisplay(m_weaponHolder.CurrentWeapon);
    }

    private void OnDisable()
    {
        m_weaponHolder.OnWeaponChanged -= OnWeaponChanged;
    }

    private void OnWeaponChanged(GameObject weapon)
    {
        UpdateDisplay(weapon);
    }

    private void UpdateDisplay(GameObject weapon)
    {
        if (weapon == null)
        {
            m_weaponNameText.text = m_noWeaponText;

            if (m_weaponIcon != null)
                m_weaponIcon.enabled = false;

            return;
        }

        m_weaponNameText.text = weapon.name.ToUpper();

        if (m_weaponIcon != null)
        {
            SpriteRenderer sr = weapon.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                m_weaponIcon.sprite = sr.sprite;
                m_weaponIcon.enabled = true;
            }
            else
            {
                m_weaponIcon.enabled = false;
            }
        }
    }
}
