using UnityEngine;

namespace Bastion.Core
{
    /// <summary>Or et vies du joueur. Toute variation passe par ici et publie un événement.</summary>
    [DefaultExecutionOrder(-190)]
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        public int Gold { get; private set; }
        public int Lives { get; private set; }
        public int MaxLives { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Initialize(int gold, int lives)
        {
            Gold = gold; Lives = lives; MaxLives = lives;
            GameEvents.GoldChanged(Gold);
            GameEvents.LivesChanged(Lives, MaxLives);
        }

        public bool CanAfford(int cost) => Gold >= cost;

        public bool TrySpend(int cost)
        {
            if (!CanAfford(cost)) return false;
            Gold -= cost;
            GameEvents.GoldChanged(Gold);
            return true;
        }

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            Gold += amount;
            GameEvents.GoldChanged(Gold);
        }

        public void LoseLives(int amount)
        {
            if (amount <= 0 || Lives <= 0) return;
            Lives = Mathf.Max(0, Lives - amount);
            GameEvents.LivesChanged(Lives, MaxLives);
        }
    }
}
