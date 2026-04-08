using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Entity/Boss Settings")]
public class BossSettings : EnemySettings
{
    [Header("Boss Phases")]
    public List<BossPhase> Phases = new List<BossPhase>();
}
