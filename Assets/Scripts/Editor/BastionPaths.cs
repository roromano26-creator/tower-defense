using System.IO;
using UnityEditor;
using UnityEngine;

namespace Bastion.EditorTools
{
    /// <summary>Chemins des assets générés + utilitaires de sauvegarde idempotents (regénérer n'écrase pas les réglages manuels sauf demande).</summary>
    public static class BastionPaths
    {
        public const string Materials = "Assets/Materials";
        public const string Meshes = "Assets/Art/Placeholders/Meshes";
        public const string Textures = "Assets/Art/Placeholders/Textures";
        public const string PrefTowers = "Assets/Prefabs/Towers";
        public const string PrefTowerVisuals = "Assets/Prefabs/Towers/Visuals";
        public const string PrefEnemies = "Assets/Prefabs/Enemies";
        public const string PrefEnemyVisuals = "Assets/Prefabs/Enemies/Visuals";
        public const string PrefProjectiles = "Assets/Prefabs/Projectiles";
        public const string PrefVFX = "Assets/Prefabs/VFX";
        public const string PrefUI = "Assets/Prefabs/UI";
        public const string DataTowers = "Assets/Data/Towers";
        public const string DataEnemies = "Assets/Data/Enemies";
        public const string DataWaves = "Assets/Data/Waves";
        public const string DataLevels = "Assets/Data/Levels";
        public const string Settings = "Assets/Settings/URP";
        public const string Scenes = "Assets/Scenes";
        public const string MainScene = "Assets/Scenes/Main.unity";

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        public static void EnsureAll()
        {
            foreach (var p in new[] { Materials, Meshes, Textures, PrefTowers, PrefTowerVisuals, PrefEnemies, PrefEnemyVisuals,
                                      PrefProjectiles, PrefVFX, PrefUI, DataTowers, DataEnemies, DataWaves, DataLevels, Settings, Scenes })
                EnsureFolder(p);
        }

        /// <summary>Charge l'asset s'il existe, sinon le crée via `make` et le sauvegarde.</summary>
        public static T GetOrCreate<T>(string path, System.Func<T> make) where T : Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var obj = make();
            AssetDatabase.CreateAsset(obj, path);
            return obj;
        }

        public static GameObject SavePrefab(GameObject go, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }
    }
}
