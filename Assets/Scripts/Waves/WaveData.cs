using UnityEngine;
using Bastion.Enemies;

namespace Bastion.Waves
{
    /// <summary>Un groupe d'ennemis identiques spawné en rafale.</summary>
    [System.Serializable]
    public class SpawnGroup
    {
        public EnemyData enemy;
        public int count = 5;
        [Tooltip("Secondes entre deux ennemis du groupe.")] public float interval = 0.8f;
        [Tooltip("Secondes d'attente avant ce groupe (après le précédent).")] public float delayBefore = 0f;
    }

    [CreateAssetMenu(menuName = "Bastion/Vague", fileName = "Wave_")]
    public class WaveData : ScriptableObject
    {
        public string title = "Éclaireurs";
        public bool isBossWave = false;
        public SpawnGroup[] groups;

        public int TotalEnemies()
        {
            int n = 0;
            if (groups != null) foreach (var g in groups) n += g.count;
            return n;
        }
    }
}
