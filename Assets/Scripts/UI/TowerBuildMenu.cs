using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Bastion.Core;
using Bastion.Towers;

namespace Bastion.UI
{
    /// <summary>
    /// Menu radial-ish de construction, ancré sous la cellule sélectionnée (projeté écran → canvas).
    /// Un premier tap sur une tour la prévisualise (fantôme + portée), un second confirme :
    /// sur mobile il n'y a pas de survol, la prévisualisation doit donc être un geste explicite.
    /// </summary>
    public class TowerBuildMenu : MonoBehaviour
    {
        public static TowerBuildMenu Instance { get; private set; }

        [SerializeField] private RectTransform panel;
        [SerializeField] private UIPulse pulse;
        [SerializeField] private Transform buttonsRoot;
        [SerializeField] private Button buttonPrefab;
        [SerializeField] private TMP_Text infoText;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private TowerPlacer placer;

        private readonly List<(Button b, TowerData d)> buttons = new();
        private TowerData previewed;
        private Vector3 anchorWorld;
        private bool open;

        private void Awake()
        {
            Instance = this;
            foreach (var d in TowerManager.Instance != null ? TowerManager.Instance.Catalog : new List<TowerData>())
            {
                var b = Instantiate(buttonPrefab, buttonsRoot);
                var label = b.GetComponentInChildren<TMP_Text>();
                label.text = $"{d.displayName}\n<size=70%>{MetaProgression.Cost(d.levels[0].cost)} or</size>";
                var img = b.GetComponent<Image>();
                if (img != null) img.color = d.accentColor;
                var data = d;
                b.onClick.AddListener(() => OnTowerTapped(data));
                buttons.Add((b, d));
            }
            GameEvents.OnGoldChanged += _ => RefreshAffordability();
            Close();
        }

        public void Open(Vector2Int cell, Vector3 world)
        {
            anchorWorld = world;
            previewed = null;
            infoText.text = "Choisis une tour";
            open = true;
            pulse.Show(true);
            RefreshAffordability();
        }

        public void Close()
        {
            open = false;
            previewed = null;
            if (pulse != null) pulse.Show(false);
        }

        private void OnTowerTapped(TowerData d)
        {
            if (previewed == d) { placer.ConfirmBuild(d); return; }
            previewed = d;
            placer.PreviewTower(d);
            var l = d.levels[0];
            infoText.text = $"<b>{d.displayName}</b> · {MetaProgression.Cost(l.cost)} or\n{d.description}\nDégâts {l.damage:0} · Portée {l.range:0.#} · Cadence {l.fireRate:0.#}/s\n<i>Touche à nouveau pour construire</i>";
        }

        private void RefreshAffordability()
        {
            foreach (var (b, d) in buttons)
                b.interactable = ResourceManager.Instance != null && ResourceManager.Instance.CanAfford(MetaProgression.Cost(d.levels[0].cost));
        }

        private void LateUpdate()
        {
            if (!open || worldCamera == null) return;
            // Le panneau suit la cellule quand la caméra bouge, et reste dans l'écran.
            var screen = worldCamera.WorldToScreenPoint(anchorWorld + Vector3.forward * -1.2f);
            var canvas = panel.parent as RectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screen, null, out var local);
            var half = panel.rect.size / 2f;
            var bounds = canvas.rect;
            local.x = Mathf.Clamp(local.x, bounds.xMin + half.x + 8, bounds.xMax - half.x - 8);
            local.y = Mathf.Clamp(local.y - half.y - 16, bounds.yMin + half.y + 8, bounds.yMax - half.y - 8);
            panel.anchoredPosition = local;
        }
    }
}
