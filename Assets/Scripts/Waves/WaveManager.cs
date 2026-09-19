using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bastion.Core;
using Bastion.Enemies;
using Bastion.Grid;

namespace Bastion.Waves
{
    /// <summary>
    /// Lit LevelData.waves, spawne les groupes, applique la progression de difficulté, gère le
    /// décompte automatique en phase de construction, et déclare la vague terminée quand le
    /// dernier ennemi est mort ou sorti.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        private LevelData level;
        private readonly List<WaveData> waves = new();   // scriptées, puis générées en mode Sans fin
        private System.Random rng;
        private int waveIndex = -1;           // index de la vague en cours (0-based), -1 avant la première
        private int pendingSpawns;
        private float countdown;
        private bool countingDown;

        public int CurrentWave => waveIndex + 1;
        public int TotalWaves => level != null ? level.waves.Length : 0;
        public WaveData CurrentWaveData => waveIndex >= 0 && waveIndex < waves.Count ? waves[waveIndex] : null;
        public WaveData NextWaveData => waveIndex + 1 < waves.Count ? waves[waveIndex + 1] : (GameManager.Instance.Endless ? EnsureGenerated(waveIndex + 1) : null);
        public bool IsBossWave => CurrentWaveData != null && CurrentWaveData.isBossWave;
        public bool IsEndlessWave => waveIndex >= TotalWaves;

        private void Awake() { Instance = this; }

        private void Start()
        {
            level = GameManager.Instance.Level;
            if (GameManager.Instance.IsAssault) { enabled = false; return; }   // en Assaut, c'est le joueur qui spawne
            waves.AddRange(level.waves);
            rng = new System.Random(level.levelId.GetHashCode() ^ (int)GameManager.Instance.Difficulty);   // déterministe par niveau/difficulté : un record est comparable
            StartCountdown();
        }

        private void OnEnable()
        {
            GameEvents.OnEnemyKilled += HandleEnemyGone;
            GameEvents.OnEnemyReachedEnd += HandleEnemyGone;
            GameEvents.OnStateChanged += HandleState;
        }

        private void OnDisable()
        {
            GameEvents.OnEnemyKilled -= HandleEnemyGone;
            GameEvents.OnEnemyReachedEnd -= HandleEnemyGone;
            GameEvents.OnStateChanged -= HandleState;
        }

        private void HandleState(GameState s)
        {
            if (s == GameState.Building && waveIndex >= 0) StartCountdown();
        }

        private void StartCountdown()
        {
            if (level.autoStartDelay <= 0f || NextWaveData == null) { countingDown = false; return; }
            countdown = level.autoStartDelay;
            countingDown = true;
            GameEvents.WaveCountdown(countdown);
        }

        private void Update()
        {
            if (!countingDown || GameManager.Instance.State != GameState.Building) return;
            countdown -= Time.deltaTime;
            GameEvents.WaveCountdown(Mathf.Max(0f, countdown));
            if (countdown <= 0f) { countingDown = false; GameManager.Instance.StartNextWave(); }
        }

        public void StartNextWave()
        {
            if (NextWaveData == null) return;
            countingDown = false;
            waveIndex++;
            pendingSpawns = CurrentWaveData.TotalEnemies();
            GameEvents.WaveStarted(CurrentWave, TotalWaves);
            StartCoroutine(RunWave(CurrentWaveData));
        }

        private IEnumerator RunWave(WaveData wave)
        {
            var diff = GameManager.Instance.Difficulty;
            float healthMul = (1f + waveIndex * level.healthScalingPerWave) * DifficultySettings.HealthMultiplier(diff);
            float rewardMul = (1f + waveIndex * level.rewardScalingPerWave) * DifficultySettings.RewardMultiplier(diff);
            // Au-delà du scénario, la vie grimpe plus vite que l'or : le mode Sans fin doit finir par gagner.
            int beyond = waveIndex - TotalWaves + 1;
            if (beyond > 0) { healthMul *= 1f + beyond * 0.08f; rewardMul *= 1f + beyond * 0.02f; }
            foreach (var g in wave.groups)
            {
                if (g.delayBefore > 0f) yield return new WaitForSeconds(g.delayBefore);
                for (int i = 0; i < g.count; i++)
                {
                    EnemyManager.Instance.Spawn(g.enemy, g.enemy.isBoss ? healthMul * 1.5f : healthMul, rewardMul);
                    pendingSpawns--;
                    if (i < g.count - 1) yield return new WaitForSeconds(g.interval);
                }
            }
            CheckWaveEnd();
        }

        private void HandleEnemyGone(Enemy _) => CheckWaveEnd();

        /// <summary>
        /// Vague générée : un budget de menace croissant dépensé en groupes tirés dans le pool du niveau,
        /// un boss du pool toutes les `bossEvery` vagues. Sans asset ni scénario : la durée de vie vient d'ici.
        /// </summary>
        private WaveData EnsureGenerated(int index)
        {
            while (waves.Count <= index) waves.Add(Generate(waves.Count));
            return waves[index];
        }

        private WaveData Generate(int index)
        {
            int n = index - TotalWaves + 1;                       // 1 pour la première vague générée
            var w = ScriptableObject.CreateInstance<WaveData>();
            w.name = $"Endless_{n}";
            w.title = $"Sans fin · {n}";
            var pool = level.endlessPool != null && level.endlessPool.Length > 0 ? level.endlessPool : DefaultPool();
            int budget = level.endlessBudgetBase + n * level.endlessBudgetGrowth;
            var groups = new List<SpawnGroup>();
            int guard = 0;
            while (budget > 0 && guard++ < 12)
            {
                var e = pool[rng.Next(pool.Length)];
                int cost = Mathf.Max(1, e.threat);
                int count = Mathf.Clamp(budget / cost, 1, 14);
                count = Mathf.Max(1, count / (rng.Next(2) + 1));   // ne dépense pas tout sur un seul groupe
                groups.Add(new SpawnGroup { enemy = e, count = count, interval = Mathf.Max(0.3f, 1.1f - n * 0.02f), delayBefore = groups.Count == 0 ? 0f : 1.5f });
                budget -= count * cost;
            }
            bool boss = level.bossEvery > 0 && n % level.bossEvery == 0 && level.endlessBossPool != null && level.endlessBossPool.Length > 0;
            if (boss)
            {
                var b = level.endlessBossPool[rng.Next(level.endlessBossPool.Length)];
                groups.Add(new SpawnGroup { enemy = b, count = 1 + n / 15, interval = 5f, delayBefore = 3f });
                w.title = $"Sans fin · {n} — {b.displayName}";
            }
            w.isBossWave = boss;
            w.groups = groups.ToArray();
            return w;
        }

        /// <summary>Sans pool défini, on réutilise les ennemis rencontrés dans le scénario.</summary>
        private EnemyData[] DefaultPool()
        {
            var set = new HashSet<EnemyData>();
            foreach (var w in level.waves) foreach (var g in w.groups) if (g.enemy != null && !g.enemy.isBoss) set.Add(g.enemy);
            var arr = new EnemyData[set.Count]; set.CopyTo(arr); return arr;
        }

        private void CheckWaveEnd()
        {
            if (GameManager.Instance.State != GameState.WaveRunning) return;
            if (pendingSpawns > 0 || EnemyManager.Instance.AliveCount > 0) return;
            int bonus = level.waveClearBonus + (IsBossWave ? level.waveClearBonus : 0);
            if (IsEndlessWave) bonus = Mathf.RoundToInt(bonus * 1.5f);
            ResourceManager.Instance.AddGold(bonus);
            GameEvents.WaveCompleted(CurrentWave);
        }
    }
}
