using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemyWave
{
    public List<GameObject> EnemyPrefabs;
    [Tooltip("Delay in seconds before this wave spawns after the previous wave is cleared.")]
    public float DelayBeforeSpawn;
}
