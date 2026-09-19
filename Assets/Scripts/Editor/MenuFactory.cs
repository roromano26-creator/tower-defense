using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using Bastion.UI;
using Bastion.UI.Menu;
using Bastion.Towers;

namespace Bastion.EditorTools
{
    /// <summary>Scène Menu : vitrine 3D de tours qui tourne, titre, cartes de niveaux, difficulté, boutique d'améliorations.</summary>
    public static class MenuFactory
    {
        public const string MenuScene = "Assets/Scenes/Menu.unity";

        public static void Build(TowerData[] towers, VolumeProfile post)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.color = MaterialLibrary.Hex("#FFF1D6"); sun.intensity = 1.5f; sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = MaterialLibrary.Hex("#5B6C9E"); RenderSettings.ambientEquatorColor = MaterialLibrary.Hex("#3A3F5C"); RenderSettings.ambientGroundColor = MaterialLibrary.Hex("#16161F");
            RenderSettings.fog = true; RenderSettings.fogColor = MaterialLibrary.Hex("#11111A"); RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 20f; RenderSettings.fogEndDistance = 45f;

            var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = MaterialLibrary.Hex("#11111A"); cam.fieldOfView = 34f; cam.allowHDR = true;
            camGo.transform.position = new Vector3(0f, 7.5f, -13f); camGo.transform.rotation = Quaternion.Euler(24f, 0f, 0f);
            var cd = camGo.AddComponent<UniversalAdditionalCameraData>(); cd.renderPostProcessing = true;
            camGo.AddComponent<AudioListener>();
            var vol = new GameObject("PostProcess").AddComponent<Volume>(); vol.isGlobal = true; vol.sharedProfile = post;

            // Vitrine : un plateau sombre, les cinq tours au niveau 3 en cercle, qui tourne lentement derrière l'UI.
            var showcase = new GameObject("Showcase"); showcase.transform.position = new Vector3(0f, -0.5f, 2f);
            showcase.AddComponent<MenuShowcase>();
            var plate = PlaceholderFactory.Part(showcase.transform, "Plate", PrimitiveType.Cylinder, MaterialLibrary.GroundBuildable, Vector3.zero, new Vector3(9f, 0.2f, 9f));
            plate.isStatic = false;
            for (int i = 0; i < towers.Length; i++)
            {
                float a = i * Mathf.PI * 2f / towers.Length;
                var visual = towers[i].levels[towers[i].levels.Length - 1].visualPrefab;
                if (visual == null) continue;
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(visual, showcase.transform);
                inst.transform.localPosition = new Vector3(Mathf.Cos(a) * 3.2f, 0.2f, Mathf.Sin(a) * 3.2f);
                inst.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f);
            }

            BuildUI();

            EditorSceneManager.SaveScene(scene, MenuScene);
        }

        private static void BuildUI()
        {
            var canvasGo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            var safe = UIFactory.Rect("SafeArea", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            safe.gameObject.AddComponent<SafeArea>();

            var menu = safe.gameObject.AddComponent<MainMenu>();

            // Bandeau haut : titre, étoiles, gemmes, bouton Améliorations.
            var title = UIFactory.Text("Title", safe, "BASTION", 72, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -24), new Vector2(600, 90), TextAlignmentOptions.Left, Color.white);
            title.fontStyle = FontStyles.Bold; title.characterSpacing = 12;
            UIFactory.Text("Tagline", safe, "Défendez le bastion. Montez en puissance. Tenez sans fin.", 24, new Vector2(0, 1), new Vector2(0, 1), new Vector2(44, -104), new Vector2(900, 40), TextAlignmentOptions.Left, new Color(1, 1, 1, 0.7f));
            var stars = UIFactory.Text("Stars", safe, "0 ★", 34, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-420, -30), new Vector2(160, 60), TextAlignmentOptions.Right, new Color(1f, 0.8f, 0.3f));
            var gems = UIFactory.Text("Gems", safe, "0 ◆", 34, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-250, -30), new Vector2(150, 60), TextAlignmentOptions.Right, new Color(0.55f, 0.85f, 1f));
            var upgrades = UIFactory.Button("Upgrades", safe, "Améliorations", new Color(0.24f, 0.86f, 0.52f), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -20), new Vector2(210, 80), 26);

            // Sélecteur de difficulté.
            var diffRow = UIFactory.Rect("Difficulty", safe, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -230), new Vector2(760, -160));
            var diffLayout = diffRow.gameObject.AddComponent<HorizontalLayoutGroup>(); diffLayout.spacing = 10; diffLayout.childControlWidth = true; diffLayout.childControlHeight = true; diffLayout.childForceExpandWidth = true;
            var diffButtons = new Button[Core.DifficultySettings.Count];
            for (int i = 0; i < diffButtons.Length; i++)
                diffButtons[i] = UIFactory.Button($"Diff{i}", diffRow, Core.DifficultySettings.Label((Core.Difficulty)i), new Color(0.15f, 0.15f, 0.25f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 24);

            // Sélecteur de mode : Défense (tours) / Assaut (monstres).
            var modeRow = UIFactory.Rect("Mode", safe, new Vector2(0, 1), new Vector2(0, 1), new Vector2(820, -230), new Vector2(1300, -160));
            var modeLayout = modeRow.gameObject.AddComponent<HorizontalLayoutGroup>(); modeLayout.spacing = 10; modeLayout.childControlWidth = true; modeLayout.childControlHeight = true; modeLayout.childForceExpandWidth = true;
            var modeButtons = new Button[Core.GameModes.Count];
            for (int i = 0; i < modeButtons.Length; i++)
                modeButtons[i] = UIFactory.Button($"Mode{i}", modeRow, Core.GameModes.Label((Core.GameMode)i), new Color(0.15f, 0.15f, 0.25f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 24);

            // Liste de niveaux : défilement horizontal de cartes.
            var scroll = UIFactory.Rect("Levels", safe, new Vector2(0, 0), new Vector2(1, 1), new Vector2(40, 40), new Vector2(-40, -250));
            var sr = scroll.gameObject.AddComponent<ScrollRect>(); sr.horizontal = true; sr.vertical = false; sr.movementType = ScrollRect.MovementType.Elastic; sr.scrollSensitivity = 30;
            var viewport = UIFactory.Rect("Viewport", scroll, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = UIFactory.Rect("Content", viewport, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(0, 0.5f);
            var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.spacing = 24; layout.padding = new RectOffset(8, 8, 8, 8); layout.childControlWidth = false; layout.childControlHeight = true; layout.childForceExpandWidth = false; layout.childAlignment = TextAnchor.MiddleLeft;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>(); fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport = viewport; sr.content = content;

            var shop = BuildShop(safe);
            var reset = UIFactory.Button("Reset", safe, "Réinitialiser le profil", new Color(0.3f, 0.12f, 0.12f), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 16), new Vector2(280, 56), 18);

            PlaceholderFactory.SetField(menu, "cardPrefab", LevelCardPrefab()); PlaceholderFactory.SetField(menu, "cardsRoot", content);
            PlaceholderFactory.SetField(menu, "gemsText", gems); PlaceholderFactory.SetField(menu, "starsText", stars);
            PlaceholderFactory.SetField(menu, "upgradesButton", upgrades); PlaceholderFactory.SetField(menu, "shop", shop);
            PlaceholderFactory.SetField(menu, "resetButton", reset);
            PlaceholderFactory.SetArray(menu, "difficultyButtons", diffButtons);
            PlaceholderFactory.SetArray(menu, "modeButtons", modeButtons);
        }

        private static UpgradeShop BuildShop(RectTransform safe)
        {
            var overlay = UIFactory.Image("ShopOverlay", safe, new Color(0, 0, 0, 0.7f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var rt = overlay.rectTransform; rt.gameObject.AddComponent<CanvasGroup>(); var pulse = rt.gameObject.AddComponent<UIPulse>();
            var panel = UIFactory.Pill("Panel", rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 760));
            var t = UIFactory.Text("Title", panel, "Améliorations permanentes", 40, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -20), new Vector2(-300, -80), TextAlignmentOptions.Left, Color.white); t.fontStyle = FontStyles.Bold;
            var gems = UIFactory.Text("Gems", panel, "0 ◆", 34, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-110, -30), new Vector2(200, 60), TextAlignmentOptions.Right, new Color(0.55f, 0.85f, 1f));
            var close = UIFactory.Button("Close", panel, "✕", new Color(0.2f, 0.2f, 0.32f), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -16), new Vector2(64, 64), 28);
            var rows = UIFactory.Rect("Rows", panel, new Vector2(0, 0), new Vector2(1, 1), new Vector2(24, 24), new Vector2(-24, -100));
            var vl = rows.gameObject.AddComponent<VerticalLayoutGroup>(); vl.spacing = 10; vl.childControlWidth = true; vl.childControlHeight = true; vl.childForceExpandHeight = true;
            var shop = rt.gameObject.AddComponent<UpgradeShop>();
            PlaceholderFactory.SetField(shop, "pulse", pulse); PlaceholderFactory.SetField(shop, "rowsRoot", rows);
            PlaceholderFactory.SetField(shop, "rowPrefab", UpgradeRowPrefab()); PlaceholderFactory.SetField(shop, "closeButton", close); PlaceholderFactory.SetField(shop, "gemsText", gems);
            return shop;
        }

        private static LevelCard LevelCardPrefab()
        {
            string path = $"{BastionPaths.PrefUI}/LevelCard.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<LevelCard>();
            var holder = new GameObject("Holder", typeof(RectTransform));
            var card = UIFactory.Pill("LevelCard", holder.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(460, 560));
            card.gameObject.AddComponent<LayoutElement>().preferredWidth = 460;
            var previewGo = new GameObject("Preview", typeof(RectTransform), typeof(RawImage)); previewGo.transform.SetParent(card, false);
            var prt = (RectTransform)previewGo.transform; prt.anchorMin = new Vector2(0, 1); prt.anchorMax = new Vector2(1, 1); prt.pivot = new Vector2(0.5f, 1); prt.anchoredPosition = new Vector2(0, -20); prt.sizeDelta = new Vector2(-40, 280);
            var preview = previewGo.GetComponent<RawImage>(); preview.raycastTarget = false;
            var title = UIFactory.Text("Title", card, "1. Niveau", 30, new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -320), new Vector2(-24, -310), TextAlignmentOptions.Left, Color.white); title.fontStyle = FontStyles.Bold;
            var sub = UIFactory.Text("Subtitle", card, "", 20, new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -410), new Vector2(-24, -355), TextAlignmentOptions.TopLeft, new Color(1, 1, 1, 0.7f));
            var stars = UIFactory.Text("Stars", card, "☆☆☆", 36, new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(24, 60), new Vector2(0, 120), TextAlignmentOptions.Left, new Color(1f, 0.8f, 0.3f));
            var record = UIFactory.Text("Record", card, "", 18, new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(0, 60), new Vector2(-24, 120), TextAlignmentOptions.Right, new Color(0.55f, 0.85f, 1f));
            var play = UIFactory.Button("Play", card, "Jouer", new Color(0.24f, 0.86f, 0.52f), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(400, 64), 26);
            var lockImg = UIFactory.Image("Lock", card, new Color(0, 0, 0, 0.72f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            lockImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); lockImg.type = Image.Type.Sliced; lockImg.pixelsPerUnitMultiplier = 0.35f;
            var lockText = UIFactory.Text("LockText", lockImg.transform, "🔒", 24, new Vector2(0, 0), new Vector2(1, 1), new Vector2(20, 0), new Vector2(-20, 0), TextAlignmentOptions.Center, Color.white);
            var comp = card.gameObject.AddComponent<LevelCard>();
            PlaceholderFactory.SetField(comp, "titleText", title); PlaceholderFactory.SetField(comp, "subtitleText", sub); PlaceholderFactory.SetField(comp, "starsText", stars);
            PlaceholderFactory.SetField(comp, "recordText", record); PlaceholderFactory.SetField(comp, "preview", preview); PlaceholderFactory.SetField(comp, "button", play);
            PlaceholderFactory.SetField(comp, "lockOverlay", lockImg.gameObject); PlaceholderFactory.SetField(comp, "lockText", lockText);
            card.SetParent(null, false); Object.DestroyImmediate(holder);
            return BastionPaths.SavePrefab(card.gameObject, path).GetComponent<LevelCard>();
        }

        private static UpgradeRow UpgradeRowPrefab()
        {
            string path = $"{BastionPaths.PrefUI}/UpgradeRow.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<UpgradeRow>();
            var holder = new GameObject("Holder", typeof(RectTransform));
            var row = UIFactory.Image("UpgradeRow", holder.transform, new Color(1, 1, 1, 0.06f), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, 90));
            var rrt = row.rectTransform; rrt.gameObject.AddComponent<LayoutElement>().preferredHeight = 90;
            var accent = UIFactory.Image("Accent", rrt, Color.green, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0), new Vector2(8, 0));
            var title = UIFactory.Text("Title", rrt, "Forge", 26, new Vector2(0, 1), new Vector2(0.55f, 1), new Vector2(24, -8), new Vector2(0, -44), TextAlignmentOptions.Left, Color.white); title.fontStyle = FontStyles.Bold;
            var desc = UIFactory.Text("Desc", rrt, "", 18, new Vector2(0, 0), new Vector2(0.55f, 1), new Vector2(24, 6), new Vector2(0, -44), TextAlignmentOptions.TopLeft, new Color(1, 1, 1, 0.7f));
            var rank = UIFactory.Text("Rank", rrt, "○○○○○", 24, new Vector2(0.55f, 0), new Vector2(0.8f, 1), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center, new Color(0.55f, 0.85f, 1f));
            var buy = UIFactory.Button("Buy", rrt, "20 ◆", new Color(0.24f, 0.86f, 0.52f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(180, 64), 24);
            var comp = rrt.gameObject.AddComponent<UpgradeRow>();
            PlaceholderFactory.SetField(comp, "titleText", title); PlaceholderFactory.SetField(comp, "descText", desc); PlaceholderFactory.SetField(comp, "rankText", rank);
            PlaceholderFactory.SetField(comp, "buyButton", buy); PlaceholderFactory.SetField(comp, "buyLabel", buy.GetComponentInChildren<TMP_Text>()); PlaceholderFactory.SetField(comp, "accent", accent);
            rrt.SetParent(null, false); Object.DestroyImmediate(holder);
            return BastionPaths.SavePrefab(rrt.gameObject, path).GetComponent<UpgradeRow>();
        }
    }
}
