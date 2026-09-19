using System.Collections.Generic;
using UnityEngine;
using Bastion.Core;

namespace Bastion.Grid
{
    /// <summary>
    /// Grille du niveau : sait ce qu'il y a dans chaque cellule, convertit monde ↔ cellule, et
    /// expose les waypoints du chemin. Construit aussi le sol placeholder (une dalle par cellule)
    /// pour que la lisibilité du chemin ne dépende pas d'un asset importé.
    /// </summary>
    [DefaultExecutionOrder(-180)]
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        [SerializeField] private LevelData level;
        [SerializeField] private Transform groundRoot;
        [SerializeField] private Material buildableMaterial;
        [SerializeField] private Material pathMaterial;
        [SerializeField] private Material blockedMaterial;
        [SerializeField] private GameObject buildableTilePrefab;   // remplaçable par une dalle d'asset
        [SerializeField] private GameObject pathTilePrefab;
        [SerializeField] private GameObject blockedTilePrefab;

        private CellType[,] cells;
        private Towers.Tower[,] occupants;
        private readonly List<Vector3> waypoints = new();

        public LevelData Level => level;
        public float CellSize => level.cellSize;
        public IReadOnlyList<Vector3> Waypoints => waypoints;
        public Vector3 SpawnPoint => waypoints[0];
        public Vector3 ExitPoint => waypoints[waypoints.Count - 1];
        public Bounds WorldBounds { get; private set; }

        private void Awake()
        {
            Instance = this;
            Build();
        }

        private Material tintedBuildable, tintedPath, tintedBlocked;

        /// <summary>Un matériau par type de dalle, teinté par le biome du niveau : trois instances, pas une par dalle (SRP Batcher).</summary>
        private void PrepareMaterials()
        {
            tintedBuildable = level.buildableMaterial != null ? level.buildableMaterial : Tint(buildableMaterial, level.buildableColor);
            tintedPath = level.pathMaterial != null ? level.pathMaterial : Tint(pathMaterial, level.pathColor);
            tintedBlocked = level.blockedMaterial != null ? level.blockedMaterial : Tint(blockedMaterial, level.blockedColor);
        }

        private static Material Tint(Material source, Color c)
        {
            if (source == null) return null;
            var m = new Material(source); m.SetColor("_BaseColor", c); return m;
        }

        public void SetLevel(LevelData l) { level = l; }

        public void Build()
        {
            if (Core.GameManager.Instance != null) level = Core.GameManager.Instance.Level;
            PrepareMaterials();
            cells = new CellType[level.width, level.height];
            occupants = new Towers.Tower[level.width, level.height];
            foreach (var b in level.blocked) if (InBounds(b)) cells[b.x, b.y] = CellType.Blocked;
            waypoints.Clear();
            foreach (var p in level.path)
            {
                if (!InBounds(p)) continue;
                cells[p.x, p.y] = CellType.Path;
                waypoints.Add(CellToWorld(p));
            }
            // Le spawn et la sortie sont poussés d'une cellule hors grille pour que les ennemis
            // apparaissent et disparaissent hors champ plutôt que de « pop » sur une dalle.
            if (waypoints.Count >= 2)
            {
                waypoints.Insert(0, waypoints[0] + (waypoints[0] - waypoints[1]).normalized * level.cellSize * 1.5f);
                int n = waypoints.Count;
                waypoints.Add(waypoints[n - 1] + (waypoints[n - 1] - waypoints[n - 2]).normalized * level.cellSize * 1.5f);
            }
            WorldBounds = new Bounds(new Vector3(level.width * level.cellSize / 2f, 0, level.height * level.cellSize / 2f),
                                     new Vector3(level.width * level.cellSize, 1, level.height * level.cellSize));
            if (groundRoot != null) BuildGround();
        }

        private void BuildGround()
        {
            for (int i = groundRoot.childCount - 1; i >= 0; i--)
                if (Application.isPlaying) Destroy(groundRoot.GetChild(i).gameObject); else DestroyImmediate(groundRoot.GetChild(i).gameObject);
            for (int x = 0; x < level.width; x++)
                for (int y = 0; y < level.height; y++)
                {
                    var type = cells[x, y];
                    var prefab = type == CellType.Path ? pathTilePrefab : type == CellType.Blocked ? blockedTilePrefab : buildableTilePrefab;
                    GameObject tile;
                    if (prefab != null) tile = Instantiate(prefab, groundRoot);
                    else
                    {
                        tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        tile.transform.SetParent(groundRoot, false);
                        tile.transform.localScale = new Vector3(level.cellSize * 0.96f, type == CellType.Path ? 0.18f : 0.3f, level.cellSize * 0.96f);
                        var r = tile.GetComponent<Renderer>();
                        r.sharedMaterial = type == CellType.Path ? tintedPath : type == CellType.Blocked ? tintedBlocked : tintedBuildable;
                        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                    tile.name = $"Tile_{x}_{y}_{type}";
                    tile.transform.position = CellToWorld(new Vector2Int(x, y)) + Vector3.down * (type == CellType.Path ? 0.06f : 0f);
                    tile.isStatic = true;
                }
        }

        public bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < level.width && c.y < level.height;

        public Vector3 CellToWorld(Vector2Int c) => new((c.x + 0.5f) * level.cellSize, 0f, (c.y + 0.5f) * level.cellSize);

        public Vector2Int WorldToCell(Vector3 w) => new(Mathf.FloorToInt(w.x / level.cellSize), Mathf.FloorToInt(w.z / level.cellSize));

        public CellType GetType(Vector2Int c) => InBounds(c) ? cells[c.x, c.y] : CellType.Blocked;

        public bool IsBuildable(Vector2Int c) => InBounds(c) && cells[c.x, c.y] == CellType.Buildable && occupants[c.x, c.y] == null;

        public Towers.Tower GetTower(Vector2Int c) => InBounds(c) ? occupants[c.x, c.y] : null;

        public void SetOccupant(Vector2Int c, Towers.Tower t) { if (InBounds(c)) occupants[c.x, c.y] = t; }

        /// <summary>Longueur totale du chemin — utile au tri « premier ennemi » du ciblage.</summary>
        public float PathLength()
        {
            float d = 0;
            for (int i = 1; i < waypoints.Count; i++) d += Vector3.Distance(waypoints[i - 1], waypoints[i]);
            return d;
        }
    }
}
