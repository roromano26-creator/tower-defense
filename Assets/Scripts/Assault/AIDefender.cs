using System.Collections.Generic;
using UnityEngine;
using Bastion.Core;
using Bastion.Grid;
using Bastion.Towers;

namespace Bastion.Assault
{
    /// <summary>
    /// Défenseur IA du mode Assaut. Toutes les `assaultAiThinkInterval` secondes il construit ou améliore.
    /// Choix de la case : celle qui couvre le plus de chemin. Choix de la tour : pondéré par le prix,
    /// et par l'anti-aérien si le joueur envoie des volants. Un revenu passif l'empêche d'être affamé
    /// par un joueur qui n'envoie rien. Lisible et battable : c'est une IA de jeu, pas un adversaire parfait.
    /// </summary>
    public class AIDefender : MonoBehaviour
    {
        private LevelData level;
        private float nextThink;
        private float incomeAccumulator;
        private int flyingSeen, groundSeen;
        private readonly List<Vector2Int> candidates = new();
        private System.Random rng;

        private void Start()
        {
            if (!GameManager.Instance.IsAssault) { enabled = false; return; }
            level = GameManager.Instance.Level;
            rng = new System.Random(level.levelId.GetHashCode());
            GameEvents.OnEnemySpawned += e => { if (e.IsFlying) flyingSeen++; else groundSeen++; };
            // Pré-construction : quelques tours avant le premier monstre, pour que le joueur voie la défense à percer.
            for (int i = 0; i < 3; i++) TryBuild();
            nextThink = Time.time + level.assaultAiThinkInterval;
        }

        private void Update()
        {
            var state = GameManager.Instance.State;
            if (state != GameState.WaveRunning) return;
            incomeAccumulator += level.assaultAiIncomePerSecond * DifficultySettings.HealthMultiplier(GameManager.Instance.Difficulty) * Time.deltaTime;
            if (incomeAccumulator >= 1f) { int g = Mathf.FloorToInt(incomeAccumulator); incomeAccumulator -= g; ResourceManager.Instance.AddGold(g); }
            if (Time.time < nextThink) return;
            nextThink = Time.time + level.assaultAiThinkInterval;
            // Une amélioration une fois sur trois quand il y a de quoi, sinon une construction.
            if (rng.Next(3) == 0 && TryUpgrade()) return;
            if (!TryBuild()) TryUpgrade();
        }

        private bool TryBuild()
        {
            var tm = TowerManager.Instance; var grid = GridManager.Instance;
            var data = PickTower(tm.Catalog);
            if (data == null) return false;
            var cell = PickCell(grid, data.levels[0].range);
            if (cell == null) return false;
            return tm.TryBuild(data, cell.Value);
        }

        private TowerData PickTower(IReadOnlyList<TowerData> catalog)
        {
            int gold = ResourceManager.Instance.Gold;
            bool wantAntiAir = flyingSeen > groundSeen / 2 && flyingSeen > 0;
            var affordable = new List<TowerData>();
            foreach (var t in catalog)
                if (MetaProgression.Cost(t.levels[0].cost) <= gold && (!wantAntiAir || t.canTargetFlying)) affordable.Add(t);
            if (affordable.Count == 0 && wantAntiAir) foreach (var t in catalog) if (MetaProgression.Cost(t.levels[0].cost) <= gold) affordable.Add(t);
            if (affordable.Count == 0) return null;
            // Pondération par le prix : une tour chère est plus souvent la bonne réponse quand on peut se la payer.
            int total = 0; foreach (var t in affordable) total += t.levels[0].cost;
            int pick = rng.Next(total);
            foreach (var t in affordable) { pick -= t.levels[0].cost; if (pick < 0) return t; }
            return affordable[affordable.Count - 1];
        }

        /// <summary>Case libre couvrant le plus de waypoints à portée, avec un peu de hasard pour ne pas être prévisible.</summary>
        private Vector2Int? PickCell(GridManager grid, float range)
        {
            candidates.Clear();
            var l = grid.Level; float best = -1f; Vector2Int? bestCell = null;
            var wps = grid.Waypoints; float r2 = range * range;
            for (int x = 0; x < l.width; x++)
                for (int y = 0; y < l.height; y++)
                {
                    var c = new Vector2Int(x, y);
                    if (!grid.IsBuildable(c)) continue;
                    var w = grid.CellToWorld(c);
                    float score = 0f;
                    for (int i = 0; i < wps.Count; i++) { var d = wps[i] - w; d.y = 0; if (d.sqrMagnitude <= r2) score += 1f; }
                    if (score <= 0f) continue;
                    score += (float)rng.NextDouble() * 1.5f;
                    if (score > best) { best = score; bestCell = c; }
                }
            return bestCell;
        }

        private bool TryUpgrade()
        {
            var tm = TowerManager.Instance;
            Tower cheapest = null;
            foreach (var t in tm.Towers)
                if (t.CanUpgrade && t.UpgradeCost <= ResourceManager.Instance.Gold && (cheapest == null || t.UpgradeCost < cheapest.UpgradeCost)) cheapest = t;
            return cheapest != null && tm.TryUpgrade(cheapest);
        }
    }
}
