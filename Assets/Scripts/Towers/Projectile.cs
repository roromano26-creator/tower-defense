using UnityEngine;
using Bastion.Enemies;

namespace Bastion.Towers
{
    /// <summary>
    /// Projectile poolé. Deux trajectoires : droite (flèche, éclair) ou arc balistique (boulet).
    /// Si la cible meurt en vol, le projectile continue jusqu'au dernier point connu et frappe la
    /// zone quand même (un boulet de canon ne disparaît pas parce que sa cible est morte).
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private bool ballistic = false;
        [SerializeField] private float arcHeight = 2.5f;
        [SerializeField] private TrailRenderer trail;
        [SerializeField] private Renderer tintTarget;

        private Enemy target;
        private Vector3 lastTargetPos, start;
        private float speed, progress, flightTime;
        private System.Action<Enemy, Vector3> onHit;
        private MaterialPropertyBlock mpb;
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        public void Launch(Enemy target, float speed, Color tint, System.Action<Enemy, Vector3> onHit)
        {
            this.target = target; this.speed = speed; this.onHit = onHit;
            start = transform.position;
            lastTargetPos = target.HitPosition;
            progress = 0f;
            flightTime = Vector3.Distance(start, lastTargetPos) / Mathf.Max(0.1f, speed);
            if (trail != null) { trail.Clear(); trail.startColor = tint; trail.endColor = new Color(tint.r, tint.g, tint.b, 0f); }
            if (tintTarget != null)
            {
                mpb ??= new MaterialPropertyBlock();
                tintTarget.GetPropertyBlock(mpb);
                mpb.SetColor(EmissionId, tint * 2.5f);
                tintTarget.SetPropertyBlock(mpb);
            }
        }

        private void Update()
        {
            if (target != null && target.IsAlive) lastTargetPos = target.HitPosition;
            if (ballistic)
            {
                progress += Time.deltaTime / Mathf.Max(0.05f, flightTime);
                var p = Vector3.Lerp(start, lastTargetPos, progress);
                p.y += Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI) * arcHeight;
                var dir = p - transform.position;
                transform.position = p;
                if (dir.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(dir);
                if (progress >= 1f) Hit();
            }
            else
            {
                var to = lastTargetPos - transform.position;
                float step = speed * Time.deltaTime;
                if (to.magnitude <= step) { transform.position = lastTargetPos; Hit(); return; }
                transform.position += to.normalized * step;
                transform.rotation = Quaternion.LookRotation(to);
            }
        }

        private void Hit()
        {
            var cb = onHit; onHit = null;
            var t = target != null && target.IsAlive ? target : null;
            TowerManager.Instance.ReleaseProjectile(this);
            cb?.Invoke(t, lastTargetPos);
        }
    }
}
