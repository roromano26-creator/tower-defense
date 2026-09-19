using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bastion.Core
{
    /// <summary>
    /// Sauvegarde du profil joueur (progression, réglages) en JSON dans PlayerPrefs.
    /// Suffisant pour un profil de quelques Ko ; passer à un fichier dans persistentDataPath
    /// si la sauvegarde grossit (JsonUtility ne gère pas les dictionnaires, d'où les listes).
    /// </summary>
    public static class SaveSystem
    {
        private const string Key = "bastion.profile.v1";

        [Serializable] public class LevelResult { public string levelId; public int stars; public bool completed; public int bestWave; }
        [Serializable] public class UpgradeRank { public string upgradeId; public int rank; }
        public const int GemsPerStar = 10;

        [Serializable]
        public class Profile
        {
            public List<LevelResult> levels = new();
            public List<UpgradeRank> upgrades = new();
            public int gems;
            public float musicVolume = 0.8f;
            public float sfxVolume = 1f;
            public bool vibrations = true;
        }

        private static Profile cached;

        public static Profile Load()
        {
            if (cached != null) return cached;
            var json = PlayerPrefs.GetString(Key, "");
            cached = string.IsNullOrEmpty(json) ? new Profile() : JsonUtility.FromJson<Profile>(json) ?? new Profile();
            return cached;
        }

        public static void Save()
        {
            if (cached == null) return;
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(cached));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 3 étoiles si toutes les vies sont intactes, 2 au-dessus de la moitié, 1 sinon. Chaque étoile
        /// gagnée pour la première fois rapporte des gemmes ; rejouer un niveau déjà à 3 étoiles n'en donne plus.
        /// Retourne les gemmes gagnées.
        /// </summary>
        public static int StarsForLives(int livesLeft, int maxLives) => livesLeft >= maxLives ? 3 : livesLeft * 2 >= maxLives ? 2 : 1;

        public static int RecordVictory(string levelId, Difficulty d, GameMode mode, int stars, int wavesCleared)
        {
            var p = Load();
            var r = GetOrAdd(levelId, d, mode);
            r.completed = true;
            r.bestWave = Mathf.Max(r.bestWave, wavesCleared);
            int newStars = Mathf.Max(0, stars - r.stars);
            r.stars = Mathf.Max(r.stars, stars);
            int gems = Mathf.RoundToInt(newStars * GemsPerStar * DifficultySettings.GemsMultiplier(d));
            p.gems += gems;
            Save();
            return gems;
        }

        /// <summary>Record de vague (mode Sans fin inclus). Retourne vrai si c'est un nouveau record.</summary>
        public static bool RecordWave(string levelId, Difficulty d, GameMode mode, int wavesCleared)
        {
            var r = GetOrAdd(levelId, d, mode);
            bool record = wavesCleared > r.bestWave;
            r.bestWave = Mathf.Max(r.bestWave, wavesCleared);
            Save();
            return record;
        }

        public static void AddGems(int n) { if (n <= 0) return; Load().gems += n; Save(); }

        private static LevelResult GetOrAdd(string levelId, Difficulty d, GameMode mode)
        {
            var p = Load(); string key = ResultKey(levelId, d, mode);
            var r = p.levels.Find(l => l.levelId == key);
            if (r == null) { r = new LevelResult { levelId = key }; p.levels.Add(r); }
            return r;
        }

        /// <summary>Clé de sauvegarde : « level_01 » en Normal (compatible avec les anciens profils), « level_01@1 » au-delà.</summary>
        public static string ResultKey(string levelId, Difficulty d, GameMode mode = GameMode.Defense)
            => (d == Difficulty.Normal ? levelId : $"{levelId}@{(int)d}") + (mode == GameMode.Assault ? "#a" : "");

        public static int TotalStars() { int n = 0; foreach (var l in Load().levels) n += l.stars; return n; }
        public static int Gems => Load().gems;
        public static int GetRank(string upgradeId) => Load().upgrades.Find(u => u.upgradeId == upgradeId)?.rank ?? 0;

        /// <summary>Achète le rang suivant si les gemmes suffisent. Retourne faux sinon.</summary>
        public static bool TryBuyUpgrade(MetaUpgradeData u)
        {
            var p = Load();
            int rank = GetRank(u.upgradeId);
            if (rank >= u.maxRank) return false;
            int cost = u.CostForRank(rank + 1);
            if (p.gems < cost) return false;
            p.gems -= cost;
            var entry = p.upgrades.Find(x => x.upgradeId == u.upgradeId);
            if (entry == null) { entry = new UpgradeRank { upgradeId = u.upgradeId }; p.upgrades.Add(entry); }
            entry.rank = rank + 1;
            Save();
            return true;
        }

        public static void ResetProfile() { cached = new Profile(); Save(); }

        public static LevelResult GetResult(string levelId, Difficulty d = Difficulty.Normal, GameMode mode = GameMode.Defense) => Load().levels.Find(l => l.levelId == ResultKey(levelId, d, mode));
    }
}
