using UnityEngine;
using Bastion.Grid;

namespace Bastion.Core
{
    /// <summary>
    /// Machine à états de la partie. Singleton de scène (pas de DontDestroyOnLoad : une scène = une partie).
    /// Ne contient aucune logique de vague, de tour ou d'ennemi : il orchestre seulement.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Niveau")]
        [Tooltip("Niveau de secours quand la scène est lancée directement, sans passer par le menu.")]
        [SerializeField] private LevelData level;
        [SerializeField] private string menuSceneName = "Menu";

        [Header("Vitesse")]
        [SerializeField] private float[] speedSteps = { 1f, 2f, 3f };

        public LevelData Level => level;
        public Difficulty Difficulty { get; private set; }
        public GameMode Mode { get; private set; }
        public bool IsAssault => Mode == GameMode.Assault;
        /// <summary>Prime d'or par monstre tué : bonus permanent en Défense, neutre en Assaut (l'or va à l'IA).</summary>
        public float EnemyGoldMultiplier => IsAssault ? 1f : MetaProgression.EnemyGoldMultiplier;
        /// <summary>Vrai après la victoire, quand le joueur a choisi de continuer en vagues générées.</summary>
        public bool Endless { get; private set; }
        public int ScriptedWaveCount => level.waves.Length;
        public GameState State { get; private set; } = GameState.Menu;
        public float GameSpeed => speedSteps[speedIndex];

        private int speedIndex;
        private GameState stateBeforePause;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            GameEvents.ClearAll();
            level = LevelSession.Resolve(level);      // le menu a choisi ; sinon le champ de la scène
            Difficulty = LevelSession.Difficulty;
            Mode = LevelSession.Mode;
            Application.targetFrameRate = 60;      // 60 fps visé, même sur milieu de gamme
            QualitySettings.vSyncCount = 0;
        }

        private void Start()
        {
            if (IsAssault)
            {
                // Le défenseur IA a des vies pleines et un trésor de départ ; la difficulté enrichit l'IA.
                int aiGold = Mathf.RoundToInt(level.assaultAiStartingGold * DifficultySettings.HealthMultiplier(Difficulty));
                ResourceManager.Instance.Initialize(aiGold, level.startingLives);
                SetState(GameState.Building);          // l'IA pré-construit pendant cet état, puis l'assaut démarre
                Assault.AssaultController.Instance.Begin();
                return;
            }
            int lives = Mathf.Max(1, Mathf.RoundToInt(level.startingLives * DifficultySettings.LivesMultiplier(Difficulty))) + MetaProgression.ExtraStartingLives;
            ResourceManager.Instance.Initialize(level.startingGold + MetaProgression.ExtraStartingGold, lives);
            SetState(GameState.Building);
        }

        private void OnEnable()
        {
            GameEvents.OnLivesChanged += HandleLives;
            GameEvents.OnWaveCompleted += HandleWaveCompleted;
        }

        private void OnDisable()
        {
            GameEvents.OnLivesChanged -= HandleLives;
            GameEvents.OnWaveCompleted -= HandleWaveCompleted;
        }

        public void SetState(GameState next)
        {
            if (State == next) return;
            State = next;
            Time.timeScale = next == GameState.Paused || next == GameState.Victory || next == GameState.Defeat ? 0f : GameSpeed;
            GameEvents.StateChanged(next);
        }

        /// <summary>Appelé par l'UI (bouton « Lancer la vague ») ou par le décompte automatique.</summary>
        public void StartNextWave()
        {
            if (State != GameState.Building || IsAssault) return;
            SetState(GameState.WaveRunning);
            Waves.WaveManager.Instance.StartNextWave();
        }

        public void TogglePause()
        {
            if (State == GameState.Paused) { SetState(stateBeforePause); return; }
            if (State == GameState.Victory || State == GameState.Defeat) return;
            stateBeforePause = State;
            SetState(GameState.Paused);
        }

        public void CycleGameSpeed()
        {
            speedIndex = (speedIndex + 1) % speedSteps.Length;
            if (State == GameState.Building || State == GameState.WaveRunning) Time.timeScale = GameSpeed;
            GameEvents.GameSpeedChanged(GameSpeed);
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.buildIndex);
        }

        public void GoToMenu()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(menuSceneName);
        }

        /// <summary>Enchaîne sur le niveau suivant du catalogue s'il existe et est déverrouillé.</summary>
        public bool TryLoadNextLevel()
        {
            var catalog = LevelCatalog.Load();
            var next = catalog != null ? catalog.Next(level) : null;
            if (next == null || !catalog.IsUnlocked(catalog.IndexOf(next), Difficulty, Mode)) return false;
            LevelSession.Select(next);
            RestartLevel();
            return true;
        }

        public int GemsEarned { get; private set; }

        private void HandleLives(int lives, int _)
        {
            if (lives > 0 || State == GameState.Defeat || State == GameState.Victory) return;
            if (IsAssault)
            {
                // Le bastion est tombé : victoire de l'assaillant, étoiles selon le temps restant.
                float left = Assault.AssaultController.Instance.TimeLeft / Mathf.Max(1f, level.assaultDuration);
                int stars = left >= 0.5f ? 3 : left >= 0.2f ? 2 : 1;
                GemsEarned = SaveSystem.RecordVictory(level.levelId, Difficulty, Mode, stars, 0);
                SetState(GameState.Victory);
                return;
            }
            WavesCleared = Waves.WaveManager.Instance.CurrentWave - 1;
            NewRecord = SaveSystem.RecordWave(level.levelId, Difficulty, Mode, WavesCleared);
            SetState(GameState.Defeat);
        }

        public int WavesCleared { get; private set; }
        public bool NewRecord { get; private set; }

        private void HandleWaveCompleted(int waveIndex)
        {
            if (State == GameState.Defeat) return;
            WavesCleared = waveIndex;
            if (Endless)
            {
                // Paliers du mode Sans fin : des gemmes toutes les 5 vagues au-delà du scénario, multipliées par la difficulté.
                int beyond = waveIndex - ScriptedWaveCount;
                if (beyond > 0 && beyond % 5 == 0)
                {
                    int gems = Mathf.RoundToInt(level.endlessGemsPerMilestone * DifficultySettings.GemsMultiplier(Difficulty));
                    SaveSystem.AddGems(gems);
                    GemsEarned += gems;
                    GameEvents.GemsEarned(gems);
                }
                SaveSystem.RecordWave(level.levelId, Difficulty, Mode, waveIndex);
                SetState(GameState.Building);
                return;
            }
            if (waveIndex >= ScriptedWaveCount)
            {
                GemsEarned = SaveSystem.RecordVictory(level.levelId, Difficulty, Mode, SaveSystem.StarsForLives(ResourceManager.Instance.Lives, ResourceManager.Instance.MaxLives), waveIndex);
                SetState(GameState.Victory);
            }
            else SetState(GameState.Building);
        }

        /// <summary>Mode Assaut : le temps est écoulé, le défenseur a tenu.</summary>
        public void AssaultTimeOut()
        {
            if (State != GameState.WaveRunning) return;
            SetState(GameState.Defeat);
        }

        /// <summary>Depuis l'écran de victoire : on garde ses tours et son or, les vagues deviennent générées et sans fin.</summary>
        public void ContinueEndless()
        {
            if (State != GameState.Victory || !level.endlessEnabled) return;
            Endless = true;
            SetState(GameState.Building);
        }
    }
}
