using UnityEngine;
using Bastion.Grid;

namespace Bastion.Core
{
    /// <summary>Niveau choisi dans le menu, transmis à la scène de jeu. Persisté dans PlayerPrefs pour survivre à un rechargement.</summary>
    public static class LevelSession
    {
        private const string Key = "bastion.selectedLevel";
        private const string DiffKey = "bastion.selectedDifficulty";
        private const string ModeKey = "bastion.selectedMode";
        public static GameMode Mode
        {
            get => (GameMode)PlayerPrefs.GetInt(ModeKey, 0);
            set => PlayerPrefs.SetInt(ModeKey, (int)value);
        }
        public static LevelData Selected { get; private set; }
        public static Difficulty Difficulty
        {
            get => (Difficulty)PlayerPrefs.GetInt(DiffKey, 0);
            set => PlayerPrefs.SetInt(DiffKey, (int)value);
        }

        public static void Select(LevelData l)
        {
            Selected = l;
            PlayerPrefs.SetString(Key, l != null ? l.levelId : "");
        }

        /// <summary>Niveau à jouer : la sélection, sinon celui sauvegardé, sinon `fallback` (le champ de la scène).</summary>
        public static LevelData Resolve(LevelData fallback)
        {
            if (Selected != null) return Selected;
            var catalog = LevelCatalog.Load();
            var id = PlayerPrefs.GetString(Key, "");
            var found = catalog != null && !string.IsNullOrEmpty(id) ? catalog.Find(id) : null;
            return found != null ? found : fallback;
        }
    }
}
