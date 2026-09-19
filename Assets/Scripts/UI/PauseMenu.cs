using UnityEngine;
using UnityEngine.UI;
using Bastion.Core;

namespace Bastion.UI
{
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private UIPulse pulse;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button menuButton;

        private void Awake()
        {
            GameEvents.OnStateChanged += s => pulse.Show(s == GameState.Paused);
            resumeButton.onClick.AddListener(() => GameManager.Instance.TogglePause());
            restartButton.onClick.AddListener(() => GameManager.Instance.RestartLevel());
            quitButton.onClick.AddListener(Application.Quit);
            if (menuButton != null) menuButton.onClick.AddListener(() => GameManager.Instance.GoToMenu());
            pulse.Show(false);
        }
    }
}
