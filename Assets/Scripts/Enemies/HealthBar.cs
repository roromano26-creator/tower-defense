using UnityEngine;

namespace Bastion.Enemies
{
    /// <summary>
    /// Barre de vie world-space en deux quads (fond + remplissage) qui fait toujours face à la caméra.
    /// Pas de Canvas : cinquante canvases world-space par vague coûtent cher sur mobile.
    /// </summary>
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private Transform fill;
        [SerializeField] private Renderer fillRenderer;
        [SerializeField] private Gradient colorByHealth;

        private Camera cam;
        private MaterialPropertyBlock mpb;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private float fullWidth;

        private void Awake()
        {
            cam = Camera.main;
            mpb = new MaterialPropertyBlock();
            fullWidth = fill.localScale.x;
        }

        public void Set(float ratio, bool boss)
        {
            gameObject.SetActive(ratio < 1f || boss);
            var s = fill.localScale; s.x = fullWidth * Mathf.Clamp01(ratio); fill.localScale = s;
            fill.localPosition = new Vector3(-(fullWidth - s.x) / 2f, 0, -0.001f);
            if (fillRenderer != null)
            {
                fillRenderer.GetPropertyBlock(mpb);
                mpb.SetColor(BaseColorId, colorByHealth.Evaluate(ratio));
                fillRenderer.SetPropertyBlock(mpb);
            }
        }

        private void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
        }
    }
}
