using System;
using UnityEngine;

namespace Bastion.Core
{
    /// <summary>
    /// Bus d'événements statique. L'UI et le gameplay ne se référencent jamais directement :
    /// tout passe par ici, ce qui permet de tester un système isolément dans une scène vide.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<GameState> OnStateChanged;
        public static event Action<int> OnGoldChanged;
        public static event Action<int, int> OnLivesChanged;          // (actuelles, max)
        public static event Action<int, int> OnWaveStarted;           // (index 1-based, total)
        public static event Action<int> OnWaveCompleted;
        public static event Action<float> OnWaveCountdown;            // secondes restantes avant auto-lancement
        public static event Action<Enemies.Enemy> OnEnemySpawned;
        public static event Action<Enemies.Enemy> OnEnemyKilled;
        public static event Action<Enemies.Enemy> OnEnemyReachedEnd;
        public static event Action<Towers.Tower> OnTowerPlaced;
        public static event Action<Towers.Tower> OnTowerUpgraded;
        public static event Action<Towers.Tower> OnTowerSold;
        public static event Action<Towers.Tower> OnTowerSelected;     // null = désélection
        public static event Action<Vector3, float> OnCameraShakeRequested;
        public static event Action<float> OnGameSpeedChanged;
        public static event Action<int> OnGemsEarned;
        public static event Action<float, float> OnManaChanged;        // (mana, cap) — mode Assaut
        public static event Action<float> OnAssaultTimer;              // secondes restantes

        /// <summary>
        /// Purge tous les abonnés. Appelé par GameManager.Awake : les événements sont statiques et
        /// survivent au rechargement de scène, sinon l'UI détruite de la partie précédente y reste abonnée.
        /// </summary>
        public static void ClearAll()
        {
            OnStateChanged = null; OnGoldChanged = null; OnLivesChanged = null; OnWaveStarted = null;
            OnWaveCompleted = null; OnWaveCountdown = null; OnEnemySpawned = null; OnEnemyKilled = null;
            OnEnemyReachedEnd = null; OnTowerPlaced = null; OnTowerUpgraded = null; OnTowerSold = null;
            OnTowerSelected = null; OnCameraShakeRequested = null; OnGameSpeedChanged = null; OnGemsEarned = null; OnManaChanged = null; OnAssaultTimer = null;
        }

        public static void StateChanged(GameState s) => OnStateChanged?.Invoke(s);
        public static void GoldChanged(int g) => OnGoldChanged?.Invoke(g);
        public static void LivesChanged(int l, int max) => OnLivesChanged?.Invoke(l, max);
        public static void WaveStarted(int i, int total) => OnWaveStarted?.Invoke(i, total);
        public static void WaveCompleted(int i) => OnWaveCompleted?.Invoke(i);
        public static void WaveCountdown(float s) => OnWaveCountdown?.Invoke(s);
        public static void EnemySpawned(Enemies.Enemy e) => OnEnemySpawned?.Invoke(e);
        public static void EnemyKilled(Enemies.Enemy e) => OnEnemyKilled?.Invoke(e);
        public static void EnemyReachedEnd(Enemies.Enemy e) => OnEnemyReachedEnd?.Invoke(e);
        public static void TowerPlaced(Towers.Tower t) => OnTowerPlaced?.Invoke(t);
        public static void TowerUpgraded(Towers.Tower t) => OnTowerUpgraded?.Invoke(t);
        public static void TowerSold(Towers.Tower t) => OnTowerSold?.Invoke(t);
        public static void TowerSelected(Towers.Tower t) => OnTowerSelected?.Invoke(t);
        public static void CameraShake(Vector3 at, float strength) => OnCameraShakeRequested?.Invoke(at, strength);
        public static void GameSpeedChanged(float s) => OnGameSpeedChanged?.Invoke(s);
        public static void GemsEarned(int g) => OnGemsEarned?.Invoke(g);
        public static void ManaChanged(float m, float cap) => OnManaChanged?.Invoke(m, cap);
        public static void AssaultTimer(float s) => OnAssaultTimer?.Invoke(s);
    }
}
