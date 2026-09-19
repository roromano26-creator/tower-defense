using UnityEngine;

namespace Bastion.UI
{
    /// <summary>Contraint un RectTransform à la zone sûre (encoche, coins arrondis, barre de gestes).</summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        private Rect last;
        private void Awake() => Apply();
        private void Update() { if (Screen.safeArea != last) Apply(); }

        private void Apply()
        {
            last = Screen.safeArea;
            var rt = (RectTransform)transform;
            var min = last.position; var max = last.position + last.size;
            min.x /= Screen.width; min.y /= Screen.height; max.x /= Screen.width; max.y /= Screen.height;
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
    }
}
