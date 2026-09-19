using UnityEngine;
using Bastion.Grid;

namespace Bastion.UI.Menu
{
    /// <summary>Miniature d'un niveau dessinée depuis ses données : le chemin, les rochers, les couleurs du biome. Aucune image à produire par niveau.</summary>
    public static class LevelPreview
    {
        public static Texture2D Render(LevelData l, int scale = 6)
        {
            int w = l.width * scale, h = l.height * scale;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var cells = new CellType[l.width, l.height];
            foreach (var b in l.blocked) if (In(l, b)) cells[b.x, b.y] = CellType.Blocked;
            foreach (var p in l.path) if (In(l, p)) cells[p.x, p.y] = CellType.Path;
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var c = cells[x / scale, y / scale];
                    var col = c == CellType.Path ? l.pathColor : c == CellType.Blocked ? l.blockedColor : l.buildableColor;
                    bool edge = x % scale == 0 || y % scale == 0;
                    pixels[y * w + x] = edge ? col * 0.8f : col;
                }
            t.SetPixels(pixels); t.Apply();
            return t;
        }

        private static bool In(LevelData l, Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < l.width && c.y < l.height;
    }
}
