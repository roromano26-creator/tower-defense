using UnityEditor;
using UnityEngine;

namespace Bastion.EditorTools
{
    /// <summary>
    /// Point d'entrée : menu « Bastion ». Tout le projet est une recette rejouable, pas un dépôt de YAML.
    /// 1. Génère URP, matériaux, meshes, prefabs, données, scène et réglages Android.
    /// 2. Réapplique seulement les Player Settings Android.
    /// 3. Construit l'AAB dans Builds/.
    /// </summary>
    public static class ProjectBootstrap
    {
        [MenuItem("Bastion/1. Générer le projet (URP, placeholders, données, scène)")]
        public static void GenerateAll()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Bastion", "Dossiers…", 0.05f);
                BastionPaths.EnsureAll();

                EditorUtility.DisplayProgressBar("Bastion", "URP + post-process…", 0.15f);
                URPSetup.Apply();
                var post = URPSetup.PostProcessProfile();

                EditorUtility.DisplayProgressBar("Bastion", "Matériaux et meshes…", 0.3f);
                _ = MaterialLibrary.ParticleAdd; _ = MeshLibrary.Cone(); _ = MeshLibrary.Crystal(); _ = MeshLibrary.Ring();
                AssetDatabase.SaveAssets();

                EditorUtility.DisplayProgressBar("Bastion", "Données (tours, ennemis, vagues, niveau)…", 0.5f);
                var towers = DataFactory.Towers();
                var enemies = DataFactory.Enemies();
                var level = DataFactory.Level(DataFactory.Waves(enemies), towers, enemies);
                var level2 = DataFactory.Level2(DataFactory.Waves2(enemies), enemies);
                var level3 = DataFactory.Level3(DataFactory.Waves3(enemies), enemies);
                var upgrades = DataFactory.Upgrades();
                DataFactory.Catalog(new[] { level, level2, level3 }, upgrades);
                AssetDatabase.SaveAssets();

                EditorUtility.DisplayProgressBar("Bastion", "Scènes…", 0.7f);
                SceneFactory.Build(level, towers, post);
                MenuFactory.Build(towers, post);

                EditorUtility.DisplayProgressBar("Bastion", "Réglages Android…", 0.9f);
                AndroidBuildSettings.Apply();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Bastion : projet généré. Ouvrez Assets/Scenes/Menu.unity et appuyez sur Play.");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        [MenuItem("Bastion/2. Appliquer les Player Settings Android")]
        public static void ApplyAndroid() => AndroidBuildSettings.Apply();

        [MenuItem("Bastion/3. Build & Run sur l'appareil USB (APK dev)")]
        public static void BuildAndRun() => AndroidBuildSettings.BuildAndRunApk();

        [MenuItem("Bastion/4. Construire l'AAB Play Store (Builds/)")]
        public static void BuildAndroid() => AndroidBuildSettings.BuildAab();

        [MenuItem("Bastion/Régénérer les placeholders (supprime Prefabs/*/Visuals)")]
        public static void RegeneratePlaceholders()
        {
            if (!EditorUtility.DisplayDialog("Bastion", "Supprimer les visuels placeholders et les recréer ? Les visuels importés référencés dans les ScriptableObjects ne sont pas touchés.", "Régénérer", "Annuler")) return;
            AssetDatabase.DeleteAsset(BastionPaths.PrefTowerVisuals);
            AssetDatabase.DeleteAsset(BastionPaths.PrefEnemyVisuals);
            BastionPaths.EnsureAll();
            DataFactory.Towers(); DataFactory.Enemies();
            AssetDatabase.SaveAssets();
        }
    }
}
