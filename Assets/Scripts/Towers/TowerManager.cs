using System.Collections.Generic;
using UnityEngine;
using Bastion.Core;
using Bastion.Grid;
using Bastion.VFX;

namespace Bastion.Towers
{
    /// <summary>Catalogue des tours, achat / upgrade / vente, pools de projectiles.</summary>
    [DefaultExecutionOrder(-150)]
    public class TowerManager : MonoBehaviour
    {
        public static TowerManager Instance { get; private set; }

        [SerializeField] private TowerData[] catalog;
        [SerializeField] private Tower towerPrefab;      // prefab logique commun
        [SerializeField] private Transform towersRoot;
        [SerializeField] private Transform projectilesRoot;

        private readonly List<Tower> towers = new();
        private readonly Dictionary<Projectile, ObjectPool<Projectile>> projectilePools = new();
        private readonly Dictionary<Projectile, ObjectPool<Projectile>> instanceToPool = new();

        public IReadOnlyList<TowerData> Catalog => catalog;
        public IReadOnlyList<Tower> Towers => towers;

        private void Awake()
        {
            Instance = this;
            // Le niveau peut restreindre les tours proposées (courbe d'apprentissage) ; vide = tout le catalogue.
            var allowed = GameManager.Instance != null ? GameManager.Instance.Level.availableTowers : null;
            if (allowed != null && allowed.Length > 0) catalog = allowed;
        }

        public bool TryBuild(TowerData data, Vector2Int cell)
        {
            var grid = GridManager.Instance;
            if (!grid.IsBuildable(cell)) return false;
            if (!ResourceManager.Instance.TrySpend(MetaProgression.Cost(data.levels[0].cost))) return false;
            var pos = grid.CellToWorld(cell);
            var t = Instantiate(towerPrefab, pos, Quaternion.identity, towersRoot);
            t.name = $"Tower_{data.towerId}_{cell.x}_{cell.y}";
            t.Initialize(data, cell);
            grid.SetOccupant(cell, t);
            towers.Add(t);
            VFXManager.Instance.Play(VFXKind.Build, pos, data.accentColor);
            GameEvents.TowerPlaced(t);
            return true;
        }

        public bool TryUpgrade(Tower t)
        {
            if (t == null || !t.CanUpgrade) return false;
            if (!ResourceManager.Instance.TrySpend(t.UpgradeCost)) return false;
            t.Upgrade();
            GameEvents.TowerUpgraded(t);
            return true;
        }

        public void Sell(Tower t)
        {
            if (t == null) return;
            ResourceManager.Instance.AddGold(t.SellValue);
            GridManager.Instance.SetOccupant(t.Cell, null);
            towers.Remove(t);
            VFXManager.Instance.Play(VFXKind.Sell, t.transform.position, Color.yellow);
            GameEvents.TowerSold(t);
            Destroy(t.gameObject);
        }

        public Projectile GetProjectile(Projectile prefab, Vector3 at)
        {
            if (!projectilePools.TryGetValue(prefab, out var pool))
            {
                pool = new ObjectPool<Projectile>(prefab, projectilesRoot, 12);
                projectilePools[prefab] = pool;
            }
            var p = pool.Get(at, Quaternion.identity);
            instanceToPool[p] = pool;
            return p;
        }

        public void ReleaseProjectile(Projectile p)
        {
            if (instanceToPool.TryGetValue(p, out var pool)) pool.Release(p);
            else Destroy(p.gameObject);
        }
    }
}
