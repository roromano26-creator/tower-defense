using UnityEngine;
using UnityEngine.EventSystems;
using Bastion.Core;
using Bastion.Grid;

namespace Bastion.Towers
{
    /// <summary>
    /// Entrée tactile du placement. Tap sur une cellule libre → ouvre le menu de construction sur
    /// cette cellule (fantôme de prévisualisation + disque de portée). Tap sur une tour → panneau
    /// d'upgrade. Tap ailleurs → tout fermer. Un drag caméra n'est jamais compté comme un tap.
    /// </summary>
    public class TowerPlacer : MonoBehaviour
    {
        [SerializeField] private Camera cam;
        [SerializeField] private CameraRig.CameraController cameraController;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private GameObject cellHighlight;      // cadre lumineux sur la cellule visée
        [SerializeField] private RangeIndicator previewRange;
        [SerializeField] private Transform ghostRoot;           // fantôme semi-transparent de la tour
        [SerializeField] private Material ghostMaterial;

        public Vector2Int? SelectedCell { get; private set; }
        public Tower SelectedTower { get; private set; }

        private Vector3 pressPos;
        private bool pressed;
        private bool pressOverUI;   // décidé à l'appui : au relâchement, IsPointerOverGameObject(fingerId) répond faux sur Android

        private void Start() { ClearSelection(); }

        private void Update()
        {
            var state = GameManager.Instance.State;
            if (state != GameState.Building && state != GameState.WaveRunning) return;
            if (GameManager.Instance.IsAssault) return;      // en Assaut, les tours sont à l'IA

            if (Input.GetMouseButtonDown(0)) { pressed = true; pressPos = Input.mousePosition; pressOverUI = IsPointerOverUI(); }
            if (Input.GetMouseButtonUp(0) && pressed)
            {
                pressed = false;
                if (cameraController != null && cameraController.IsDragging) return;
                if ((Input.mousePosition - pressPos).magnitude > 12f) return;
                if (pressOverUI) return;
                HandleTap(Input.mousePosition);
            }
        }

        private static bool IsPointerOverUI()
        {
            if (EventSystem.current == null) return false;
            if (Input.touchCount > 0) return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            return EventSystem.current.IsPointerOverGameObject();
        }

        private void HandleTap(Vector3 screen)
        {
            var ray = cam.ScreenPointToRay(screen);
            // Plan y = 0 : plus robuste qu'un raycast physique sur des dalles fines.
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(ray, out float dist)) { ClearSelection(); return; }
            var world = ray.GetPoint(dist);
            var grid = GridManager.Instance;
            var cell = grid.WorldToCell(world);
            if (!grid.InBounds(cell)) { ClearSelection(); return; }

            var tower = grid.GetTower(cell);
            if (tower != null) { SelectTower(tower); return; }
            if (grid.IsBuildable(cell)) { SelectCell(cell); return; }
            ClearSelection();
        }

        private void SelectCell(Vector2Int cell)
        {
            ClearSelection();
            SelectedCell = cell;
            var pos = GridManager.Instance.CellToWorld(cell);
            cellHighlight.SetActive(true);
            cellHighlight.transform.position = pos + Vector3.up * 0.16f;
            previewRange.transform.position = pos;
            UI.TowerBuildMenu.Instance.Open(cell, pos);
        }

        private void SelectTower(Tower t)
        {
            ClearSelection();
            SelectedTower = t;
            t.SetSelected(true);
            GameEvents.TowerSelected(t);
        }

        public void ClearSelection()
        {
            if (SelectedTower != null) SelectedTower.SetSelected(false);
            SelectedTower = null;
            SelectedCell = null;
            if (cellHighlight != null) cellHighlight.SetActive(false);
            if (previewRange != null) previewRange.Show(false);
            ClearGhost();
            GameEvents.TowerSelected(null);
            if (UI.TowerBuildMenu.Instance != null) UI.TowerBuildMenu.Instance.Close();
        }

        /// <summary>Le menu de construction appelle ceci quand le joueur survole/sélectionne un type.</summary>
        public void PreviewTower(TowerData data)
        {
            if (SelectedCell == null) return;
            ClearGhost();
            previewRange.SetRadius(data.levels[0].range);
            previewRange.Show(true);
            var prefab = data.levels[0].visualPrefab;
            if (prefab == null) return;
            var g = Instantiate(prefab, ghostRoot);
            ghostRoot.position = GridManager.Instance.CellToWorld(SelectedCell.Value);
            foreach (var r in g.GetComponentsInChildren<Renderer>())
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = ghostMaterial;
                r.sharedMaterials = mats;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        public void ConfirmBuild(TowerData data)
        {
            if (SelectedCell == null) return;
            var cell = SelectedCell.Value;
            if (TowerManager.Instance.TryBuild(data, cell)) ClearSelection();
        }

        private void ClearGhost()
        {
            if (ghostRoot == null) return;
            for (int i = ghostRoot.childCount - 1; i >= 0; i--) Destroy(ghostRoot.GetChild(i).gameObject);
        }
    }
}
