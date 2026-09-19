using UnityEngine;
using Bastion.Waves;
using Bastion.Enemies;

namespace Bastion.Grid
{
    public enum CellType { Buildable, Path, Blocked }

    /// <summary>
    /// Un niveau : dimensions de la grille, chemin (liste de cellules dans l'ordre), vagues, économie.
    /// Le chemin est dessiné en coordonnées de cellules ; GridManager en déduit les waypoints monde.
    /// </summary>
    [CreateAssetMenu(menuName = "Bastion/Niveau", fileName = "Level_")]
    public class LevelData : ScriptableObject
    {
        public string levelId = "level_01";
        public string displayName = "La Vallée";
        [TextArea] public string description = "Un chemin en S entre trois collines.";
        [Tooltip("Étoiles totales requises pour jouer ce niveau (en plus d'avoir fini le précédent).")] public int starsToUnlock = 0;
        [Tooltip("Tours proposées dans ce niveau. Vide = tout le catalogue.")] public Towers.TowerData[] availableTowers;

        [Header("Biome (visuel)")]
        public Color buildableColor = new(0.2f, 0.29f, 0.37f);
        public Color pathColor = new(0.78f, 0.64f, 0.42f);
        public Color blockedColor = new(0.23f, 0.23f, 0.28f);
        public Color sunColor = new(1f, 0.95f, 0.84f);
        public Color ambientSky = new(0.36f, 0.42f, 0.62f);
        public Color ambientGround = new(0.09f, 0.09f, 0.12f);
        public Color fogColor = new(0.07f, 0.07f, 0.1f);
        [Tooltip("Matériaux de dalle optionnels (assets importés) ; sinon la couleur du biome teinte les dalles générées.")]
        public Material buildableMaterial, pathMaterial, blockedMaterial;

        [Header("Grille")]
        public int width = 14;
        public int height = 10;
        public float cellSize = 2f;

        [Tooltip("Cellules du chemin dans l'ordre de parcours (entrée → sortie). Un tour par cellule, pas de diagonale.")]
        public Vector2Int[] path;

        [Tooltip("Cellules décoratives inconstructibles (rochers, eau).")]
        public Vector2Int[] blocked;

        [Header("Économie")]
        public int startingGold = 150;
        public int startingLives = 20;
        public int waveClearBonus = 25;

        [Header("Vagues")]
        public WaveData[] waves;
        [Tooltip("Secondes de préparation avant le lancement automatique de la vague suivante (0 = manuel).")]
        public float autoStartDelay = 25f;
        [Tooltip("Une vague sur N est une vague boss (le WaveData la marque quand même explicitement).")]
        public int bossEvery = 5;

        [Header("Mode Sans fin (après la dernière vague scriptée)")]
        public bool endlessEnabled = true;
        [Tooltip("Ennemis tirés au sort dans les vagues générées.")] public EnemyData[] endlessPool;
        [Tooltip("Boss tirés au sort toutes les bossEvery vagues.")] public EnemyData[] endlessBossPool;
        [Tooltip("Budget de menace de la première vague générée ; chaque ennemi coûte son `threat`.")] public int endlessBudgetBase = 30;
        [Tooltip("Budget ajouté par vague générée.")] public int endlessBudgetGrowth = 6;
        [Tooltip("Gemmes gagnées tous les 5 paliers du mode Sans fin.")] public int endlessGemsPerMilestone = 5;

        [Header("Mode Assaut (le joueur déploie les monstres)")]
        [Tooltip("Durée de l'assaut en secondes ; à zéro, le défenseur a tenu.")] public float assaultDuration = 300f;
        public float assaultManaRegen = 5f;
        public float assaultManaCap = 150f;
        [Tooltip("Mana par point de menace : coût d'un monstre = threat × ce facteur.")] public float assaultManaPerThreat = 6f;
        [Tooltip("Secondes entre deux déploiements d'un même boss.")] public float assaultBossCooldown = 45f;
        [Tooltip("Monstres déployables ; vide = pool Sans fin + boss.")] public EnemyData[] assaultPool;
        public int assaultAiStartingGold = 260;
        [Tooltip("Or gagné par l'IA chaque seconde, en plus des monstres tués.")] public float assaultAiIncomePerSecond = 2.5f;
        [Tooltip("Secondes entre deux décisions de l'IA (construire / améliorer).")] public float assaultAiThinkInterval = 5f;

        [Header("Progression")]
        [Tooltip("Multiplicateur de vie des ennemis par vague : vie × (1 + waveIndex × facteur).")]
        public float healthScalingPerWave = 0.12f;
        public float rewardScalingPerWave = 0.05f;
    }
}
