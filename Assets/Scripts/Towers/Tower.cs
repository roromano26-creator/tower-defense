using System.Collections.Generic;
using UnityEngine;
using Bastion.Core;
using Bastion.Enemies;
using Bastion.VFX;

namespace Bastion.Towers
{
    /// <summary>
    /// Une tour posée : niveau courant, ciblage, cadence, tir. Le comportement d'attaque est choisi
    /// par TowerData.attackKind ; le visuel de chaque niveau est un prefab enfant de « Visual » qui
    /// expose (optionnellement) un enfant nommé « FirePoint » et un enfant « Turret » qui pivote.
    /// </summary>
    public class Tower : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private RangeIndicator rangeIndicator;
        [SerializeField] private AuraRing aura;

        public TowerData Data { get; private set; }
        public int LevelIndex { get; private set; }
        public TowerLevel Level => Data.levels[LevelIndex];
        public bool CanUpgrade => LevelIndex < Data.levels.Length - 1;
        public int UpgradeCost => CanUpgrade ? MetaProgression.Cost(Data.levels[LevelIndex + 1].cost) : 0;
        public int SellValue => Mathf.RoundToInt(MetaProgression.Cost(Data.TotalInvested(LevelIndex)) * Mathf.Min(1f, Data.sellRefund + MetaProgression.SellRefundBonus));
        public Vector2Int Cell { get; private set; }
        public TargetPriority Priority { get; set; }

        private Transform firePoint, turret;
        private float cooldown;
        private readonly List<Enemy> buffer = new(32);
        private Enemy target;

        public void Initialize(TowerData data, Vector2Int cell)
        {
            Data = data; Cell = cell; LevelIndex = 0;
            Priority = data.defaultPriority;
            ApplyVisual();
            if (aura != null) aura.SetColor(data.accentColor);
            SetSelected(false);
        }

        public bool Upgrade()
        {
            if (!CanUpgrade) return false;
            LevelIndex++;
            ApplyVisual();
            VFXManager.Instance.Play(VFXKind.Upgrade, transform.position, Data.accentColor);
            return true;
        }

        private void ApplyVisual()
        {
            for (int i = visualRoot.childCount - 1; i >= 0; i--) Destroy(visualRoot.GetChild(i).gameObject);
            var prefab = Level.visualPrefab;
            if (prefab != null)
            {
                var v = Instantiate(prefab, visualRoot);
                firePoint = FindDeep(v.transform, "FirePoint");
                turret = FindDeep(v.transform, "Turret");
            }
            if (firePoint == null) firePoint = visualRoot;
            if (rangeIndicator != null) rangeIndicator.SetRadius(Level.range);
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++) { var f = FindDeep(root.GetChild(i), name); if (f != null) return f; }
            return null;
        }

        public void SetSelected(bool on) { if (rangeIndicator != null) rangeIndicator.Show(on); }

        private void Update()
        {
            if (GameManager.Instance.State != GameState.WaveRunning) return;
            cooldown -= Time.deltaTime;
            if (target == null || !target.IsAlive || !InRange(target)) target = AcquireTarget();
            if (target == null) return;
            if (turret != null)
            {
                var dir = target.transform.position - turret.position; dir.y = 0;
                if (dir.sqrMagnitude > 0.001f)
                    turret.rotation = Quaternion.Slerp(turret.rotation, Quaternion.LookRotation(dir), 12f * Time.deltaTime);
            }
            if (cooldown <= 0f) { Fire(); cooldown = 1f / Level.fireRate; }
        }

        private bool InRange(Enemy e)
        {
            var d = e.transform.position - transform.position; d.y = 0;
            return d.sqrMagnitude <= Level.range * Level.range;
        }

        private Enemy AcquireTarget()
        {
            int n = EnemyManager.Instance.GetInRange(transform.position, Level.range, Data.canTargetGround, Data.canTargetFlying, buffer);
            return n == 0 ? null : TowerTargeting.Pick(buffer, Priority, transform.position);
        }

        private void Fire()
        {
            var origin = firePoint.position;
            VFXManager.Instance.Play(VFXKind.MuzzleFlash, origin, Data.accentColor, 0.8f);
            if (Data.hitscan || Data.projectilePrefab == null)
            {
                VFXManager.Instance.Beam(origin, target.HitPosition, Data.accentColor);
                ApplyHit(target, target.HitPosition);
            }
            else
            {
                var p = TowerManager.Instance.GetProjectile(Data.projectilePrefab, origin);
                p.Launch(target, Data.projectileSpeed, Data.accentColor, ApplyHit);
            }
        }

        /// <summary>Point d'impact : applique dégâts, zone et statuts selon le type d'attaque.</summary>
        private void ApplyHit(Enemy primary, Vector3 at)
        {
            var lvl = Level;
            float dmgMul = MetaProgression.DamageMultiplier;
            switch (Data.attackKind)
            {
                case AttackKind.Direct:
                case AttackKind.Sniper:
                    if (primary != null && primary.IsAlive) primary.TakeDamage(lvl.damage * dmgMul, Data.damageType);
                    VFXManager.Instance.Play(VFXKind.Impact, at, Data.accentColor);
                    break;
                case AttackKind.AreaOfEffect:
                    HitArea(at, lvl, e => e.TakeDamage(lvl.damage * dmgMul, Data.damageType));
                    VFXManager.Instance.Play(VFXKind.ImpactArea, at, Data.accentColor, lvl.splashRadius);
                    GameEvents.CameraShake(at, 0.12f);
                    break;
                case AttackKind.Slow:
                    HitArea(at, lvl, e => { e.TakeDamage(lvl.damage * dmgMul, Data.damageType); e.ApplySlow(lvl.slowFactor, lvl.effectDuration); });
                    VFXManager.Instance.Play(VFXKind.ImpactArea, at, Data.accentColor, Mathf.Max(0.6f, lvl.splashRadius));
                    break;
                case AttackKind.Burn:
                    HitArea(at, lvl, e => { e.TakeDamage(lvl.damage * dmgMul, Data.damageType); e.ApplyBurn(lvl.dotDamagePerSecond * dmgMul, lvl.effectDuration); });
                    VFXManager.Instance.Play(VFXKind.ImpactArea, at, Data.accentColor, lvl.splashRadius);
                    break;
            }
        }

        private void HitArea(Vector3 at, TowerLevel lvl, System.Action<Enemy> apply)
        {
            float r = Mathf.Max(0.3f, lvl.splashRadius);
            int n = EnemyManager.Instance.GetInRange(at, r, Data.canTargetGround, Data.canTargetFlying, buffer);
            for (int i = 0; i < n; i++) apply(buffer[i]);
        }
    }
}
