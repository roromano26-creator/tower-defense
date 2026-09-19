using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Bastion.UI;
using Bastion.Towers;

namespace Bastion.EditorTools
{
    /// <summary>
    /// UI mobile générée : Canvas Screen-Space, échelle 1920×1080 (match 0.5), zone sûre, boutons de
    /// 96 px minimum (≈ 48 dp) — le pouce, pas la souris. Pastilles arrondies sombres, accent vert.
    /// </summary>
    public static class UIFactory
    {
        private static readonly Color Panel = new(0.05f, 0.05f, 0.1f, 0.88f);
        private static readonly Color Brand = new(0.24f, 0.86f, 0.52f);
        private static readonly Color Danger = new(0.95f, 0.3f, 0.3f);
        private static readonly Color Ink = Color.white;

        public static Canvas Build(Camera cam, TowerPlacer placer)
        {
            EnsureTmpResources();
            var canvasGo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var safe = Rect("SafeArea", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            safe.gameObject.AddComponent<SafeArea>();

            BuildHud(safe, cam);
            BuildBuildMenu(safe, cam, placer);
            BuildUpgradePanel(safe, placer);
            BuildPauseMenu(safe);
            BuildEndScreen(safe);
            BuildDeployBar(safe);
            return canvas;
        }

        // ---------- HUD ----------
        private static void BuildHud(RectTransform safe, Camera cam)
        {
            var hudGo = new GameObject("HUD"); hudGo.transform.SetParent(safe, false);
            var hud = hudGo.AddComponent<HUD>();

            var top = Pill("TopBar", safe, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -16), new Vector2(760, 84));
            var lives = Text("Lives", top, "20", 36, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(90, 0), new Vector2(120, 60), TextAlignmentOptions.Left, Danger);
            var heart = Text("Heart", top, "♥", 40, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(45, 0), new Vector2(60, 60), TextAlignmentOptions.Center, Danger);
            var livesBar = Image("LivesBar", top, new Color(1, 1, 1, 0.12f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(120, -28), new Vector2(150, 8));
            var livesFill = Image("LivesFill", livesBar, Danger, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            livesFill.type = Image.Type.Filled; livesFill.fillMethod = Image.FillMethod.Horizontal;
            var gold = Text("Gold", top, "180", 36, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-20, 0), new Vector2(160, 60), TextAlignmentOptions.Right, new Color(1f, 0.8f, 0.3f));
            Text("Coin", top, "●", 30, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(85, 0), new Vector2(40, 60), TextAlignmentOptions.Center, new Color(1f, 0.8f, 0.3f));
            var wave = Text("Wave", top, "Vague 0", 30, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-90, 0), new Vector2(200, 60), TextAlignmentOptions.Right, Ink);

            var countdown = Text("Countdown", safe, "", 26, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -116), new Vector2(600, 40), TextAlignmentOptions.Center, new Color(1, 1, 1, 0.75f));
            var banner = Text("BossBanner", safe, "", 54, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 200), new Vector2(1200, 90), TextAlignmentOptions.Center, Danger);
            banner.fontStyle = FontStyles.Bold;

            var start = Button("StartWave", safe, "Lancer la vague", Brand, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 24), new Vector2(360, 96), 32);
            var speed = Button("Speed", safe, "×1", new Color(0.15f, 0.15f, 0.25f, 0.95f), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -16), new Vector2(110, 84), 32);
            var pause = Button("Pause", safe, "❚❚", new Color(0.15f, 0.15f, 0.25f, 0.95f), new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -16), new Vector2(96, 84), 30);

            PlaceholderFactory.SetField(hud, "goldText", gold); PlaceholderFactory.SetField(hud, "livesText", lives);
            PlaceholderFactory.SetField(hud, "livesFill", livesFill); PlaceholderFactory.SetField(hud, "waveText", wave);
            PlaceholderFactory.SetField(hud, "countdownText", countdown); PlaceholderFactory.SetField(hud, "bossBanner", banner);
            PlaceholderFactory.SetField(hud, "startWaveButton", start); PlaceholderFactory.SetField(hud, "startWaveLabel", start.GetComponentInChildren<TMP_Text>());
            PlaceholderFactory.SetField(hud, "speedButton", speed); PlaceholderFactory.SetField(hud, "speedLabel", speed.GetComponentInChildren<TMP_Text>());
            PlaceholderFactory.SetField(hud, "pauseButton", pause);
            _ = heart;
        }

        // ---------- barre de déploiement (mode Assaut) ----------
        private static void BuildDeployBar(RectTransform safe)
        {
            var bar = Pill("DeployBar", safe, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(1180, 150));
            var mana = Image("ManaBar", bar, new Color(1, 1, 1, 0.12f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -18), new Vector2(-24, -8));
            var manaFill = Image("ManaFill", mana.rectTransform, new Color(0.55f, 0.85f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            manaFill.type = Image.Type.Filled; manaFill.fillMethod = Image.FillMethod.Horizontal;
            var row = Rect("Buttons", bar, new Vector2(0, 0), new Vector2(1, 1), new Vector2(16, 10), new Vector2(-16, -26));
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10; layout.childForceExpandWidth = true; layout.childForceExpandHeight = true; layout.childControlWidth = true; layout.childControlHeight = true;
            var comp = bar.gameObject.AddComponent<MonsterDeployBar>();
            PlaceholderFactory.SetField(comp, "root", bar.gameObject); PlaceholderFactory.SetField(comp, "buttonsRoot", row);
            PlaceholderFactory.SetField(comp, "buttonPrefab", TowerButtonPrefab()); PlaceholderFactory.SetField(comp, "manaFill", manaFill);
        }

        // ---------- menu de construction ----------
        private static void BuildBuildMenu(RectTransform safe, Camera cam, TowerPlacer placer)
        {
            var panel = Pill("BuildMenu", safe, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 250));
            var pulse = panel.gameObject.AddComponent<CanvasGroup>(); var up = panel.gameObject.AddComponent<UIPulse>();
            var row = Rect("Buttons", panel, new Vector2(0, 0.35f), new Vector2(1, 1), new Vector2(16, 0), new Vector2(-16, -12));
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12; layout.childForceExpandWidth = true; layout.childForceExpandHeight = true; layout.childControlWidth = true; layout.childControlHeight = true;
            var info = Text("Info", panel, "Choisis une tour", 22, new Vector2(0, 0), new Vector2(1, 0.35f), new Vector2(20, 8), new Vector2(-20, -4), TextAlignmentOptions.Center, new Color(1, 1, 1, 0.85f));
            info.richText = true;

            var btnPrefab = TowerButtonPrefab();
            var menu = panel.gameObject.AddComponent<TowerBuildMenu>();
            PlaceholderFactory.SetField(menu, "panel", panel); PlaceholderFactory.SetField(menu, "pulse", up);
            PlaceholderFactory.SetField(menu, "buttonsRoot", row); PlaceholderFactory.SetField(menu, "buttonPrefab", btnPrefab);
            PlaceholderFactory.SetField(menu, "infoText", info); PlaceholderFactory.SetField(menu, "worldCamera", cam); PlaceholderFactory.SetField(menu, "placer", placer);
            _ = pulse;
        }

        private static Button TowerButtonPrefab()
        {
            string path = $"{BastionPaths.PrefUI}/TowerButton.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<Button>();
            var holder = new GameObject("Holder", typeof(RectTransform));
            var b = Button("TowerButton", (RectTransform)holder.transform, "Tour\n<size=70%>0 or</size>", Brand, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(150, 130), 24);
            b.GetComponentInChildren<TMP_Text>().richText = true;
            var le = b.gameObject.AddComponent<LayoutElement>(); le.minHeight = 110; le.minWidth = 140;
            b.transform.SetParent(null, false);
            Object.DestroyImmediate(holder);
            return BastionPaths.SavePrefab(b.gameObject, path).GetComponent<Button>();
        }

        // ---------- panneau d'upgrade ----------
        private static void BuildUpgradePanel(RectTransform safe, TowerPlacer placer)
        {
            var panel = Pill("UpgradePanel", safe, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-120, 24), new Vector2(760, 210));
            panel.gameObject.AddComponent<CanvasGroup>(); var up = panel.gameObject.AddComponent<UIPulse>();
            var title = Text("Title", panel, "Tour", 30, new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -14), new Vector2(-80, -52), TextAlignmentOptions.Left, Ink); title.fontStyle = FontStyles.Bold; title.richText = true;
            var stats = Text("Stats", panel, "", 22, new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -56), new Vector2(-24, -90), TextAlignmentOptions.Left, new Color(1, 1, 1, 0.8f));
            var upgrade = Button("Upgrade", panel, "Améliorer", Brand, new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 16), new Vector2(300, 96), 24);
            var sell = Button("Sell", panel, "Vendre", new Color(0.5f, 0.35f, 0.1f), new Vector2(0, 0), new Vector2(0, 0), new Vector2(336, 16), new Vector2(190, 96), 24);
            var prio = Button("Priority", panel, "Cible : premier", new Color(0.2f, 0.2f, 0.32f), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-20, 16), new Vector2(200, 96), 22);
            var close = Button("Close", panel, "✕", new Color(0.2f, 0.2f, 0.32f), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-14, -12), new Vector2(64, 64), 28);
            foreach (var b in new[] { upgrade, sell, prio }) b.GetComponentInChildren<TMP_Text>().richText = true;

            var comp = panel.gameObject.AddComponent<TowerUpgradePanel>();
            PlaceholderFactory.SetField(comp, "pulse", up); PlaceholderFactory.SetField(comp, "titleText", title); PlaceholderFactory.SetField(comp, "statsText", stats);
            PlaceholderFactory.SetField(comp, "upgradeButton", upgrade); PlaceholderFactory.SetField(comp, "upgradeLabel", upgrade.GetComponentInChildren<TMP_Text>());
            PlaceholderFactory.SetField(comp, "sellButton", sell); PlaceholderFactory.SetField(comp, "sellLabel", sell.GetComponentInChildren<TMP_Text>());
            PlaceholderFactory.SetField(comp, "priorityButton", prio); PlaceholderFactory.SetField(comp, "priorityLabel", prio.GetComponentInChildren<TMP_Text>());
            PlaceholderFactory.SetField(comp, "closeButton", close); PlaceholderFactory.SetField(comp, "placer", placer);
        }

        // ---------- pause ----------
        private static void BuildPauseMenu(RectTransform safe)
        {
            var overlay = Image("PauseOverlay", safe, new Color(0, 0, 0, 0.55f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var rt = overlay.rectTransform;
            rt.gameObject.AddComponent<CanvasGroup>(); var up = rt.gameObject.AddComponent<UIPulse>();
            var panel = Pill("Panel", rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 420));
            var title = Text("Title", panel, "Pause", 44, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -20), new Vector2(0, -90), TextAlignmentOptions.Center, Ink); title.fontStyle = FontStyles.Bold;
            var resume = Button("Resume", panel, "Reprendre", Brand, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(420, 96), 30);
            var restart = Button("Restart", panel, "Recommencer", new Color(0.2f, 0.2f, 0.32f), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -216), new Vector2(420, 96), 30);
            var menuBtn = Button("Menu", panel, "Menu principal", new Color(0.2f, 0.2f, 0.32f), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -322), new Vector2(420, 84), 26);
            var quit = Button("Quit", panel, "Quitter", new Color(0.4f, 0.15f, 0.15f), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -416), new Vector2(420, 70), 22);
            panel.sizeDelta = new Vector2(520, 520);
            var comp = rt.gameObject.AddComponent<PauseMenu>();
            PlaceholderFactory.SetField(comp, "pulse", up); PlaceholderFactory.SetField(comp, "resumeButton", resume);
            PlaceholderFactory.SetField(comp, "restartButton", restart); PlaceholderFactory.SetField(comp, "quitButton", quit); PlaceholderFactory.SetField(comp, "menuButton", menuBtn);
        }

        // ---------- fin de partie ----------
        private static void BuildEndScreen(RectTransform safe)
        {
            var overlay = Image("EndOverlay", safe, new Color(0, 0, 0, 0.7f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var rt = overlay.rectTransform;
            rt.gameObject.AddComponent<CanvasGroup>(); var up = rt.gameObject.AddComponent<UIPulse>();
            var panel = Pill("Panel", rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 560));
            var title = Text("Title", panel, "Victoire !", 56, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -20), new Vector2(0, -110), TextAlignmentOptions.Center, Brand); title.fontStyle = FontStyles.Bold;
            var stars = Text("Stars", panel, "★★★", 64, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -110), new Vector2(0, -200), TextAlignmentOptions.Center, new Color(1f, 0.8f, 0.3f));
            var sub = Text("Subtitle", panel, "", 26, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -200), new Vector2(-30, -270), TextAlignmentOptions.Center, new Color(1, 1, 1, 0.85f));
            var gems = Text("Gems", panel, "", 30, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -270), new Vector2(-30, -320), TextAlignmentOptions.Center, new Color(0.55f, 0.85f, 1f));
            var endless = Button("Endless", panel, "Continuer · Sans fin", new Color(0.55f, 0.3f, 0.85f), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 130), new Vector2(660, 80), 26);
            var next = Button("Next", panel, "Niveau suivant", Brand, new Vector2(0, 0), new Vector2(0, 0), new Vector2(24, 28), new Vector2(340, 90), 26);
            var restart = Button("Restart", panel, "Rejouer", new Color(0.2f, 0.2f, 0.32f), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 28), new Vector2(180, 90), 24);
            var menuBtn = Button("Menu", panel, "Menu", new Color(0.2f, 0.2f, 0.32f), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-216, 28), new Vector2(160, 90), 24);
            var comp = rt.gameObject.AddComponent<EndScreen>();
            PlaceholderFactory.SetField(comp, "pulse", up); PlaceholderFactory.SetField(comp, "titleText", title);
            PlaceholderFactory.SetField(comp, "subtitleText", sub); PlaceholderFactory.SetField(comp, "starsText", stars); PlaceholderFactory.SetField(comp, "restartButton", restart);
            PlaceholderFactory.SetField(comp, "nextButton", next); PlaceholderFactory.SetField(comp, "menuButton", menuBtn); PlaceholderFactory.SetField(comp, "gemsText", gems);
            PlaceholderFactory.SetField(comp, "endlessButton", endless);
        }

        // ---------- briques ----------
        public static RectTransform Rect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = offMin; rt.offsetMax = offMax;
            return rt;
        }

        public static Image Image(string name, Transform parent, Color color, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = aMin; rt.anchorMax = aMax;
            if (aMin == aMax) { rt.pivot = aMin; rt.anchoredPosition = pos; rt.sizeDelta = size; } else { rt.offsetMin = pos; rt.offsetMax = size; }
            var img = go.GetComponent<Image>(); img.color = color; img.raycastTarget = true;
            return img;
        }

        /// <summary>Pastille arrondie sombre : le sprite « UISprite » intégré, en sliced, donne des coins ronds sans asset.</summary>
        public static RectTransform Pill(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
        {
            var img = Image(name, parent, Panel, aMin, aMax, pos, size);
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 0.35f;
            return img.rectTransform;
        }

        public static TMP_Text Text(string name, Transform parent, string content, float size, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 sz, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = aMin; rt.anchorMax = aMax;
            if (aMin == aMax) { rt.pivot = aMin; rt.anchoredPosition = pos; rt.sizeDelta = sz; } else { rt.offsetMin = pos; rt.offsetMax = sz; }
            var t = go.GetComponent<TextMeshProUGUI>(); t.text = content; t.fontSize = size; t.alignment = align; t.color = color; t.raycastTarget = false;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        public static Button Button(string name, Transform parent, string label, Color color, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, float fontSize)
        {
            var img = Image(name, parent, color, aMin, aMax, pos, size);
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 0.5f;
            var b = img.gameObject.AddComponent<Button>();
            var colors = b.colors; colors.pressedColor = new Color(0.8f, 0.8f, 0.8f); colors.disabledColor = new Color(1, 1, 1, 0.35f); colors.fadeDuration = 0.05f; b.colors = colors;
            var t = Text("Label", img.transform, label, fontSize, Vector2.zero, Vector2.one, new Vector2(8, 4), new Vector2(-8, -4), TextAlignmentOptions.Center, Ink);
            t.fontStyle = FontStyles.Bold;
            return b;
        }

        /// <summary>Importe les ressources essentielles TextMeshPro si absentes (police par défaut), via réflexion : l'API a changé de nom entre versions.</summary>
        private static void EnsureTmpResources()
        {
            if (TMP_Settings.instance != null && TMP_Settings.defaultFontAsset != null) return;
            var type = System.Type.GetType("TMPro.EditorUtilities.TMP_PackageResourceImporter, Unity.TextMeshPro.Editor")
                    ?? System.Type.GetType("TMPro.EditorUtilities.TMP_PackageResourceImporter, Unity.ugui.Editor");
            var m = type?.GetMethod("ImportResources", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (m != null) { m.Invoke(null, new object[] { true, false, false }); AssetDatabase.Refresh(); }
            else Debug.LogWarning("TextMeshPro : importez « TMP Essential Resources » (Window → TextMeshPro) puis relancez la génération.");
        }
    }
}
