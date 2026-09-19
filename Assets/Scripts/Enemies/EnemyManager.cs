using System.Collections.Generic;
using UnityEngine;
using Bastion.Core;


namespace Bastion.Enemies
{
    /// <summary>
    /// Registre des ennemis vivants + pools par EnemyData. Les tours interrogent ce registre
    /// (distance au carré, pas de physique) : c'est la boucle la plus chaude du jeu.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public class EnemyManager : MonoBehaviour
    {
        public static EnemyManager Instance { get; private set; }

        [SerializeField] private Enemy enemyPrefab;       // prefab logique commun ; le visuel vient de EnemyData
        [SerializeField] private Transform poolRoot;

        private readonly List<Enemy> alive = new();
        private readonly Dictionary<EnemyData, ObjectPool<Enemy>> pools = new();

        public IReadOnlyList<Enemy> Alive => alive;
        public int AliveCount => alive.Count;

        private void Awake() { Instance = this; }

        public Enemy Spawn(EnemyData data, float healthMul, float rewardMul)
        {
            if (!pools.TryGetValue(data, out var pool))
            {
                // Un pool par type : chaque instance poolée garde le visuel de son EnemyData.
                var root = new GameObject($"Pool_{data.enemyId}").transform;
                root.SetParent(poolRoot, false);
                pool = new ObjectPool<Enemy>(enemyPrefab, root, 4);
                pools[data] = pool;
            }
            var e = pool.Get(Vector3.zero, Quaternion.identity);
            alive.Add(e);
            e.Spawn(data, healthMul, rewardMul, ReturnToPool);
            return e;
        }

        private void ReturnToPool(Enemy e)
        {
            alive.Remove(e);
            pools[e.Data].Release(e);
        }

        /// <summary>Remplit `buffer` avec les ennemis à portée ; retourne le nombre trouvé.</summary>
        public int GetInRange(Vector3 center, float range, bool ground, bool flying, List<Enemy> buffer)
        {
            buffer.Clear();
            float r2 = range * range;
            for (int i = 0; i < alive.Count; i++)
            {
                var e = alive[i];
                if (!e.IsAlive) continue;
                if (e.IsFlying ? !flying : !ground) continue;
                var d = e.transform.position - center; d.y = 0;
                if (d.sqrMagnitude <= r2) buffer.Add(e);
            }
            return buffer.Count;
        }

        public void KillAll()
        {
            for (int i = alive.Count - 1; i >= 0; i--) alive[i].TakeDamage(float.MaxValue, DamageType.Magic);
        }
    }
}
