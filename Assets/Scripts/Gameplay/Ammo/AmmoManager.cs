using System;
using System.Collections.Generic;
using Input;
using UnityEngine;
using UnityServiceLocator;

public class AmmoManager : MonoBehaviour
{
    [SerializeField] private InputReader m_inputReader;
    [SerializeField] private List<AmmoSettings> m_ammoTypes;

    private readonly Dictionary<AmmoType, int> m_ammoCounts = new();
    private int m_currentIndex;
    private MutationManager m_mutationManager;

    public AmmoSettings CurrentAmmoSettings => m_ammoTypes.Count > 0 ? m_ammoTypes[m_currentIndex] : null;
    public Action<AmmoSettings> OnAmmoChanged;
    public Action<AmmoType, int> OnAmmoCountChanged;

    public void Reset()
    {
        m_ammoCounts.Clear();
        CycleToStandard();
    }

    private void Awake()
    {
        ServiceLocator.Global.Register(this);
    }

    private void Start()
    {
        ServiceLocator.Global.TryGet(out m_mutationManager);
    }

    private void OnEnable()
    {
        m_inputReader.Next += NextAmmo;
        m_inputReader.Previous += PreviousAmmo;
    }

    private void OnDisable()
    {
        m_inputReader.Next -= NextAmmo;
        m_inputReader.Previous -= PreviousAmmo;
    }

    private void NextAmmo() => CycleAmmo(1);
    private void PreviousAmmo() => CycleAmmo(-1);

    private void CycleAmmo(int direction)
    {
        if (m_ammoTypes.Count == 0) return;

        int start = m_currentIndex;
        int idx = start;
        for (int i = 0; i < m_ammoTypes.Count; i++)
        {
            idx = (idx + direction + m_ammoTypes.Count) % m_ammoTypes.Count;
            if (HasAvailableAmmo(m_ammoTypes[idx])) break;
        }

        if (idx == start) return;

        m_currentIndex = idx;
        OnAmmoChanged?.Invoke(CurrentAmmoSettings);
    }

    private bool HasAvailableAmmo(AmmoSettings settings)
    {
        if (settings == null) return false;
        if (settings.Type == AmmoType.Standard) return true;
        return m_ammoCounts.TryGetValue(settings.Type, out int count) && count > 0;
    }

    public bool RollAmmoEfficiency()
    {
        if (m_mutationManager == null) return false;
        float efficiency = m_mutationManager.GetAmmoEfficiency();
        return efficiency > 0f && UnityEngine.Random.value * 100f < efficiency;
    }

    public int TryDrawFromPool(AmmoType type, int maxAmount)
    {
        if (maxAmount <= 0) return 0;
        if (type == AmmoType.Standard) return maxAmount;

        if (!m_ammoCounts.TryGetValue(type, out int count) || count <= 0) return 0;

        int drawn = Mathf.Min(maxAmount, count);
        m_ammoCounts[type] = count - drawn;
        OnAmmoCountChanged?.Invoke(type, count - drawn);
        return drawn;
    }

    public void ReturnToPool(AmmoType type, int amount)
    {
        if (amount <= 0 || type == AmmoType.Standard) return;

        if (!m_ammoCounts.ContainsKey(type))
            m_ammoCounts[type] = 0;

        m_ammoCounts[type] += amount;
        OnAmmoCountChanged?.Invoke(type, m_ammoCounts[type]);
    }

    public AmmoSettings GetSettingsForType(AmmoType type)
    {
        for (int i = 0; i < m_ammoTypes.Count; i++)
        {
            if (m_ammoTypes[i].Type == type) return m_ammoTypes[i];
        }
        return null;
    }


    public void AddAmmo(AmmoType type, int amount)
    {
        if (!m_ammoCounts.ContainsKey(type))
            m_ammoCounts[type] = 0;

        m_ammoCounts[type] += amount;
        OnAmmoCountChanged?.Invoke(type, m_ammoCounts[type]);

        CycleToType(type);
    }

    private void CycleToType(AmmoType type)
    {
        for (int i = 0; i < m_ammoTypes.Count; i++)
        {
            if (m_ammoTypes[i].Type == type)
            {
                if (m_currentIndex != i)
                {
                    m_currentIndex = i;
                    OnAmmoChanged?.Invoke(CurrentAmmoSettings);
                }
                return;
            }
        }
    }

    public int GetAmmoCount(AmmoType type)
    {
        return m_ammoCounts.TryGetValue(type, out int count) ? count : 0;
    }

    public void CycleToStandard()
    {
        for (int i = 0; i < m_ammoTypes.Count; i++)
        {
            if (m_ammoTypes[i].Type == AmmoType.Standard)
            {
                m_currentIndex = i;
                OnAmmoChanged?.Invoke(CurrentAmmoSettings);
                return;
            }
        }
    }
}
