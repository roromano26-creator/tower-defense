using UnityEngine;
using Bastion.Grid;

namespace Bastion.Core
{
    /// <summary>
    /// Liste ordonnée des niveaux et des améliorations permanentes. Vit dans Resources/ pour être
    /// lisible depuis n'importe quelle scène. Ajouter un niveau = l'ajouter ici, rien d'autre.
    /// </summary>
    [CreateAssetMenu(menuName = "Bastion/Catalogue", fileName = "LevelCatalog")]
    public class LevelCatalog : ScriptableObject
    {
        public const string ResourcePath = "LevelCatalog";

        public LevelData[] levels;
        public MetaUpgradeData[] upgrades;

        private static LevelCatalog cached;
        public static LevelCatalog Load()
        {
            if (cached == null) cached = Resources.Load<LevelCatalog>(ResourcePath);
            return cached;
        }

        public int IndexOf(LevelData l) => System.Array.IndexOf(levels, l);
        public LevelData Find(string id) => System.Array.Find(levels, l => l != null && l.levelId == id);
        public LevelData Next(LevelData l) { int i = IndexOf(l); return i >= 0 && i + 1 < levels.Length ? levels[i + 1] : null; }

        /// <summary>
        /// Un niveau est jouable en Normal si le précédent est terminé (en Normal) et si le profil a assez
        /// d'étoiles ; en Difficile / Cauchemar, s'il a été terminé dans la difficulté juste en dessous.
        /// </summary>
        public bool IsUnlocked(int index, Difficulty d = Difficulty.Normal, GameMode mode = GameMode.Defense)
        {
            if (index < 0 || index >= levels.Length) return false;
            if (mode == GameMode.Assault)
            {
                // L'Assaut d'un niveau s'ouvre quand on l'a défendu une fois : on connaît le terrain avant de l'attaquer.
                var defended = SaveSystem.GetResult(levels[index].levelId, Difficulty.Normal, GameMode.Defense);
                if (defended == null || !defended.completed) return false;
            }
            if (d > Difficulty.Normal)
            {
                var below = SaveSystem.GetResult(levels[index].levelId, d - 1, mode);
                return below != null && below.completed;
            }
            if (mode == GameMode.Assault) return true;
            if (index == 0) return true;
            var prev = SaveSystem.GetResult(levels[index - 1].levelId);
            return prev != null && prev.completed && SaveSystem.TotalStars() >= levels[index].starsToUnlock;
        }
    }
}
