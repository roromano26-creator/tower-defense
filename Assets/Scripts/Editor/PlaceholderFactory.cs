using UnityEngine;
using UnityEngine.Rendering;
using Bastion.Towers;
using Bastion.Enemies;
using Bastion.Grid;
using Bastion.VFX;

namespace Bastion.EditorTools
{
    /// <summary>
    /// Fabrique des visuels placeholders : primitives assemblées et matérialisées, inspirées des
    /// silhouettes du pack Piloto (tourelles qui grandissent niveau par niveau, accents émissifs).
    /// Chaque visuel est un prefab autonome sous Prefabs/*/Visuals — c'est lui que vos vrais modèles remplacent.
    /// </summary>
    public static class PlaceholderFactory
    {
        // ---------- primitives utilitaires ----------
        public static GameObject Part(Transform parent, string name, PrimitiveType type, Material mat, Vector3 pos, Vector3 scale, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos; go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        public static GameObject MeshPart(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, Vector3 scale, Vector3? euler = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos; go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        private static Transform Node(Transform parent, string name, Vector3 pos)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = pos; return go.transform;
        }

        private static void NoShadows(GameObject go) { foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off; }

        // ---------- tours (3 niveaux chacune) ----------
        public static GameObject TowerVisual(string id, int level)
        {
            string path = $"{BastionPaths.PrefTowerVisuals}/V_{id}_L{level + 1}.prefab";
            var existing = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var root = new GameObject($"V_{id}_L{level + 1}");
            var t = root.transform;
            float h;                                   // hauteur de la plateforme de tir
            switch (id)
            {
                case "crossbow": h = Crossbow(t, level); break;
                case "cannon": h = Cannon(t, level); break;
                case "frost": h = Frost(t, level); break;
                case "furnace": h = Furnace(t, level); break;
                default: h = Arcane(t, level); break;
            }
            Node(t, "FirePoint", new Vector3(0, h, 0));
            return BastionPaths.SavePrefab(root, path);
        }

        private static float Crossbow(Transform t, int L)
        {
            Part(t, "Base", PrimitiveType.Cylinder, MaterialLibrary.Gold, new Vector3(0, 0.08f, 0), new Vector3(1.6f, 0.08f, 1.6f));
            float top = 0.16f;
            for (int i = 0; i <= L; i++)
            {   // le fût de pierre grandit d'un étage par niveau
                Part(t, $"Stone{i}", PrimitiveType.Cylinder, i % 2 == 0 ? MaterialLibrary.Stone : MaterialLibrary.WoodDark, new Vector3(0, top + 0.3f, 0), new Vector3(1.1f - i * 0.1f, 0.3f, 1.1f - i * 0.1f));
                top += 0.6f;
            }
            for (int i = 0; i < 4; i++)
            {   // quatre poteaux de bois
                float a = i * 90f * Mathf.Deg2Rad;
                Part(t, $"Post{i}", PrimitiveType.Cube, MaterialLibrary.Wood, new Vector3(Mathf.Cos(a) * 0.65f, top / 2f, Mathf.Sin(a) * 0.65f), new Vector3(0.18f, top, 0.18f));
            }
            var turret = Node(t, "Turret", new Vector3(0, top + 0.15f, 0));
            Part(turret, "Deck", PrimitiveType.Cylinder, MaterialLibrary.Orange, Vector3.zero, new Vector3(1.3f, 0.06f, 1.3f));
            int bows = 1 + L;                          // une arbalète de plus par niveau, comme sur la planche de référence
            for (int i = 0; i < bows; i++)
            {
                float a = i * 360f / bows;
                var bow = Node(turret, $"Bow{i}", Vector3.zero);
                bow.localEulerAngles = new Vector3(0, a, 0);
                Part(bow, "Stock", PrimitiveType.Cube, MaterialLibrary.WoodDark, new Vector3(0, 0.25f, 0.3f), new Vector3(0.12f, 0.12f, 0.9f));
                Part(bow, "Limb", PrimitiveType.Cube, MaterialLibrary.Metal, new Vector3(0, 0.25f, 0.55f), new Vector3(1.0f, 0.06f, 0.08f));
                MeshPart(bow, "Tip", MeshLibrary.Cone(), MaterialLibrary.Metal, new Vector3(0, 0.25f, 0.8f), new Vector3(0.12f, 0.15f, 0.12f), new Vector3(90, 0, 0));
            }
            if (L == 2) for (int i = 0; i < 6; i++) { float a = i * 60f * Mathf.Deg2Rad; MeshPart(t, $"Spike{i}", MeshLibrary.Cone(), MaterialLibrary.Metal, new Vector3(Mathf.Cos(a) * 0.9f, 0.25f, Mathf.Sin(a) * 0.9f), new Vector3(0.2f, 0.35f, 0.2f)); }
            return top + 0.4f;
        }

        private static float Cannon(Transform t, int L)
        {
            Part(t, "Base", PrimitiveType.Cylinder, MaterialLibrary.Purple, new Vector3(0, 0.08f, 0), new Vector3(1.7f, 0.08f, 1.7f));
            float top = 0.16f;
            for (int i = 0; i <= L; i++)
            {
                Part(t, $"Stone{i}", PrimitiveType.Cylinder, MaterialLibrary.StoneDark, new Vector3(0, top + 0.28f, 0), new Vector3(1.3f - i * 0.12f, 0.28f, 1.3f - i * 0.12f));
                Part(t, $"Band{i}", PrimitiveType.Cylinder, MaterialLibrary.Gold, new Vector3(0, top + 0.56f, 0), new Vector3(1.36f - i * 0.12f, 0.03f, 1.36f - i * 0.12f));
                top += 0.58f;
            }
            for (int i = 0; i < 2 + L; i++)
            {   // crânes et cornes, plus nombreux à chaque niveau
                float a = (i * 360f / (2 + L) + 30f) * Mathf.Deg2Rad;
                Part(t, $"Skull{i}", PrimitiveType.Sphere, MaterialLibrary.Skull, new Vector3(Mathf.Cos(a) * 0.62f, top * 0.55f, Mathf.Sin(a) * 0.62f), Vector3.one * 0.22f);
                MeshPart(t, $"Horn{i}", MeshLibrary.Cone(), MaterialLibrary.Skull, new Vector3(Mathf.Cos(a) * 0.7f, top - 0.1f, Mathf.Sin(a) * 0.7f), new Vector3(0.14f, 0.35f, 0.14f), new Vector3(-30 + Mathf.Cos(a) * 20, 0, Mathf.Sin(a) * 30));
            }
            var turret = Node(t, "Turret", new Vector3(0, top + 0.1f, 0));
            Part(turret, "Mount", PrimitiveType.Cube, MaterialLibrary.Wood, new Vector3(0, 0.1f, 0), new Vector3(0.5f, 0.25f, 0.6f));
            float len = 0.9f + L * 0.2f;
            Part(turret, "Barrel", PrimitiveType.Cylinder, MaterialLibrary.Metal, new Vector3(0, 0.35f, len * 0.25f), new Vector3(0.32f + L * 0.04f, len / 2f, 0.32f + L * 0.04f), new Vector3(90, 0, 0));
            Part(turret, "Muzzle", PrimitiveType.Cylinder, MaterialLibrary.RedDark, new Vector3(0, 0.35f, len * 0.7f), new Vector3(0.38f + L * 0.04f, 0.08f, 0.38f + L * 0.04f), new Vector3(90, 0, 0));
            if (L >= 1) Part(turret, "Barrel2", PrimitiveType.Cylinder, MaterialLibrary.Metal, new Vector3(0, 0.6f, len * 0.2f), new Vector3(0.22f, len / 2.4f, 0.22f), new Vector3(90, 0, 0));
            return top + 0.45f;
        }

        private static float Frost(Transform t, int L)
        {
            Part(t, "Base", PrimitiveType.Cylinder, MaterialLibrary.StoneDark, new Vector3(0, 0.06f, 0), new Vector3(1.7f, 0.06f, 1.7f));
            Part(t, "SnowMound", PrimitiveType.Sphere, MaterialLibrary.Snow, new Vector3(0, 0.1f, 0), new Vector3(1.5f, 0.55f, 1.5f));
            float top = 0.45f;
            for (int i = 0; i < L; i++)
            {   // le niveau 2 et 3 empilent des blocs d'igloo
                Part(t, $"Igloo{i}", PrimitiveType.Cylinder, MaterialLibrary.Snow, new Vector3(0, top + 0.25f, 0), new Vector3(1.05f - i * 0.1f, 0.25f, 1.05f - i * 0.1f));
                top += 0.5f;
            }
            Part(t, "Dome", PrimitiveType.Sphere, MaterialLibrary.Snow, new Vector3(0, top + 0.1f, 0), new Vector3(1.0f, 0.75f, 1.0f));
            for (int i = 0; i < 2 + L; i++)
            {   // bonshommes de neige autour, canons courts
                float a = i * 360f / (2 + L) * Mathf.Deg2Rad; var p = new Vector3(Mathf.Cos(a) * 0.72f, 0, Mathf.Sin(a) * 0.72f);
                Part(t, $"SnowBody{i}", PrimitiveType.Sphere, MaterialLibrary.Snow, p + new Vector3(0, 0.45f, 0), Vector3.one * 0.4f);
                Part(t, $"SnowHead{i}", PrimitiveType.Sphere, MaterialLibrary.Snow, p + new Vector3(0, 0.75f, 0), Vector3.one * 0.26f);
                MeshPart(t, $"Nose{i}", MeshLibrary.Cone(), MaterialLibrary.Orange, p + new Vector3(Mathf.Cos(a) * 0.13f, 0.75f, Mathf.Sin(a) * 0.13f), new Vector3(0.06f, 0.15f, 0.06f), new Vector3(90, -a * Mathf.Rad2Deg + 90, 0));
                Part(t, $"Pipe{i}", PrimitiveType.Cylinder, MaterialLibrary.Metal, p + new Vector3(0, 0.3f, 0), new Vector3(0.14f, 0.18f, 0.14f), new Vector3(90, -a * Mathf.Rad2Deg + 90, 0));
            }
            if (L == 2) for (int i = 0; i < 4; i++) { float a = (i * 90f + 45f) * Mathf.Deg2Rad; Part(t, $"Frame{i}", PrimitiveType.Cube, MaterialLibrary.WoodDark, new Vector3(Mathf.Cos(a) * 0.8f, top / 2f + 0.2f, Mathf.Sin(a) * 0.8f), new Vector3(0.1f, top + 0.4f, 0.1f)); }
            var turret = Node(t, "Turret", new Vector3(0, top + 0.5f, 0));
            MeshPart(turret, "Crystal", MeshLibrary.Crystal(), MaterialLibrary.Ice, new Vector3(0, 0.3f, 0), new Vector3(0.6f + L * 0.1f, 0.7f + L * 0.1f, 0.6f + L * 0.1f));
            return top + 0.9f;
        }

        private static float Furnace(Transform t, int L)
        {
            Part(t, "LavaRing", PrimitiveType.Cylinder, MaterialLibrary.Lava, new Vector3(0, 0.05f, 0), new Vector3(1.85f, 0.05f, 1.85f));
            Part(t, "Base", PrimitiveType.Cube, MaterialLibrary.RedDark, new Vector3(0, 0.25f, 0), new Vector3(1.4f, 0.3f, 1.4f));
            float top = 0.4f;
            for (int i = 0; i <= L; i++)
            {
                Part(t, $"Block{i}", PrimitiveType.Cube, i % 2 == 0 ? MaterialLibrary.RedDark : MaterialLibrary.StoneDark, new Vector3(0, top + 0.2f, 0), new Vector3(1.1f - i * 0.15f, 0.4f, 1.1f - i * 0.15f));
                Part(t, $"Glow{i}", PrimitiveType.Cube, MaterialLibrary.Lava, new Vector3(0, top + 0.2f, 0), new Vector3(1.12f - i * 0.15f, 0.06f, 1.12f - i * 0.15f));
                top += 0.4f;
            }
            Part(t, "Chimney", PrimitiveType.Cylinder, MaterialLibrary.Metal, new Vector3(0, top + 0.35f, 0), new Vector3(0.35f, 0.35f, 0.35f));
            for (int i = 0; i < 2 + L; i++)
            {   // tuyaux/serpents de métal autour (clin d'œil aux têtes de dragon de la référence)
                float a = i * 360f / (2 + L) * Mathf.Deg2Rad;
                Part(t, $"Pipe{i}", PrimitiveType.Cylinder, MaterialLibrary.Metal, new Vector3(Mathf.Cos(a) * 0.75f, top * 0.5f, Mathf.Sin(a) * 0.75f), new Vector3(0.16f, top * 0.5f, 0.16f));
                Part(t, $"PipeHead{i}", PrimitiveType.Sphere, MaterialLibrary.Metal, new Vector3(Mathf.Cos(a) * 0.75f, top, Mathf.Sin(a) * 0.75f), Vector3.one * 0.26f);
            }
            var turret = Node(t, "Turret", new Vector3(0, top + 0.75f, 0));
            Part(turret, "Ember", PrimitiveType.Sphere, MaterialLibrary.Lava, Vector3.zero, Vector3.one * (0.45f + L * 0.1f));
            return top + 0.75f;
        }

        private static float Arcane(Transform t, int L)
        {
            Part(t, "Base", PrimitiveType.Cube, MaterialLibrary.Purple, new Vector3(0, 0.12f, 0), new Vector3(1.6f, 0.24f, 1.6f));
            Part(t, "Trim", PrimitiveType.Cube, MaterialLibrary.Gold, new Vector3(0, 0.26f, 0), new Vector3(1.3f, 0.05f, 1.3f));
            float top = 0.3f;
            for (int i = 0; i <= L; i++)
            {   // colonnes de pierre + plateau violet, un étage par niveau
                for (int c = 0; c < 4; c++)
                {
                    float a = (c * 90f + 45f) * Mathf.Deg2Rad; float r = 0.5f - i * 0.05f;
                    Part(t, $"Column{i}_{c}", PrimitiveType.Cylinder, MaterialLibrary.Stone, new Vector3(Mathf.Cos(a) * r, top + 0.4f, Mathf.Sin(a) * r), new Vector3(0.14f, 0.4f, 0.14f));
                }
                top += 0.8f;
                Part(t, $"Deck{i}", PrimitiveType.Cube, MaterialLibrary.Purple, new Vector3(0, top + 0.08f, 0), new Vector3(1.2f - i * 0.1f, 0.16f, 1.2f - i * 0.1f));
                Part(t, $"DeckTrim{i}", PrimitiveType.Cube, MaterialLibrary.Gold, new Vector3(0, top + 0.17f, 0), new Vector3(0.9f - i * 0.1f, 0.03f, 0.9f - i * 0.1f));
                top += 0.2f;
            }
            for (int c = 0; c < 4; c++)
            {
                float a = c * 90f * Mathf.Deg2Rad;
                MeshPart(t, $"CornerCrystal{c}", MeshLibrary.Crystal(), MaterialLibrary.ArcaneCrystal, new Vector3(Mathf.Cos(a) * 0.55f, top + 0.1f, Mathf.Sin(a) * 0.55f), new Vector3(0.3f, 0.45f, 0.3f));
            }
            var turret = Node(t, "Turret", new Vector3(0, top + 0.6f, 0));
            MeshPart(turret, "Crystal", MeshLibrary.Crystal(), MaterialLibrary.ArcaneCrystal, Vector3.zero, new Vector3(0.7f + L * 0.15f, 0.9f + L * 0.2f, 0.7f + L * 0.15f));
            return top + 0.9f;
        }

        // ---------- ennemis ----------
        public static GameObject EnemyVisual(string id)
        {
            string path = $"{BastionPaths.PrefEnemyVisuals}/V_{id}.prefab";
            var existing = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var root = new GameObject($"V_{id}"); var t = root.transform;
            switch (id)
            {
                case "goblin":
                    Part(t, "Body", PrimitiveType.Capsule, MaterialLibrary.Green, new Vector3(0, 0.45f, 0), new Vector3(0.45f, 0.4f, 0.45f));
                    MeshPart(t, "Hat", MeshLibrary.Cone(), MaterialLibrary.RedDark, new Vector3(0, 0.8f, 0), new Vector3(0.4f, 0.4f, 0.4f));
                    Part(t, "Eye", PrimitiveType.Sphere, MaterialLibrary.EyeRed, new Vector3(0.1f, 0.6f, 0.18f), Vector3.one * 0.08f);
                    Part(t, "Eye2", PrimitiveType.Sphere, MaterialLibrary.EyeRed, new Vector3(-0.1f, 0.6f, 0.18f), Vector3.one * 0.08f);
                    break;
                case "ogre":
                    Part(t, "Body", PrimitiveType.Capsule, MaterialLibrary.Brown, new Vector3(0, 0.75f, 0), new Vector3(0.9f, 0.75f, 0.9f));
                    Part(t, "ShoulderL", PrimitiveType.Sphere, MaterialLibrary.Metal, new Vector3(0.5f, 1.15f, 0), Vector3.one * 0.4f);
                    Part(t, "ShoulderR", PrimitiveType.Sphere, MaterialLibrary.Metal, new Vector3(-0.5f, 1.15f, 0), Vector3.one * 0.4f);
                    Part(t, "Belt", PrimitiveType.Cylinder, MaterialLibrary.WoodDark, new Vector3(0, 0.7f, 0), new Vector3(0.95f, 0.06f, 0.95f));
                    Part(t, "Eye", PrimitiveType.Sphere, MaterialLibrary.EyeRed, new Vector3(0, 1.25f, 0.4f), Vector3.one * 0.14f);
                    break;
                case "bat":
                    Part(t, "Body", PrimitiveType.Sphere, MaterialLibrary.BatPurple, new Vector3(0, 0, 0), new Vector3(0.45f, 0.4f, 0.45f));
                    Part(t, "WingL", PrimitiveType.Cube, MaterialLibrary.BatPurple, new Vector3(0.45f, 0.05f, 0), new Vector3(0.6f, 0.04f, 0.35f), new Vector3(0, 0, 15));
                    Part(t, "WingR", PrimitiveType.Cube, MaterialLibrary.BatPurple, new Vector3(-0.45f, 0.05f, 0), new Vector3(0.6f, 0.04f, 0.35f), new Vector3(0, 0, -15));
                    Part(t, "Eye", PrimitiveType.Sphere, MaterialLibrary.EyeRed, new Vector3(0.09f, 0.05f, 0.2f), Vector3.one * 0.07f);
                    Part(t, "Eye2", PrimitiveType.Sphere, MaterialLibrary.EyeRed, new Vector3(-0.09f, 0.05f, 0.2f), Vector3.one * 0.07f);
                    break;
                case "frostgolem":
                    Part(t, "Body", PrimitiveType.Cube, MaterialLibrary.Ice, new Vector3(0, 0.6f, 0), new Vector3(0.7f, 0.8f, 0.6f), new Vector3(0, 45, 0));
                    Part(t, "Head", PrimitiveType.Cube, MaterialLibrary.Snow, new Vector3(0, 1.15f, 0), Vector3.one * 0.4f, new Vector3(0, 45, 0));
                    for (int i = 0; i < 3; i++) MeshPart(t, $"Spike{i}", MeshLibrary.Crystal(), MaterialLibrary.Ice, new Vector3(-0.2f + i * 0.2f, 1.3f, -0.15f), new Vector3(0.2f, 0.4f, 0.2f), new Vector3(-20, 0, i * 15 - 15));
                    break;
                case "icewyrm":
                    Part(t, "Body", PrimitiveType.Capsule, MaterialLibrary.Ice, new Vector3(0, 0, 0), new Vector3(0.7f, 1.1f, 0.7f), new Vector3(90, 0, 0));
                    Part(t, "Head", PrimitiveType.Sphere, MaterialLibrary.Snow, new Vector3(0, 0.1f, 1.2f), Vector3.one * 0.7f);
                    Part(t, "WingL", PrimitiveType.Cube, MaterialLibrary.Ice, new Vector3(1.1f, 0.1f, 0), new Vector3(1.6f, 0.05f, 0.8f), new Vector3(0, 0, 12));
                    Part(t, "WingR", PrimitiveType.Cube, MaterialLibrary.Ice, new Vector3(-1.1f, 0.1f, 0), new Vector3(1.6f, 0.05f, 0.8f), new Vector3(0, 0, -12));
                    for (int i = 0; i < 4; i++) MeshPart(t, $"Spine{i}", MeshLibrary.Crystal(), MaterialLibrary.Ice, new Vector3(0, 0.4f, 0.6f - i * 0.45f), new Vector3(0.25f, 0.45f, 0.25f));
                    Part(t, "Eye", PrimitiveType.Sphere, MaterialLibrary.EyeRed, new Vector3(0.2f, 0.2f, 1.5f), Vector3.one * 0.14f);
                    Part(t, "Eye2", PrimitiveType.Sphere, MaterialLibrary.EyeRed, new Vector3(-0.2f, 0.2f, 1.5f), Vector3.one * 0.14f);
                    MeshPart(t, "Aura", MeshLibrary.Ring(0.7f), MaterialLibrary.Additive("AuraIce", "#7CC6FF"), new Vector3(0, -1.5f, 0), new Vector3(2.6f, 1, 2.6f));
                    break;
                default: // warlord (boss)
                    Part(t, "Body", PrimitiveType.Capsule, MaterialLibrary.RedDark, new Vector3(0, 1.0f, 0), new Vector3(1.2f, 1.0f, 1.2f));
                    Part(t, "Armor", PrimitiveType.Cylinder, MaterialLibrary.Metal, new Vector3(0, 1.0f, 0), new Vector3(1.3f, 0.35f, 1.3f));
                    MeshPart(t, "HornL", MeshLibrary.Cone(), MaterialLibrary.Skull, new Vector3(0.35f, 1.9f, 0), new Vector3(0.2f, 0.6f, 0.2f), new Vector3(0, 0, -30));
                    MeshPart(t, "HornR", MeshLibrary.Cone(), MaterialLibrary.Skull, new Vector3(-0.35f, 1.9f, 0), new Vector3(0.2f, 0.6f, 0.2f), new Vector3(0, 0, 30));
                    Part(t, "Eye", PrimitiveType.Sphere, MaterialLibrary.EyeRed, new Vector3(0.18f, 1.6f, 0.5f), Vector3.one * 0.16f);
                    Part(t, "Eye2", PrimitiveType.Sphere, MaterialLibrary.EyeRed, new Vector3(-0.18f, 1.6f, 0.5f), Vector3.one * 0.16f);
                    MeshPart(t, "Aura", MeshLibrary.Ring(0.7f), MaterialLibrary.Additive("AuraBoss", "#FF3A3A"), new Vector3(0, 0.05f, 0), new Vector3(2.6f, 1, 2.6f));
                    break;
            }
            return BastionPaths.SavePrefab(root, path);
        }

        // ---------- prefabs logiques (ceux qui portent les scripts) ----------
        public static Tower TowerPrefab()
        {
            string path = $"{BastionPaths.PrefTowers}/Tower.prefab";
            var existing = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<Tower>();
            var root = new GameObject("Tower"); var t = root.transform;
            var visual = Node(t, "Visual", Vector3.zero);
            var range = Node(t, "RangeIndicator", Vector3.zero);
            var disc = Part(range, "Disc", PrimitiveType.Cylinder, MaterialLibrary.RangeDisc, new Vector3(0, 0.05f, 0), new Vector3(10, 0.01f, 10));
            NoShadows(disc); disc.SetActive(false);
            var ri = range.gameObject.AddComponent<RangeIndicator>();
            SetField(ri, "disc", disc.transform);
            var auraGo = MeshPart(t, "Aura", MeshLibrary.Ring(0.82f), MaterialLibrary.AuraAdd, new Vector3(0, 0.03f, 0), new Vector3(2.2f, 1, 2.2f));
            NoShadows(auraGo);
            var aura = auraGo.AddComponent<AuraRing>();
            SetField(aura, "target", auraGo.GetComponent<Renderer>());
            var tower = root.AddComponent<Tower>();
            SetField(tower, "visualRoot", visual); SetField(tower, "rangeIndicator", ri); SetField(tower, "aura", aura);
            return BastionPaths.SavePrefab(root, path).GetComponent<Tower>();
        }

        public static Enemy EnemyPrefab()
        {
            string path = $"{BastionPaths.PrefEnemies}/Enemy.prefab";
            var existing = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<Enemy>();
            var root = new GameObject("Enemy"); var t = root.transform;
            var visual = Node(t, "Visual", Vector3.zero);
            var hit = Node(t, "HitPoint", new Vector3(0, 0.7f, 0));
            var bar = Node(t, "HealthBar", new Vector3(0, 1.7f, 0));
            var back = Part(bar, "Back", PrimitiveType.Quad, MaterialLibrary.HealthBack, Vector3.zero, new Vector3(1.0f, 0.12f, 1));
            var fill = Part(bar, "Fill", PrimitiveType.Quad, MaterialLibrary.HealthFill, new Vector3(0, 0, -0.001f), new Vector3(0.96f, 0.08f, 1));
            NoShadows(back); NoShadows(fill);
            var hb = bar.gameObject.AddComponent<HealthBar>();
            SetField(hb, "fill", fill.transform); SetField(hb, "fillRenderer", fill.GetComponent<Renderer>());
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(MaterialLibrary.Hex("#FF3B3B"), 0f), new GradientColorKey(MaterialLibrary.Hex("#F5B62A"), 0.5f), new GradientColorKey(MaterialLibrary.Hex("#3DDC84"), 1f) },
                      new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
            SetField(hb, "colorByHealth", g);
            root.AddComponent<PathFollower>();
            var e = root.AddComponent<Enemy>();
            SetField(e, "visualRoot", visual); SetField(e, "hitPoint", hit); SetField(e, "healthBar", hb);
            return BastionPaths.SavePrefab(root, path).GetComponent<Enemy>();
        }

        public static Projectile ProjectilePrefab(string id, bool ballistic, Material mat, Mesh mesh = null, PrimitiveType prim = PrimitiveType.Sphere, Vector3? scale = null)
        {
            string path = $"{BastionPaths.PrefProjectiles}/P_{id}.prefab";
            var existing = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<Projectile>();
            var root = new GameObject($"P_{id}");
            var body = mesh != null ? MeshPart(root.transform, "Body", mesh, mat, Vector3.zero, scale ?? Vector3.one * 0.3f, new Vector3(90, 0, 0))
                                    : Part(root.transform, "Body", prim, mat, Vector3.zero, scale ?? Vector3.one * 0.3f);
            NoShadows(body);
            var trail = root.AddComponent<TrailRenderer>();
            trail.time = 0.25f; trail.startWidth = 0.18f; trail.endWidth = 0f; trail.sharedMaterial = MaterialLibrary.AuraAdd;
            trail.minVertexDistance = 0.05f; trail.shadowCastingMode = ShadowCastingMode.Off; trail.receiveShadows = false;
            var p = root.AddComponent<Projectile>();
            SetField(p, "ballistic", ballistic); SetField(p, "trail", trail); SetField(p, "tintTarget", body.GetComponent<Renderer>());
            SetField(p, "arcHeight", ballistic ? 2.5f : 0f);
            return BastionPaths.SavePrefab(root, path).GetComponent<Projectile>();
        }

        /// <summary>Renseigne un champ [SerializeField] privé via SerializedObject (le prefab reste propre, sans réflexion runtime).</summary>
        public static void SetField(Object target, string field, object value)
        {
            var so = new UnityEditor.SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"Champ {field} introuvable sur {target.name}"); return; }
            switch (value)
            {
                case Object o: p.objectReferenceValue = o; break;
                case bool b: p.boolValue = b; break;
                case float f: p.floatValue = f; break;
                case int i: p.intValue = i; break;
                case string s: p.stringValue = s; break;
                case Color c: p.colorValue = c; break;
                case Vector3 v: p.vector3Value = v; break;
                case Gradient g: p.gradientValue = g; break;
                case LayerMask lm: p.intValue = lm.value; break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetArray(Object target, string field, Object[] values)
        {
            var so = new UnityEditor.SerializedObject(target);
            var p = so.FindProperty(field);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
