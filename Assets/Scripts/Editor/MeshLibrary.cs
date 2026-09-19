using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Bastion.EditorTools
{
    /// <summary>Meshes procéduraux que les primitives Unity n'offrent pas : cône, cristal, anneau plat.</summary>
    public static class MeshLibrary
    {
        public static Mesh Cone(int segments = 16) => BastionPaths.GetOrCreate($"{BastionPaths.Meshes}/Cone.asset", () => BuildCone(segments));
        public static Mesh Crystal() => BastionPaths.GetOrCreate($"{BastionPaths.Meshes}/Crystal.asset", BuildCrystal);
        public static Mesh Ring(float inner = 0.8f) => BastionPaths.GetOrCreate($"{BastionPaths.Meshes}/Ring.asset", () => BuildRing(inner, 32));

        private static Mesh BuildCone(int seg)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
            var apex = new Vector3(0, 1, 0);
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2 / seg, a1 = (i + 1) * Mathf.PI * 2 / seg;
                var p0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0, Mathf.Sin(a0) * 0.5f);
                var p1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0, Mathf.Sin(a1) * 0.5f);
                var nn = Vector3.Cross(p1 - apex, p0 - apex).normalized;
                int b = v.Count; v.Add(apex); v.Add(p1); v.Add(p0); n.Add(nn); n.Add(nn); n.Add(nn); t.Add(b); t.Add(b + 1); t.Add(b + 2);
                b = v.Count; v.Add(Vector3.zero); v.Add(p0); v.Add(p1); n.Add(Vector3.down); n.Add(Vector3.down); n.Add(Vector3.down); t.Add(b); t.Add(b + 1); t.Add(b + 2);
            }
            var m = new Mesh { name = "Cone" }; m.SetVertices(v); m.SetNormals(n); m.SetTriangles(t, 0); m.RecalculateBounds(); return m;
        }

        /// <summary>Bipyramide hexagonale allongée : cristal de givre / arcane.</summary>
        private static Mesh BuildCrystal()
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
            const int seg = 6; var top = new Vector3(0, 1, 0); var bottom = new Vector3(0, -0.35f, 0);
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2 / seg, a1 = (i + 1) * Mathf.PI * 2 / seg;
                var p0 = new Vector3(Mathf.Cos(a0) * 0.3f, 0.15f, Mathf.Sin(a0) * 0.3f);
                var p1 = new Vector3(Mathf.Cos(a1) * 0.3f, 0.15f, Mathf.Sin(a1) * 0.3f);
                Tri(v, n, t, top, p1, p0); Tri(v, n, t, bottom, p0, p1);
            }
            var m = new Mesh { name = "Crystal" }; m.SetVertices(v); m.SetNormals(n); m.SetTriangles(t, 0); m.RecalculateBounds(); return m;
        }

        private static Mesh BuildRing(float inner, int seg)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>(); var uv = new List<Vector2>();
            for (int i = 0; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2 / seg; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                v.Add(d * inner * 0.5f); v.Add(d * 0.5f); n.Add(Vector3.up); n.Add(Vector3.up);
                uv.Add(new Vector2(0, (float)i / seg)); uv.Add(new Vector2(1, (float)i / seg));
                if (i < seg) { int b = i * 2; t.AddRange(new[] { b, b + 2, b + 1, b + 1, b + 2, b + 3 }); }
            }
            var m = new Mesh { name = "Ring" }; m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateBounds(); return m;
        }

        private static void Tri(List<Vector3> v, List<Vector3> n, List<int> t, Vector3 a, Vector3 b, Vector3 c)
        {
            var nn = Vector3.Cross(b - a, c - a).normalized;
            int i = v.Count; v.Add(a); v.Add(b); v.Add(c); n.Add(nn); n.Add(nn); n.Add(nn); t.Add(i); t.Add(i + 1); t.Add(i + 2);
        }
    }
}
