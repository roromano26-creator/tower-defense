using UnityEngine;

namespace Bastion.UI
{
    /// <summary>Petite animation d'apparition (scale + fade) indépendante de timeScale, pour les panneaux.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIPulse : MonoBehaviour
    {
        [SerializeField] private float duration = 0.18f;
        private CanvasGroup cg;
        private float t;
        private bool showing;

        private void Awake() { cg = GetComponent<CanvasGroup>(); }

        public void Show(bool on)
        {
            showing = on;
            if (on) { gameObject.SetActive(true); t = 0f; }
        }

        private void Update()
        {
            t += Time.unscaledDeltaTime / duration;
            float k = Mathf.Clamp01(t);
            float eased = 1f - Mathf.Pow(1f - k, 3f);
            if (showing)
            {
                cg.alpha = eased;
                transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, eased);
                cg.interactable = cg.blocksRaycasts = true;
            }
            else
            {
                cg.alpha = 1f - eased;
                cg.interactable = cg.blocksRaycasts = false;
                if (k >= 1f) gameObject.SetActive(false);
            }
        }
    }
}
