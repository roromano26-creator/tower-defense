using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Bastion.Core;

namespace Bastion.UI
{
    /// <summary>Écran de victoire / défaite avec étoiles et relance.</summary>
    public class EndScreen : MonoBehaviour
    {
        [SerializeField] private UIPulse pulse;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text starsText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private TMP_Text gemsText;
        [SerializeField] private Button endlessButton;

        private void Awake()
        {
            GameEvents.OnStateChanged += OnState;
            restartButton.onClick.AddListener(() => GameManager.Instance.RestartLevel());
            if (nextButton != null) nextButton.onClick.AddListener(() => GameManager.Instance.TryLoadNextLevel());
            if (menuButton != null) menuButton.onClick.AddListener(() => GameManager.Instance.GoToMenu());
            if (endlessButton != null) endlessButton.onClick.AddListener(() => GameManager.Instance.ContinueEndless());
            pulse.Show(false);
        }

        private void OnState(GameState s)
        {
            if (s != GameState.Victory && s != GameState.Defeat) { pulse.Show(false); return; }
            var rm = ResourceManager.Instance;
            var gmi = GameManager.Instance;
            if (s == GameState.Victory)
            {
                titleText.text = gmi.IsAssault ? "Le bastion est tombé !" : "Victoire !";
                subtitleText.text = gmi.IsAssault
                    ? $"Vos monstres ont percé avec {Mathf.FloorToInt(Assault.AssaultController.Instance.TimeLeft)} s d'avance."
                    : $"Le bastion tient. {rm.Lives}/{rm.MaxLives} vies restantes.";
                var r = SaveSystem.GetResult(gmi.Level.levelId, gmi.Difficulty, gmi.Mode);
                int stars = r != null ? r.stars : 1;
                starsText.text = new string('★', stars) + new string('☆', 3 - stars);
                int gems = GameManager.Instance.GemsEarned;
                if (gemsText != null) gemsText.text = gems > 0 ? $"+{gems} gemmes" : "";
                var catalog = LevelCatalog.Load();
                var next = catalog != null ? catalog.Next(GameManager.Instance.Level) : null;
                if (nextButton != null) nextButton.gameObject.SetActive(next != null && catalog.IsUnlocked(catalog.IndexOf(next), GameManager.Instance.Difficulty));
                if (endlessButton != null) endlessButton.gameObject.SetActive(gmi.Level.endlessEnabled && !gmi.IsAssault);
            }
            else
            {
                var gm = GameManager.Instance;
                bool endless = gm.Endless;
                titleText.text = gm.IsAssault ? "Le défenseur a tenu" : endless ? "Fin de la série" : "Défaite";
                var best = SaveSystem.GetResult(gm.Level.levelId, gm.Difficulty, gm.Mode);
                subtitleText.text = gm.IsAssault
                    ? $"Il restait {rm.Lives} vies au bastion. Variez les monstres, visez les trous de la défense."
                    : endless
                    ? $"{gm.WavesCleared} vagues tenues{(gm.NewRecord ? " — nouveau record !" : $" · record {best?.bestWave ?? 0}")}"
                    : $"Le bastion est tombé à la vague {Waves.WaveManager.Instance.CurrentWave}.";
                starsText.text = "";
                if (gemsText != null) gemsText.text = gm.GemsEarned > 0 ? $"+{gm.GemsEarned} gemmes" : "";
                if (nextButton != null) nextButton.gameObject.SetActive(false);
                if (endlessButton != null) endlessButton.gameObject.SetActive(false);
            }
            pulse.Show(true);
        }
    }
}
