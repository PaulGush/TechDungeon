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

        int previous = m_currentIndex;
        m_currentIndex = (m_currentIndex + direction + m_ammoTypes.Count) % m_ammoTypes.Count;

        if (m_currentIndex != previous)
            OnAmmoChanged?.Invoke(CurrentAmmoSettings);
    }

    public bool TryConsumeAmmo()
    {
        AmmoSettings current = CurrentAmmoSettings;
        if (current == null || current.Type == AmmoType.Standard) return true;

        if (m_mutationManager != null)
        {
            float efficiency = m_mutationManager.GetAmmoEfficiency();
            if (efficiency > 0f && UnityEngine.Random.value * 100f < efficiency)
                return true;
        }

        if (m_ammoCounts.TryGetValue(current.Type, out int count) && count > 0)
        {
            m_ammoCounts[current.Type] = count - 1;
            OnAmmoCountChanged?.Invoke(current.Type, count - 1);

            if (count - 1 <= 0)
                CycleToStandard();

            return true;
        }

        return false;
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

    private void CycleToStandard()
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
