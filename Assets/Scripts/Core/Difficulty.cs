namespace Bastion.Core
{
    public enum Difficulty { Normal = 0, Difficile = 1, Cauchemar = 2 }

    /// <summary>Réglages par difficulté. Une difficulté = un jeu d'étoiles séparé par niveau : trois fois plus de contenu pour zéro asset.</summary>
    public static class DifficultySettings
    {
        public static string Label(Difficulty d) => d switch { Difficulty.Difficile => "Difficile", Difficulty.Cauchemar => "Cauchemar", _ => "Normal" };
        public static float HealthMultiplier(Difficulty d) => d switch { Difficulty.Difficile => 1.45f, Difficulty.Cauchemar => 2.1f, _ => 1f };
        public static float SpeedMultiplier(Difficulty d) => d switch { Difficulty.Difficile => 1.08f, Difficulty.Cauchemar => 1.18f, _ => 1f };
        public static float RewardMultiplier(Difficulty d) => d switch { Difficulty.Difficile => 0.9f, Difficulty.Cauchemar => 0.8f, _ => 1f };
        public static float LivesMultiplier(Difficulty d) => d switch { Difficulty.Difficile => 0.6f, Difficulty.Cauchemar => 0.35f, _ => 1f };
        public static float GemsMultiplier(Difficulty d) => d switch { Difficulty.Difficile => 1.5f, Difficulty.Cauchemar => 2.5f, _ => 1f };
        public static int Count => 3;
    }
}
