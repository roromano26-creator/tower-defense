using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bastion.EditorTools
{
    /// <summary>
    /// Palette cartoon-PBR : couleurs saturées, smoothness moyenne, pas de métal, émission pour tout ce
    /// qui doit « bloomer ». Tous les matériaux sont en GPU instancing et compatibles SRP Batcher.
    /// </summary>
    public static class MaterialLibrary
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int Smoothness = Shader.PropertyToID("_Smoothness");
        private static readonly int Metallic = Shader.PropertyToID("_Metallic");
        private static readonly int Emission = Shader.PropertyToID("_EmissionColor");

        public static Material Lit(string name, string hex, float smooth = 0.35f, string emissiveHex = null, float emissive = 0f)
        {
            return BastionPaths.GetOrCreate($"{BastionPaths.Materials}/M_{name}.mat", () =>
            {
                var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = $"M_{name}" };
                m.SetColor(BaseColor, Hex(hex));
                m.SetFloat(Smoothness, smooth);
                m.SetFloat(Metallic, 0f);
                if (emissiveHex != null)
                {
                    m.EnableKeyword("_EMISSION");
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    m.SetColor(Emission, Hex(emissiveHex) * emissive);
                }
                m.enableInstancing = true;
                return m;
            });
        }

        /// <summary>Transparent non éclairé (fantôme de placement, disque de portée).</summary>
        public static Material Transparent(string name, string hex, float alpha)
        {
            return BastionPaths.GetOrCreate($"{BastionPaths.Materials}/M_{name}.mat", () =>
            {
                var m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = $"M_{name}" };
                var c = Hex(hex); c.a = alpha;
                m.SetColor(BaseColor, c);
                SetupTransparent(m, additive: false);
                return m;
            });
        }

        /// <summary>Additif non éclairé pour particules, anneaux d'aura et traînées : le bloom fait le reste.</summary>
        public static Material Additive(string name, string hex, Texture2D tex = null)
        {
            return BastionPaths.GetOrCreate($"{BastionPaths.Materials}/M_{name}.mat", () =>
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                var m = new Material(shader) { name = $"M_{name}" };
                m.SetColor(BaseColor, Hex(hex));
                if (tex != null) m.SetTexture("_BaseMap", tex);
                SetupTransparent(m, additive: true);
                return m;
            });
        }

        private static void SetupTransparent(Material m, bool additive)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", additive ? 1f : 0f);
            m.SetFloat("_SrcBlend", (float)(additive ? BlendMode.One : BlendMode.SrcAlpha));
            m.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Cull", (float)CullMode.Off);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (additive) m.EnableKeyword("_ALPHAPREMULTIPLY_ON"); else m.EnableKeyword("_ALPHABLEND_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetOverrideTag("RenderType", "Transparent");
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out var c);
            return c;
        }

        /// <summary>Texture disque doux (particules) générée procéduralement, sauvée en PNG.</summary>
        public static Texture2D SoftCircle()
        {
            string path = $"{BastionPaths.Textures}/T_SoftCircle.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            const int n = 128;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                    float a = Mathf.Clamp01(1f - d); a = a * a * (3f - 2f * a);
                    t.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            t.Apply();
            System.IO.File.WriteAllBytes(path, t.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.alphaIsTransparency = true; imp.mipmapEnabled = false; imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ---- Palette nommée (inspirée des packs Piloto : bois chaud, pierre froide, accents émissifs) ----
        public static Material Wood => Lit("Wood", "#8A5330", 0.3f);
        public static Material WoodDark => Lit("WoodDark", "#4E2E1B", 0.3f);
        public static Material Stone => Lit("Stone", "#A7AEBB", 0.4f);
        public static Material StoneDark => Lit("StoneDark", "#5A6071", 0.4f);
        public static Material Metal => Lit("Metal", "#2F3038", 0.6f);
        public static Material Gold => Lit("Gold", "#F2A83B", 0.55f);
        public static Material Orange => Lit("Orange", "#F07A2A", 0.45f);
        public static Material Snow => Lit("Snow", "#F4F6FA", 0.25f);
        public static Material Ice => Lit("Ice", "#8FD3FF", 0.7f, "#4FB6FF", 2.2f);
        public static Material Lava => Lit("Lava", "#FF5A1F", 0.5f, "#FF7A1F", 3.5f);
        public static Material RedDark => Lit("RedDark", "#7E2828", 0.35f);
        public static Material Purple => Lit("Purple", "#6E43D6", 0.45f);
        public static Material ArcaneCrystal => Lit("ArcaneCrystal", "#F0A5FF", 0.8f, "#D46CFF", 3f);
        public static Material Toxic => Lit("Toxic", "#8CFF3A", 0.6f, "#7CFF1A", 2.5f);
        public static Material Green => Lit("Green", "#6CC24A", 0.35f);
        public static Material Brown => Lit("Brown", "#7A5A3A", 0.3f);
        public static Material BatPurple => Lit("BatPurple", "#3B2A5A", 0.35f, "#A34CFF", 1.2f);
        public static Material Skull => Lit("Skull", "#EDE6D6", 0.3f);
        public static Material EyeRed => Lit("EyeRed", "#FF3030", 0.6f, "#FF2020", 4f);
        public static Material GroundBuildable => Lit("GroundBuildable", "#34495E", 0.2f);
        public static Material GroundPath => Lit("GroundPath", "#C7A26A", 0.25f);
        public static Material GroundBlocked => Lit("GroundBlocked", "#3B3B47", 0.3f);
        public static Material GroundBase => Lit("GroundBase", "#1C1C26", 0.1f);
        public static Material Ghost => Transparent("Ghost", "#5CFF9A", 0.45f);
        public static Material RangeDisc => Transparent("RangeDisc", "#9FD8FF", 0.22f);
        public static Material CellHighlight => Transparent("CellHighlight", "#FFFFFF", 0.35f);
        public static Material HealthBack => Transparent("HealthBack", "#000000", 0.6f);
        public static Material HealthFill => Transparent("HealthFill", "#3DDC84", 1f);
        public static Material ParticleAdd => Additive("ParticleAdd", "#FFFFFF", SoftCircle());
        public static Material AuraAdd => Additive("AuraAdd", "#FFFFFF");
    }
}
