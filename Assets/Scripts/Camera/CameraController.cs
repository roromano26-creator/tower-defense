using UnityEngine;
using Bastion.Core;
using Bastion.Grid;

namespace Bastion.CameraRig
{
    /// <summary>
    /// Caméra isométrique tactile : un doigt = panoramique, deux doigts = pinch-to-zoom, bornée à la
    /// grille. Fonctionne aussi à la souris (clic droit / molette) pour l'éditeur. Secousse légère sur
    /// événement (perte de vie, mort de boss). Le rig est un pivot au sol ; la caméra est son enfant.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Camera cam;
        [SerializeField] private float panSpeed = 1f;
        [SerializeField] private float zoomMin = 9f;
        [SerializeField] private float zoomMax = 26f;
        [SerializeField] private float zoomSpeed = 0.03f;
        [SerializeField] private float smoothing = 12f;
        [SerializeField] private Vector3 cameraOffsetDir = new(0f, 1.15f, -0.9f);   // direction vue 3/4, ~52°

        private Vector3 targetPivot;
        private float targetZoom;
        private Vector3 lastPointer;
        private bool dragging;
        private float shake;
        private Vector3 shakeOffset;
        private float zoomMaxEffectif;
        private int largeurEcran, hauteurEcran;

        public bool IsDragging => dragging;

        private void Start()
        {
            targetPivot = GridManager.Instance.WorldBounds.center;
            Recadrer(true);
            transform.position = targetPivot;
        }

        private void OnEnable() { GameEvents.OnCameraShakeRequested += Shake; }
        private void OnDisable() { GameEvents.OnCameraShakeRequested -= Shake; }

        private void Shake(Vector3 _, float strength) => shake = Mathf.Max(shake, strength);

        /// <summary>
        /// Calcule la distance nécessaire pour que tout le plateau tienne à l'écran, format
        /// compris. L'ancien calcul ne regardait que la taille du plateau : correct en
        /// paysage, il ne montrait que trois colonnes sur quatorze en portrait, le champ
        /// horizontal y étant deux fois plus étroit. Le plafond de zoom, fixé à 26, empêchait
        /// en plus de reculer assez pour compenser, d'où un plateau impossible à embrasser.
        /// </summary>
        private void Recadrer(bool immediat)
        {
            largeurEcran = Screen.width; hauteurEcran = Screen.height;
            var b = GridManager.Instance.WorldBounds;

            // Le garde-fou évite une distance qui explose sur un format extrême.
            float format = Mathf.Max(0.35f, (float)Screen.width / Mathf.Max(1, Screen.height));
            float pourLargeur = b.size.x / format;
            float voulu = Mathf.Max(pourLargeur, b.size.z) * 0.85f;

            // Le plafond s'adapte au besoin réel : sinon il interdirait le seul cadrage
            // qui rende le jeu jouable en portrait.
            zoomMaxEffectif = Mathf.Max(zoomMax, voulu);
            targetZoom = Mathf.Clamp(voulu, zoomMin, zoomMaxEffectif);
            if (immediat) ApplyZoom(targetZoom, true);
        }

        private void Update()
        {
            // La rotation d'un téléphone, ou le redimensionnement d'une fenêtre, change le
            // format sans prévenir : sur le web rien n'impose l'orientation, contrairement
            // à l'application Android.
            if (Screen.width != largeurEcran || Screen.height != hauteurEcran) Recadrer(false);

            HandleTouch();
            HandleMouse();
            ClampToBounds();
            float dt = Time.unscaledDeltaTime;   // la caméra répond même en pause
            transform.position = Vector3.Lerp(transform.position, targetPivot, smoothing * dt);
            ApplyZoom(Mathf.Lerp(CurrentZoom(), targetZoom, smoothing * dt), false);
            if (shake > 0.001f)
            {
                shakeOffset = Random.insideUnitSphere * shake * 0.35f;
                shake = Mathf.Lerp(shake, 0f, 8f * dt);
                cam.transform.localPosition += shakeOffset;
            }
        }

        private void HandleTouch()
        {
            if (Input.touchCount == 1)
            {
                var t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Began) { dragging = false; lastPointer = t.position; }
                else if (t.phase == TouchPhase.Moved)
                {
                    var delta = (Vector3)t.position - lastPointer;
                    if (delta.magnitude > 8f) dragging = true;     // seuil : un tap reste un tap
                    if (dragging) Pan(delta);
                    lastPointer = t.position;
                }
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) Invoke(nameof(ClearDrag), 0.05f);
            }
            else if (Input.touchCount == 2)
            {
                dragging = true;
                var a = Input.GetTouch(0); var b = Input.GetTouch(1);
                float prev = ((a.position - a.deltaPosition) - (b.position - b.deltaPosition)).magnitude;
                float now = (a.position - b.position).magnitude;
                targetZoom = Mathf.Clamp(targetZoom - (now - prev) * zoomSpeed, zoomMin, zoomMaxEffectif);
            }
        }

        private void HandleMouse()
        {
            if (Input.touchCount > 0) return;
            if (Input.GetMouseButtonDown(1)) lastPointer = Input.mousePosition;
            if (Input.GetMouseButton(1)) { Pan(Input.mousePosition - lastPointer); lastPointer = Input.mousePosition; dragging = true; }
            if (Input.GetMouseButtonUp(1)) Invoke(nameof(ClearDrag), 0.05f);
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f) targetZoom = Mathf.Clamp(targetZoom - scroll * 1.5f, zoomMin, zoomMaxEffectif);
        }

        private void ClearDrag() => dragging = false;

        private void Pan(Vector3 screenDelta)
        {
            // Déplacement proportionnel à la hauteur visible pour rester constant quel que soit le zoom.
            float worldPerPixel = targetZoom * 1.2f / Screen.height;
            var right = Vector3.right; var fwd = Vector3.forward;
            targetPivot -= (right * screenDelta.x + fwd * screenDelta.y) * worldPerPixel * panSpeed;
        }

        private void ClampToBounds()
        {
            var b = GridManager.Instance.WorldBounds;
            targetPivot.x = Mathf.Clamp(targetPivot.x, b.min.x, b.max.x);
            targetPivot.z = Mathf.Clamp(targetPivot.z, b.min.z - 2f, b.max.z);
            targetPivot.y = 0f;
        }

        private float CurrentZoom() => cam.transform.localPosition.magnitude;

        private void ApplyZoom(float distance, bool snap)
        {
            cam.transform.localPosition = cameraOffsetDir.normalized * distance;
            cam.transform.LookAt(transform.position + Vector3.up * 0.5f);
            if (snap) targetZoom = distance;
        }

        /// <summary>Recentre la caméra sur un point (ex. spawn du boss).</summary>
        public void FocusOn(Vector3 world) { targetPivot = world; }
    }
}
