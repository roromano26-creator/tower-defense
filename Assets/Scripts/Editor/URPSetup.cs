using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Bastion.EditorTools
{
    /// <summary>
    /// URP configuré pour mobile milieu de gamme : SRP Batcher, 1 cascade d'ombres à 30 m, MSAA 2x,
    /// HDR pour le bloom, pas de textures de profondeur/opaque (personne ne les lit), post-process
    /// léger : Bloom, Color Adjustments, Vignette. Tout est dans Assets/Settings/URP.
    /// </summary>
    public static class URPSetup
    {

        /// <summary>
        /// URP 17 a rendu ces quatre réglages en lecture seule sur l'asset ; leurs champs
        /// sérialisés restent la seule voie pour les écrire depuis l'éditeur. Un champ
        /// introuvable est signalé sans être fatal : le nom interne peut changer d'une
        /// version d'URP à l'autre, et perdre un réglage d'éclairage ne justifie pas de
        /// faire échouer toute la génération du projet.
        /// </summary>
        private static void ReglerEclairage(UniversalRenderPipelineAsset asset)
        {
            var so = new SerializedObject(asset);
            Ecrire(so, "m_MainLightRenderingMode", p => p.intValue = (int)LightRenderingMode.PerPixel);
            Ecrire(so, "m_MainLightShadowsSupported", p => p.boolValue = true);
            Ecrire(so, "m_AdditionalLightsRenderingMode", p => p.intValue = (int)LightRenderingMode.Disabled);
            Ecrire(so, "m_AdditionalLightShadowsSupported", p => p.boolValue = false);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Ecrire(SerializedObject so, string champ, System.Action<SerializedProperty> valeur)
        {
            var p = so.FindProperty(champ);
            if (p == null) { Debug.LogWarning($"URP : champ sérialisé '{champ}' introuvable, réglage ignoré."); return; }
            valeur(p);
        }
        public static UniversalRenderPipelineAsset Apply()
        {
            var rendererData = BastionPaths.GetOrCreate($"{BastionPaths.Settings}/BastionRenderer.asset", () =>
            {
                var r = ScriptableObject.CreateInstance<UniversalRendererData>();
                r.name = "BastionRenderer";
                r.renderingMode = RenderingMode.Forward;      // Forward+ coûte trop cher sur GPU mobile pour une scène à une lumière
                r.depthPrimingMode = DepthPrimingMode.Disabled;
                return r;
            });

            var asset = BastionPaths.GetOrCreate($"{BastionPaths.Settings}/BastionURP.asset", () =>
            {
                var a = UniversalRenderPipelineAsset.Create(rendererData);
                a.name = "BastionURP";
                return a;
            });

            asset.supportsHDR = true;                          // nécessaire au bloom propre
            asset.msaaSampleCount = 2;
            asset.renderScale = 1f;
            asset.supportsCameraDepthTexture = false;
            asset.supportsCameraOpaqueTexture = false;
            asset.useSRPBatcher = true;
            asset.supportsDynamicBatching = false;             // le SRP Batcher fait mieux, et le dynamic batching casse l'instancing
            asset.shadowDistance = 30f;
            asset.shadowCascadeCount = 1;
            asset.mainLightShadowmapResolution = 2048;
            ReglerEclairage(asset);   // aucune lumière additionnelle : émission + bloom suffisent
            asset.shadowDepthBias = 1f; asset.shadowNormalBias = 1f;
            asset.colorGradingMode = ColorGradingMode.LowDynamicRange;
            asset.colorGradingLutSize = 32;
            asset.upscalingFilter = UpscalingFilterSelection.Auto;
            EditorUtility.SetDirty(asset); EditorUtility.SetDirty(rendererData);

            GraphicsSettings.defaultRenderPipeline = asset;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = asset;
            }
            QualitySettings.SetQualityLevel(Mathf.Max(0, QualitySettings.names.Length - 1), false);
            return asset;
        }

        public static VolumeProfile PostProcessProfile()
        {
            string path = $"{BastionPaths.Settings}/BastionPostProcess.asset";
            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (existing != null) return existing;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.9f);
            bloom.threshold.Override(1.05f);        // seulement l'émissif bloome, pas la neige blanche
            bloom.scatter.Override(0.65f);
            bloom.highQualityFiltering.Override(false);   // cher sur mobile
            bloom.maxIterations.Override(5);

            var adj = profile.Add<ColorAdjustments>(true);
            adj.postExposure.Override(0.15f);
            adj.contrast.Override(12f);
            adj.saturation.Override(14f);

            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.22f);
            vig.smoothness.Override(0.5f);

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);   // ACES écrase les pastels du style cartoon

            foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }
    }
}
