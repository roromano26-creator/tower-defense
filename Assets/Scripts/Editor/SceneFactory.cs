using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Bastion.Core;
using Bastion.Grid;
using Bastion.Towers;
using Bastion.Enemies;
using Bastion.Waves;
using Bastion.VFX;
using Bastion.CameraRig;
using Bastion.UI;

namespace Bastion.EditorTools
{
    /// <summary>Assemble Main.unity : lumière, caméra, sol, managers, pools, UI. Regénérable : la scène est écrasée.</summary>
    public static class SceneFactory
    {
        public static void Build(LevelData level, TowerData[] towers, VolumeProfile post)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Éclairage : une directionnelle chaude + ambiance froide en dégradé (contraste chaud/froid du style cartoon) ---
            var sunGo = new GameObject("Sun"); var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional; sun.color = MaterialLibrary.Hex("#FFF1D6"); sun.intensity = 1.6f;
            sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.75f; sun.shadowBias = 0.03f; sun.shadowNormalBias = 0.6f;
            sunGo.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = MaterialLibrary.Hex("#5B6C9E");
            RenderSettings.ambientEquatorColor = MaterialLibrary.Hex("#3A3F5C");
            RenderSettings.ambientGroundColor = MaterialLibrary.Hex("#16161F");
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = MaterialLibrary.Hex("#11111A");
            RenderSettings.fogStartDistance = 34f; RenderSettings.fogEndDistance = 70f;
            RenderSettings.skybox = null;

            // --- Caméra + post-process ---
            var rig = new GameObject("CameraRig");
            var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera"; camGo.transform.SetParent(rig.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = MaterialLibrary.Hex("#11111A");
            cam.fieldOfView = 38f; cam.nearClipPlane = 1f; cam.farClipPlane = 90f; cam.allowHDR = true; cam.allowMSAA = true;
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true; camData.antialiasing = AntialiasingMode.None; camData.renderShadows = true;
            camGo.AddComponent<AudioListener>();
            var camCtrl = rig.AddComponent<CameraController>();
            PlaceholderFactory.SetField(camCtrl, "cam", cam);
            var volGo = new GameObject("PostProcess"); var vol = volGo.AddComponent<Volume>(); vol.isGlobal = true; vol.sharedProfile = post;
            var env = new GameObject("LevelEnvironment").AddComponent<LevelEnvironment>();
            PlaceholderFactory.SetField(env, "sun", sun); PlaceholderFactory.SetField(env, "cam", cam);

            // --- Managers ---
            var gm = new GameObject("GameManager").AddComponent<GameManager>();
            PlaceholderFactory.SetField(gm, "level", level);
            gm.gameObject.AddComponent<ResourceManager>();

            var groundRoot = new GameObject("Ground").transform;
            var basePlate = PlaceholderFactory.Part(groundRoot, "BasePlate", PrimitiveType.Cube, MaterialLibrary.GroundBase,
                new Vector3(level.width * level.cellSize / 2f, -0.6f, level.height * level.cellSize / 2f),
                new Vector3(level.width * level.cellSize + 10f, 1f, level.height * level.cellSize + 10f));
            basePlate.isStatic = true;
            var grid = new GameObject("GridManager").AddComponent<GridManager>();
            PlaceholderFactory.SetField(grid, "level", level); PlaceholderFactory.SetField(grid, "groundRoot", groundRoot);
            PlaceholderFactory.SetField(grid, "buildableMaterial", MaterialLibrary.GroundBuildable);
            PlaceholderFactory.SetField(grid, "pathMaterial", MaterialLibrary.GroundPath);
            PlaceholderFactory.SetField(grid, "blockedMaterial", MaterialLibrary.GroundBlocked);
            AddRocks(groundRoot, level);

            var poolsRoot = new GameObject("Pools").transform;
            var em = new GameObject("EnemyManager").AddComponent<EnemyManager>();
            PlaceholderFactory.SetField(em, "enemyPrefab", PlaceholderFactory.EnemyPrefab()); PlaceholderFactory.SetField(em, "poolRoot", poolsRoot);

            var tm = new GameObject("TowerManager").AddComponent<TowerManager>();
            PlaceholderFactory.SetArray(tm, "catalog", towers);
            PlaceholderFactory.SetField(tm, "towerPrefab", PlaceholderFactory.TowerPrefab());
            PlaceholderFactory.SetField(tm, "towersRoot", new GameObject("Towers").transform);
            PlaceholderFactory.SetField(tm, "projectilesRoot", poolsRoot);

            var vfx = new GameObject("VFXManager").AddComponent<VFXManager>();
            var entries = VFXFactory.All();
            var so = new SerializedObject(vfx); var arr = so.FindProperty("entries"); arr.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                var el = arr.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("kind").enumValueIndex = (int)entries[i].kind;
                el.FindPropertyRelative("prefab").objectReferenceValue = entries[i].prefab;
                el.FindPropertyRelative("prewarm").intValue = entries[i].prewarm;
            }
            so.FindProperty("beamPrefab").objectReferenceValue = VFXFactory.Beam();
            so.ApplyModifiedPropertiesWithoutUndo();

            new GameObject("WaveManager").AddComponent<WaveManager>();
            var assault = new GameObject("Assault");
            assault.AddComponent<Bastion.Assault.AssaultController>();
            assault.AddComponent<Bastion.Assault.AIDefender>();

            // --- Placement (surbrillance de cellule, fantôme, disque de portée) ---
            var placerGo = new GameObject("TowerPlacer"); var placer = placerGo.AddComponent<TowerPlacer>();
            var highlight = PlaceholderFactory.MeshPart(placerGo.transform, "CellHighlight", MeshLibrary.Ring(0.86f), MaterialLibrary.CellHighlight, Vector3.zero, new Vector3(level.cellSize * 0.95f, 1, level.cellSize * 0.95f));
            highlight.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off; highlight.SetActive(false);
            var rangeGo = new GameObject("PreviewRange"); rangeGo.transform.SetParent(placerGo.transform, false);
            var disc = PlaceholderFactory.Part(rangeGo.transform, "Disc", PrimitiveType.Cylinder, MaterialLibrary.RangeDisc, new Vector3(0, 0.05f, 0), new Vector3(10, 0.01f, 10));
            disc.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off; disc.SetActive(false);
            var ri = rangeGo.AddComponent<RangeIndicator>(); PlaceholderFactory.SetField(ri, "disc", disc.transform);
            var ghost = new GameObject("Ghost").transform; ghost.SetParent(placerGo.transform, false);
            PlaceholderFactory.SetField(placer, "cam", cam); PlaceholderFactory.SetField(placer, "cameraController", camCtrl);
            PlaceholderFactory.SetField(placer, "cellHighlight", highlight); PlaceholderFactory.SetField(placer, "previewRange", ri);
            PlaceholderFactory.SetField(placer, "ghostRoot", ghost); PlaceholderFactory.SetField(placer, "ghostMaterial", MaterialLibrary.Ghost);

            // --- Popups de dégâts ---
            var boot = new GameObject("SceneBootstrap").AddComponent<SceneBootstrap>();
            PlaceholderFactory.SetField(boot, "damagePopupPrefab", DamagePopupPrefab()); PlaceholderFactory.SetField(boot, "popupRoot", poolsRoot);

            // --- UI ---
            UIFactory.Build(cam, placer);

            EditorSceneManager.SaveScene(scene, BastionPaths.MainScene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MenuFactory.MenuScene, true), new EditorBuildSettingsScene(BastionPaths.MainScene, true) };
        }

        /// <summary>Rochers décoratifs sur les cellules bloquées : trois sphères écrasées, déterministes (seed = cellule).</summary>
        private static void AddRocks(Transform root, LevelData level)
        {
            foreach (var c in level.blocked)
            {
                var center = new Vector3((c.x + 0.5f) * level.cellSize, 0f, (c.y + 0.5f) * level.cellSize);
                var rnd = new System.Random(c.x * 131 + c.y * 17);
                for (int i = 0; i < 3; i++)
                {
                    float s = 0.5f + (float)rnd.NextDouble() * 0.6f;
                    var off = new Vector3(((float)rnd.NextDouble() - 0.5f) * 1.1f, s * 0.25f, ((float)rnd.NextDouble() - 0.5f) * 1.1f);
                    var rock = PlaceholderFactory.Part(root, $"Rock_{c.x}_{c.y}_{i}", PrimitiveType.Sphere, MaterialLibrary.StoneDark, center + off, new Vector3(s, s * 0.7f, s * 0.9f), new Vector3(0, (float)rnd.NextDouble() * 360f, 0));
                    rock.isStatic = true;
                }
            }
        }

        private static DamagePopup DamagePopupPrefab()
        {
            string path = $"{BastionPaths.PrefUI}/DamagePopup.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<DamagePopup>();
            var go = new GameObject("DamagePopup");
            var tmp = go.AddComponent<TMPro.TextMeshPro>();
            tmp.text = "12"; tmp.fontSize = 5f; tmp.alignment = TMPro.TextAlignmentOptions.Center; tmp.fontStyle = TMPro.FontStyles.Bold;
            tmp.outlineWidth = 0.2f; tmp.outlineColor = new Color32(0, 0, 0, 200);
            ((RectTransform)go.transform).sizeDelta = new Vector2(3, 1);
            var p = go.AddComponent<DamagePopup>();
            PlaceholderFactory.SetField(p, "text", tmp);
            return BastionPaths.SavePrefab(go, path).GetComponent<DamagePopup>();
        }
    }
}
