using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Bastion.Core;

namespace Bastion.UI
{
    /// <summary>Barre du haut : vies, or, vague, décompte, vitesse, pause, bouton « Lancer ».</summary>
    public class HUD : MonoBehaviour
    {
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text livesText;
        [SerializeField] private Image livesFill;
        [SerializeField] private TMP_Text waveText;
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private Button startWaveButton;
        [SerializeField] private TMP_Text startWaveLabel;
        [SerializeField] private Button speedButton;
        [SerializeField] private TMP_Text speedLabel;
        [SerializeField] private Button pauseButton;
        [SerializeField] private TMP_Text bossBanner;

        private void OnEnable()
        {
            GameEvents.OnGoldChanged += g => { if (!GameManager.Instance.IsAssault) goldText.text = g.ToString(); };   // en Assaut, ce champ affiche la mana
            GameEvents.OnLivesChanged += OnLives;
            GameEvents.OnWaveStarted += OnWaveStarted;
            GameEvents.OnWaveCountdown += s => countdownText.text = s > 0.05f ? $"Prochaine vague dans {Mathf.CeilToInt(s)} s" : "";
            GameEvents.OnStateChanged += OnState;
            GameEvents.OnGameSpeedChanged += s => speedLabel.text = $"×{s:0}";
            startWaveButton.onClick.AddListener(() => GameManager.Instance.StartNextWave());
            speedButton.onClick.AddListener(() => GameManager.Instance.CycleGameSpeed());
            pauseButton.onClick.AddListener(() => GameManager.Instance.TogglePause());
            bossBanner.gameObject.SetActive(false);
            waveText.text = "Vague 0";
            GameEvents.OnManaChanged += (m, cap) => goldText.text = $"{Mathf.FloorToInt(m)} ✦";
            GameEvents.OnAssaultTimer += s => waveText.text = $"{Mathf.FloorToInt(s / 60f)}:{Mathf.FloorToInt(s % 60f):00}";
        }

        private void Start()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsAssault)
            {
                // En Assaut : l'or affiché devient la mana, la vague devient le chrono, pas de bouton de vague.
                startWaveButton.gameObject.SetActive(false);
                countdownText.text = "";
                waveText.text = "";
            }
        }

        private void Update()
        {
            // Bouton « retour » Android (= Échap) : pause. Ici et non dans PauseMenu, qui est désactivé quand il est caché.
            if (Input.GetKeyDown(KeyCode.Escape)) GameManager.Instance.TogglePause();
        }

        private void OnLives(int l, int max)
        {
            livesText.text = $"{l}";
            if (livesFill != null) livesFill.fillAmount = max > 0 ? (float)l / max : 0f;
        }

        private void OnWaveStarted(int i, int total)
        {
            waveText.text = GameManager.Instance.Endless ? $"Vague {i} · ∞" : $"Vague {i} / {total}";
            countdownText.text = "";
            var wd = Waves.WaveManager.Instance.CurrentWaveData;
            if (wd != null && wd.isBossWave) StartCoroutine(ShowBanner($"⚠ BOSS — {wd.title}"));
        }

        private System.Collections.IEnumerator ShowBanner(string text)
        {
            bossBanner.text = text;
            bossBanner.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(2.5f);
            bossBanner.gameObject.SetActive(false);
        }

        private void OnState(GameState s)
        {
            bool building = s == GameState.Building && !GameManager.Instance.IsAssault;
            if (s == GameState.Building && GameManager.Instance.Endless) waveText.text = $"Vague {Waves.WaveManager.Instance.CurrentWave} · ∞";
            startWaveButton.gameObject.SetActive(building);
            if (building)
            {
                var next = Waves.WaveManager.Instance != null ? Waves.WaveManager.Instance.NextWaveData : null;
                startWaveLabel.text = next != null && next.isBossWave ? "Lancer le BOSS" : "Lancer la vague";
            }
        }
    }
}
