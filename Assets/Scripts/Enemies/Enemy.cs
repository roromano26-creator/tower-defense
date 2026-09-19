using UnityEngine;
using Bastion.Core;
using Bastion.Grid;
using Bastion.VFX;

namespace Bastion.Enemies
{
    /// <summary>
    /// Un ennemi : vie, armure, résistances, statuts (ralenti, brûlure), suivi du chemin.
    /// Le visuel est un enfant « Visual » instancié depuis EnemyData.visualPrefab ; ce script ne
    /// manipule que son échelle et sa teinte de dégât via MaterialPropertyBlock (pas de copie de matériau).
    /// </summary>
    [RequireComponent(typeof(PathFollower))]
    public class Enemy : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform hitPoint;      // où les projectiles visent (au torse)
        [SerializeField] private HealthBar healthBar;

        public EnemyData Data { get; private set; }
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public bool IsAlive => Health > 0f;
        public bool IsFlying => Data.isFlying;
        public Vector3 HitPosition => hitPoint != null ? hitPoint.position : transform.position + Vector3.up * 0.8f;
        public float DistanceTravelled => follower.DistanceTravelled;

        private PathFollower follower;
        private Renderer[] renderers;
        private MaterialPropertyBlock mpb;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private float slowFactor = 1f, slowUntil;
        private float burnDps, burnUntil;
        private float flashUntil;
        private int goldReward;
        private System.Action<Enemy> onDespawn;

        private void Awake()
        {
            follower = GetComponent<PathFollower>();
            mpb = new MaterialPropertyBlock();
        }

        /// <summary>Appelé par le pool à chaque spawn ; healthMul et rewardMul viennent de la progression de vague.</summary>
        public void Spawn(EnemyData data, float healthMul, float rewardMul, System.Action<Enemy> despawnCallback)
        {
            Data = data;
            onDespawn = despawnCallback;
            MaxHealth = data.maxHealth * healthMul;
            Health = MaxHealth;
            goldReward = Mathf.RoundToInt(data.goldReward * rewardMul * GameManager.Instance.EnemyGoldMultiplier);
            slowFactor = 1f; slowUntil = 0; burnDps = 0; burnUntil = 0;
            EnsureVisual();
            follower.Begin(data.hoverHeight);
            if (healthBar != null) healthBar.Set(1f, data.isBoss);
            GameEvents.EnemySpawned(this);
        }

        private void EnsureVisual()
        {
            // Le visuel n'est instancié qu'une fois par instance poolée (même EnemyData par prefab logique).
            if (visualRoot.childCount == 0 && Data.visualPrefab != null)
                Instantiate(Data.visualPrefab, visualRoot);
            visualRoot.localScale = Vector3.one * Data.visualScale;
            renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        }

        private void Update()
        {
            if (!IsAlive) return;
            float dt = Time.deltaTime;
            if (Time.time > slowUntil) slowFactor = 1f;
            if (burnDps > 0f)
            {
                if (Time.time > burnUntil) burnDps = 0f;
                else ApplyRawDamage(burnDps * dt);
            }
            float speedMul = GameManager.Instance.IsAssault ? 1f : DifficultySettings.SpeedMultiplier(GameManager.Instance.Difficulty);   // en Assaut la difficulté renforce l'IA, pas vos monstres
            follower.Step(Data.speed * slowFactor * speedMul, dt);
            if (follower.ReachedEnd) ReachEnd();
            if (flashUntil > 0f && Time.time > flashUntil) { flashUntil = 0f; SetTint(Color.white, Color.black, true); }
        }

        public void TakeDamage(float amount, DamageType type)
        {
            if (!IsAlive) return;
            float dmg = amount * Data.MultiplierFor(type);
            if (type == DamageType.Physical) dmg = Mathf.Max(1f, dmg - Data.armor);
            ApplyRawDamage(dmg);
            SetTint(new Color(1f, 0.45f, 0.45f), new Color(0.6f, 0.05f, 0.05f));
            flashUntil = Time.time + 0.08f;
            UI.DamagePopup.Show(HitPosition, dmg, type);
        }

        private void ApplyRawDamage(float dmg)
        {
            Health -= dmg;
            if (healthBar != null) healthBar.Set(Mathf.Clamp01(Health / MaxHealth), Data.isBoss);
            if (Health <= 0f) Die();
        }

        public void ApplySlow(float factor, float duration)
        {
            if (Data.slowImmune) return;
            slowFactor = Mathf.Min(slowFactor, factor);   // le ralentissement le plus fort gagne
            slowUntil = Mathf.Max(slowUntil, Time.time + duration);
            VFXManager.Instance.Attach(VFXKind.FrostStatus, transform, duration);
        }

        public void ApplyBurn(float dps, float duration)
        {
            burnDps = Mathf.Max(burnDps, dps);
            burnUntil = Mathf.Max(burnUntil, Time.time + duration);
            VFXManager.Instance.Attach(VFXKind.BurnStatus, transform, duration);
        }

        private void Die()
        {
            Health = 0f;
            ResourceManager.Instance.AddGold(goldReward);
            VFXManager.Instance.Play(Data.isBoss ? VFXKind.BossDeath : VFXKind.Death, HitPosition, Data.accentColor);
            if (Data.isBoss) GameEvents.CameraShake(transform.position, 0.6f);
            GameEvents.EnemyKilled(this);
            Despawn();
        }

        private void ReachEnd()
        {
            Health = 0f;
            ResourceManager.Instance.LoseLives(Data.livesCost);
            GameEvents.CameraShake(transform.position, 0.25f);
            GameEvents.EnemyReachedEnd(this);
            Despawn();
        }

        private void Despawn()
        {
            SetTint(Color.white, Color.black, true);
            onDespawn?.Invoke(this);
        }

        /// <summary>Flash de dégât. `reset` rend le bloc de propriétés vide : le matériau reprend ses couleurs.</summary>
        private void SetTint(Color tint, Color emission, bool reset = false)
        {
            if (renderers == null) return;
            foreach (var r in renderers)
            {
                if (reset) { mpb.Clear(); r.SetPropertyBlock(mpb); continue; }
                var baseCol = r.sharedMaterial != null && r.sharedMaterial.HasProperty(BaseColorId) ? r.sharedMaterial.GetColor(BaseColorId) : Color.white;
                r.GetPropertyBlock(mpb);
                mpb.SetColor(BaseColorId, Color.Lerp(baseCol, tint, 0.65f));
                mpb.SetColor(EmissionId, emission);
                r.SetPropertyBlock(mpb);
            }
        }
    }
}
