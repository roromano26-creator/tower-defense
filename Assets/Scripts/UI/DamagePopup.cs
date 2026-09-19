using UnityEngine;
using TMPro;
using Bastion.Core;

namespace Bastion.UI
{
    /// <summary>
    /// Nombres de dégâts flottants (TextMeshPro world-space, poolés, face caméra).
    /// Coupés au-delà de 40 simultanés : au pire moment d'une vague boss c'est du bruit, pas de l'info.
    /// </summary>
    public class DamagePopup : MonoBehaviour
    {
        private static ObjectPool<DamagePopup> pool;
        private static Transform root;
        private static int active;
        private const int MaxActive = 40;

        [SerializeField] private TMP_Text text;
        private float life;
        private Vector3 velocity;
        private Camera cam;

        public static void Register(DamagePopup prefab, Transform parent) { root = parent; pool = new ObjectPool<DamagePopup>(prefab, parent, 16); active = 0; }

        public static void Show(Vector3 at, float amount, DamageType type)
        {
            if (pool == null || active >= MaxActive) return;
            var p = pool.Get(at + Random.insideUnitSphere * 0.25f, Quaternion.identity);
            active++;
            p.Begin(amount, type);
        }

        private void Begin(float amount, DamageType type)
        {
            cam = Camera.main;
            text.text = amount >= 1000f ? $"{amount / 1000f:0.#}k" : Mathf.RoundToInt(amount).ToString();
            text.color = type switch
            {
                DamageType.Fire => new Color(1f, 0.55f, 0.2f),
                DamageType.Frost => new Color(0.55f, 0.85f, 1f),
                DamageType.Magic => new Color(0.85f, 0.6f, 1f),
                _ => Color.white,
            };
            life = 0.7f;
            velocity = new Vector3(Random.Range(-0.4f, 0.4f), 2.2f, 0f);
            transform.localScale = Vector3.one * (amount >= 100f ? 1.4f : 1f);
        }

        private void Update()
        {
            life -= Time.deltaTime;
            transform.position += velocity * Time.deltaTime;
            velocity.y -= 4f * Time.deltaTime;
            if (cam != null) transform.rotation = cam.transform.rotation;
            var c = text.color; c.a = Mathf.Clamp01(life / 0.3f); text.color = c;
            if (life <= 0f) { active--; pool.Release(this); }
        }
    }
}
