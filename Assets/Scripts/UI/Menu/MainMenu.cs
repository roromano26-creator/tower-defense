using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Bastion.Core;
using Bastion.Grid;

namespace Bastion.UI.Menu
{
    /// <summary>
    /// Menu principal : sélection de niveau (cartes générées depuis le LevelCatalog), sélecteur de
    /// difficulté, gemmes / étoiles, boutique d'améliorations. Ajouter un niveau au catalogue suffit.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private string gameSceneName = "Main";
        [SerializeField] private LevelCard cardPrefab;
        [SerializeField] private Transform cardsRoot;
        [SerializeField] private TMP_Text gemsText;
        [SerializeField] private TMP_Text starsText;
        [SerializeField] private Button upgradesButton;
        [SerializeField] private UpgradeShop shop;
        [SerializeField] private Button[] difficultyButtons;     // index = Difficulty
        [SerializeField] private Button[] modeButtons;           // index = GameMode
        [SerializeField] private Button resetButton;

        private readonly List<LevelCard> cards = new();
        private LevelCatalog catalog;

        private void Start()
        {
            Time.timeScale = 1f;
            catalog = LevelCatalog.Load();
            if (catalog == null) { Debug.LogError("LevelCatalog introuvable dans Resources/. Lancez Bastion → 1. Générer le projet."); return; }
            upgradesButton.onClick.AddListener(() => shop.Open());
            shop.OnChanged = RefreshHeader;
            for (int i = 0; i < difficultyButtons.Length; i++)
            {
                var d = (Difficulty)i;
                difficultyButtons[i].onClick.AddListener(() => { LevelSession.Difficulty = d; Rebuild(); });
            }
            for (int i = 0; i < modeButtons.Length; i++)
            {
                var m = (GameMode)i;
                modeButtons[i].onClick.AddListener(() => { LevelSession.Mode = m; Rebuild(); });
            }
            if (resetButton != null) resetButton.onClick.AddListener(() => { SaveSystem.ResetProfile(); Rebuild(); });
            Rebuild();
        }

        private void Rebuild()
        {
            foreach (var c in cards) Destroy(c.gameObject);
            cards.Clear();
            var d = LevelSession.Difficulty; var mode = LevelSession.Mode;
            for (int i = 0; i < catalog.levels.Length; i++)
            {
                var l = catalog.levels[i];
                if (l == null) continue;
                var card = Instantiate(cardPrefab, cardsRoot);
                card.Bind(l, i, d, mode, catalog, Play);
                cards.Add(card);
            }
            Highlight(difficultyButtons, (int)d);
            Highlight(modeButtons, (int)mode);
            RefreshHeader();
        }

        private static void Highlight(Button[] buttons, int active)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                var colors = buttons[i].colors;
                colors.normalColor = i == active ? new Color(0.24f, 0.86f, 0.52f) : new Color(0.15f, 0.15f, 0.25f);
                buttons[i].colors = colors;
            }
        }

        private void RefreshHeader()
        {
            gemsText.text = $"{SaveSystem.Gems} ◆";
            starsText.text = $"{SaveSystem.TotalStars()} ★";
        }

        private void Play(LevelData l)
        {
            LevelSession.Select(l);
            SceneManager.LoadScene(gameSceneName);
        }
    }
}
