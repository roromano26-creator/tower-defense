using System.Collections.Generic;
using UnityEngine;
using Bastion.Core;
using Bastion.Enemies;
using Bastion.Grid;

namespace Bastion.Assault
{
    /// <summary>
    /// Mode Assaut : le joueur a de la mana qui se régénère et un chrono ; chaque monstre coûte
    /// `threat × assaultManaPerThreat`. Les vies du défenseur (ResourceManager) sont l'objectif.
    /// Les monstres tués rapportent de l'or à l'IA : spammer des gobelins nourrit la défense.
    /// </summary>
    [DefaultExecutionOrder(-120)]
    public class AssaultController : MonoBehaviour
    {
        public static AssaultController Instance { get; private set; }

        private LevelData level;
        private float mana, cap, regen;
        private readonly Dictionary<EnemyData, float> cooldownUntil = new();
        private bool running;

        public float Mana => mana;
        public float ManaCap => cap;
        public float TimeLeft { get; private set; }
        public bool Running => running;
        public IReadOnlyList<EnemyData> Pool { get; private set; }

        private void Awake() { Instance = this; }

        public void Begin()
        {
            level = GameManager.Instance.Level;
            cap = level.assaultManaCap;
            regen = level.assaultManaRegen * MetaProgression.ManaRegenMultiplier;
            mana = cap * 0.5f;
            TimeLeft = level.assaultDuration;
            Pool = BuildPool();
            GameEvents.ManaChanged(mana, cap);
            GameEvents.AssaultTimer(TimeLeft);
            // L'IA pré-construit pendant trois secondes de « Building », puis l'assaut commence.
            Invoke(nameof(Launch), 3f);
        }

        private void Launch()
        {
            running = true;
            GameManager.Instance.SetState(GameState.WaveRunning);
        }

        private List<EnemyData> BuildPool()
        {
            var list = new List<EnemyData>();
            if (level.assaultPool != null && level.assaultPool.Length > 0) list.AddRange(level.assaultPool);
            else
            {
                if (level.endlessPool != null) list.AddRange(level.endlessPool);
                if (level.endlessBossPool != null) list.AddRange(level.endlessBossPool);
            }
            list.RemoveAll(e => e == null);
            list.Sort((a, b) => a.threat.CompareTo(b.threat));
            return list;
        }

        public int CostOf(EnemyData e) => Mathf.CeilToInt(e.threat * level.assaultManaPerThreat);
        public bool OnCooldown(EnemyData e, out float remaining)
        {
            remaining = cooldownUntil.TryGetValue(e, out var t) ? Mathf.Max(0f, t - Time.time) : 0f;
            return remaining > 0f;
        }
        public bool CanDeploy(EnemyData e) => running && mana >= CostOf(e) && !OnCooldown(e, out _);

        /// <summary>Déploie un monstre à l'entrée du chemin. Retourne faux si la mana ou le cooldown l'interdit.</summary>
        public bool Deploy(EnemyData e)
        {
            if (!CanDeploy(e)) return false;
            mana -= CostOf(e);
            if (e.isBoss) cooldownUntil[e] = Time.time + level.assaultBossCooldown;
            float healthMul = MetaProgression.MonsterHealthMultiplier;
            float rewardMul = DifficultySettings.RewardMultiplier(GameManager.Instance.Difficulty);   // ce que l'IA touche par mort
            EnemyManager.Instance.Spawn(e, healthMul, rewardMul);
            GameEvents.ManaChanged(mana, cap);
            return true;
        }

        private void Update()
        {
            if (!running || GameManager.Instance.State != GameState.WaveRunning) return;
            float dt = Time.deltaTime;
            mana = Mathf.Min(cap, mana + regen * dt);
            GameEvents.ManaChanged(mana, cap);
            TimeLeft -= dt;
            GameEvents.AssaultTimer(Mathf.Max(0f, TimeLeft));
            if (TimeLeft <= 0f) { running = false; GameManager.Instance.AssaultTimeOut(); }
        }
    }
}
