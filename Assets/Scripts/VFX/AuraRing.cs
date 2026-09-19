using UnityEngine;

namespace Bastion.VFX
{
    /// <summary>
    /// Anneau émissif au pied d'une tour (lave, givre, arcane) : pulse doucement via MaterialPropertyBlock.
    /// Décoratif ; l'anneau de portée réel est RangeIndicator.
    /// </summary>
    public class AuraRing : MonoBehaviour
    {
        [SerializeField] private Renderer target;
        [SerializeField] private Color color = Color.cyan;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField, Range(0f, 4f)] private float intensity = 1.5f;

        private MaterialPropertyBlock mpb;
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private float phase;

        private void Awake() { mpb = new MaterialPropertyBlock(); phase = Random.value * 6f; }

        public void SetColor(Color c) { color = c; }

        private void Update()
        {
            if (target == null) return;
            float k = intensity * (0.75f + 0.25f * Mathf.Sin(Time.time * pulseSpeed + phase));
            target.GetPropertyBlock(mpb);
            mpb.SetColor(EmissionId, color * k);
            target.SetPropertyBlock(mpb);
            transform.Rotate(0f, 12f * Time.deltaTime, 0f, Space.Self);
        }
    }
}
