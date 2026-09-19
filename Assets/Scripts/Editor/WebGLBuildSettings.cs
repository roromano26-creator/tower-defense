using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Bastion.EditorTools
{
    /// <summary>
    /// Player Settings WebGL : le même jeu, jouable dans un navigateur sans rien installer.
    /// Sert surtout à tester une version sur un téléphone avant de passer par le Play Store.
    /// </summary>
    public static class WebGLBuildSettings
    {
        public static void Apply()
        {
            var target = NamedBuildTarget.WebGL;
            PlayerSettings.companyName = "Veloute";
            PlayerSettings.productName = "Bastion";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.Medium);

            // Pas de gestion d'exceptions : c'est le réglage le plus rapide et le plus léger.
            // Une exception non gérée arrête le jeu au lieu de le ralentir en permanence.
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;

            // Brotli divise le téléchargement par trois, mais le navigateur ne sait le lire que
            // si le serveur envoie Content-Encoding: br. decompressionFallback embarque un
            // décompresseur JavaScript : le build fonctionne alors sur n'importe quel
            // hébergement statique, sans configuration d'en-têtes côté serveur.
            PlayerSettings.WebGL.dataCompressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;

            // Les threads WebGL exigent des en-têtes d'isolation (COOP/COEP) que tous les
            // hébergements ne posent pas ; sans eux la page refuse de se charger.
            PlayerSettings.WebGL.threadsSupport = false;

            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.gpuSkinning = true;
            PlayerSettings.graphicsJobs = false;

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Point d'entrée de l'intégration continue : génère le projet puis construit le WebGL.
        /// Appelé par GitHub Actions via -executeMethod ; sans interaction possible, toute
        /// erreur doit sortir avec un code non nul, sinon le workflow passerait au vert en
        /// publiant un dossier vide.
        /// </summary>
        public static void CIBuildWebGL()
        {
            try
            {
                Debug.Log("[CI] Génération du projet…");
                ProjectBootstrap.GenerateAll();

                Debug.Log("[CI] Player Settings WebGL…");
                Apply();

                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { MenuFactory.MenuScene, BastionPaths.MainScene },
                    locationPathName = AndroidBuildSettings.CheminDeSortie("WebGL"),
                    target = BuildTarget.WebGL,
                    options = BuildOptions.None,
                });

                if (report.summary.result != BuildResult.Succeeded)
                {
                    Debug.LogError($"[CI] Échec de la construction WebGL : {report.summary.result}");
                    EditorApplication.Exit(1);
                    return;
                }
                Debug.Log($"[CI] WebGL construit — {report.summary.totalSize / (1024 * 1024)} Mo");
                EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CI] Échec de la construction : {ex}");
                EditorApplication.Exit(1);
            }
        }
    }
}
