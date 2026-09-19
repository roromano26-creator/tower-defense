using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bastion.EditorTools
{
    /// <summary>
    /// Player Settings Android : API 26+, IL2CPP ARM64 (+ARMv7), paysage, Vulkan puis GLES3, aucune
    /// permission au-delà du strict minimum, AAB pour le Play Store, icône temporaire générée.
    /// </summary>
    public static class AndroidBuildSettings
    {
        public const string PackageName = "fr.veloute.bastion";

        public static void Apply()
        {
            var target = NamedBuildTarget.Android;
            PlayerSettings.companyName = "Veloute";
            PlayerSettings.productName = "Bastion";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(target, PackageName);
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.Medium);
            PlayerSettings.SetApiCompatibilityLevel(target, ApiCompatibilityLevel.NET_Standard);

            // Orientation : paysage uniquement, les deux sens (le téléphone se retourne, l'UI suit).
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            // Permissions minimales : pas d'internet forcé, pas de stockage externe.
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.Android.forceSDCardPermission = false;

            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.Android.startInFullscreen = true;
            PlayerSettings.Android.renderOutsideSafeArea = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;        // PBR correct ; GLES3 + API 26 le supportent
            PlayerSettings.Android.blitType = AndroidBlitType.Auto;
            PlayerSettings.graphicsJobs = false;
            PlayerSettings.gpuSkinning = true;

            EditorUserBuildSettings.buildAppBundle = true;
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

            ApplyIcon(target);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Icône temporaire : tour verte stylisée sur fond nuit, générée en PNG. À remplacer par votre icône.</summary>
        private static void ApplyIcon(NamedBuildTarget target)
        {
            string path = "Assets/Art/Icon_Temp.png";
            if (!File.Exists(path))
            {
                const int n = 512;
                var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
                var bg = MaterialLibrary.Hex("#0D0D1A"); var brand = MaterialLibrary.Hex("#3DDC84"); var gold = MaterialLibrary.Hex("#F2A83B");
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float u = (x - n / 2f) / n, v = (y - n / 2f) / n;
                        Color c = bg;
                        bool body = Mathf.Abs(u) < 0.16f && v > -0.3f && v < 0.18f;
                        bool crown = Mathf.Abs(u) < 0.24f && v >= 0.18f && v < 0.3f && ((x / 40) % 2 == 0 || v < 0.24f);
                        bool basePlate = Mathf.Abs(u) < 0.3f && v > -0.36f && v <= -0.3f;
                        if (body || crown) c = brand; else if (basePlate) c = gold;
                        if (u * u + v * v > 0.23f) c.a = 0f;
                        t.SetPixel(x, y, c);
                    }
                t.Apply();
                File.WriteAllBytes(path, t.EncodeToPNG());
                AssetDatabase.ImportAsset(path);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.alphaIsTransparency = true; imp.mipmapEnabled = false; imp.SaveAndReimport();
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var sizes = PlayerSettings.GetIconSizes(target, IconKind.Application);
            var icons = new Texture2D[sizes.Length];
            for (int i = 0; i < icons.Length; i++) icons[i] = tex;
            PlayerSettings.SetIcons(target, icons, IconKind.Application);
        }

        /// <summary>
        /// Test direct : APK de développement installé et lancé sur le téléphone branché en USB
        /// (débogage USB activé). Le Profiler peut s'y connecter (Autoconnect Profiler).
        /// </summary>
        public static void BuildAndRunApk()
        {
            Apply();
            EditorUserBuildSettings.buildAppBundle = false;          // un APK s'installe via adb, pas un AAB
            Directory.CreateDirectory("Builds");
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { MenuFactory.MenuScene, BastionPaths.MainScene },
                locationPathName = "Builds/Bastion-dev.apk",
                target = BuildTarget.Android,
                options = BuildOptions.Development | BuildOptions.AutoRunPlayer | BuildOptions.ConnectWithProfiler,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log($"Build & Run : {report.summary.result} → {opts.locationPathName}");
        }

        public static void BuildAab()
        {
            Apply();
            Directory.CreateDirectory("Builds");
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { MenuFactory.MenuScene, BastionPaths.MainScene },
                locationPathName = "Builds/Bastion.aab",
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log($"Build Android : {report.summary.result} — {report.summary.totalSize / (1024 * 1024)} Mo → {opts.locationPathName}");
        }

        /// <summary>
        /// Point d'entrée de l'intégration continue : génère le projet puis construit un APK.
        /// Appelé par GitHub Actions via -executeMethod ; aucune interaction possible, donc
        /// toute erreur doit se terminer par un code de sortie non nul, sinon le workflow
        /// passerait au vert en publiant un artefact vide.
        /// </summary>
        public static void CIBuildApk()
        {
            try
            {
                Debug.Log("[CI] Génération du projet…");
                ProjectBootstrap.GenerateAll();

                Debug.Log("[CI] Player Settings Android…");
                Apply();

                // Un APK s'installe via adb ou depuis le téléphone ; un AAB ne s'installe pas.
                EditorUserBuildSettings.buildAppBundle = false;

                // ARM64 seul : l'APK de test vise un téléphone moderne, et compiler aussi
                // ARMv7 en IL2CPP double le temps de construction sur un runner gratuit.
                // La cible Play Store (BuildAab) garde les deux architectures.
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { MenuFactory.MenuScene, BastionPaths.MainScene },
                    locationPathName = CheminDeSortie("Bastion.apk"),
                    target = BuildTarget.Android,
                    options = BuildOptions.None,
                });

                if (report.summary.result != BuildResult.Succeeded)
                {
                    Debug.LogError($"[CI] Échec de la construction APK : {report.summary.result}");
                    EditorApplication.Exit(1);
                    return;
                }
                Debug.Log($"[CI] APK construit — {report.summary.totalSize / (1024 * 1024)} Mo");
                EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CI] Échec de la construction : {ex}");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Chemin absolu sous &lt;projet&gt;/Builds. Unity lancé en ligne de commande avec
        /// -projectPath n'a pas forcément le projet comme dossier courant : un chemin
        /// relatif déposerait l'APK ailleurs et l'étape d'archivage ne trouverait rien.
        /// </summary>
        internal static string CheminDeSortie(string nomFichier)
        {
            string racine = Path.GetDirectoryName(Application.dataPath);
            string dossier = Path.Combine(racine, "Builds");
            Directory.CreateDirectory(dossier);
            return Path.Combine(dossier, nomFichier);
        }
    }
}
