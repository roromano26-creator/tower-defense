using UnityEngine;

namespace Bastion.Grid
{
    /// <summary>
    /// Déplace un transform le long des waypoints de la grille. Séparé de Enemy pour qu'un
    /// pathfinding dynamique (A*) puisse un jour remplacer la liste fixe sans toucher à l'ennemi.
    /// </summary>
    public class PathFollower : MonoBehaviour
    {
        [SerializeField] private float turnSpeed = 10f;

        private int nextIndex;
        private float hoverHeight;

        /// <summary>Distance parcourue depuis le départ (sert au ciblage « premier sur le chemin »).</summary>
        public float DistanceTravelled { get; private set; }
        public bool ReachedEnd { get; private set; }

        public void Begin(float hover)
        {
            hoverHeight = hover;
            nextIndex = 1;
            DistanceTravelled = 0;
            ReachedEnd = false;
            var start = GridManager.Instance.Waypoints[0];
            transform.position = start + Vector3.up * hoverHeight;
        }

        public void Step(float speed, float dt)
        {
            if (ReachedEnd) return;
            var wps = GridManager.Instance.Waypoints;
            var target = wps[nextIndex] + Vector3.up * hoverHeight;
            var pos = transform.position;
            var to = target - pos;
            float dist = to.magnitude;
            float move = speed * dt;
            if (dist <= move)
            {
                transform.position = target;
                DistanceTravelled += dist;
                nextIndex++;
                if (nextIndex >= wps.Count) { ReachedEnd = true; return; }
            }
            else
            {
                transform.position = pos + to / dist * move;
                DistanceTravelled += move;
            }
            if (to.sqrMagnitude > 0.0001f)
            {
                var look = Quaternion.LookRotation(new Vector3(to.x, 0, to.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * dt);
            }
        }
    }
}
