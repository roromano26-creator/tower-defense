using System.Collections.Generic;
using UnityEngine;
using Bastion.Core;

namespace Bastion.VFX
{
    public enum VFXKind { MuzzleFlash, Impact, ImpactArea, Death, BossDeath, Build, Upgrade, Sell, FrostStatus, BurnStatus, HitscanBeam }

    /// <summary>Association VFXKind → prefab de ParticleSystem. Rempli par le bootstrap, remplaçable par vos propres VFX.</summary>
    [System.Serializable]
    public class VFXEntry { public VFXKind kind; public ParticleSystem prefab; public int prewarm = 8; }

    /// <summary>
    /// Pools de particules. `Play` tire un effet one-shot à une position, `Attach` le fixe à un
    /// transform pendant une durée (statuts). Les particules se rendent au pool via `ParticleReturn`.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [SerializeField] private VFXEntry[] entries;
        [SerializeField] private LineRenderer beamPrefab;

        private readonly Dictionary<VFXKind, ObjectPool<ParticleSystem>> pools = new();
        private readonly Dictionary<Transform, (ParticleSystem ps, VFXKind kind, float until)> attached = new();
        private readonly List<Transform> expired = new();
        private ObjectPool<LineRenderer> beams;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            Instance = this;
            foreach (var e in entries)
            {
                if (e.prefab == null) continue;
                var root = new GameObject($"Pool_{e.kind}").transform;
                root.SetParent(transform, false);
                pools[e.kind] = new ObjectPool<ParticleSystem>(e.prefab, root, e.prewarm);
            }
            if (beamPrefab != null) beams = new ObjectPool<LineRenderer>(beamPrefab, transform, 4);
        }

        public void Play(VFXKind kind, Vector3 position, Color tint = default, float scale = 1f)
        {
            if (!pools.TryGetValue(kind, out var pool)) return;
            var ps = pool.Get(position, Quaternion.identity);
            ps.transform.localScale = Vector3.one * scale;
            if (tint != default)
            {
                var main = ps.main;
                main.startColor = new ParticleSystem.MinMaxGradient(tint, Color.white);
            }
            ps.Play(true);
            var ret = ps.GetComponent<ParticleReturn>() ?? ps.gameObject.AddComponent<ParticleReturn>();
            ret.Arm(() => pool.Release(ps));
        }

        public void Attach(VFXKind kind, Transform target, float duration)
        {
            if (!pools.TryGetValue(kind, out var pool)) return;
            if (attached.TryGetValue(target, out var cur) && cur.kind == kind)
            {
                attached[target] = (cur.ps, kind, Mathf.Max(cur.until, Time.time + duration));
                return;
            }
            var ps = pool.Get(target.position, Quaternion.identity);
            ps.transform.SetParent(target, true);
            ps.transform.localPosition = Vector3.up * 0.6f;
            ps.Play(true);
            attached[target] = (ps, kind, Time.time + duration);
        }

        /// <summary>Trait lumineux instantané (tour sniper).</summary>
        public void Beam(Vector3 from, Vector3 to, Color color)
        {
            if (beams == null) return;
            var lr = beams.Get(from, Quaternion.identity);
            lr.positionCount = 2;
            lr.SetPosition(0, from); lr.SetPosition(1, to);
            lr.startColor = color; lr.endColor = color;
            StartCoroutine(FadeBeam(lr));
        }

        private System.Collections.IEnumerator FadeBeam(LineRenderer lr)
        {
            float t = 0; float w = lr.widthMultiplier;
            while (t < 0.15f) { t += Time.deltaTime; lr.widthMultiplier = Mathf.Lerp(w, 0f, t / 0.15f); yield return null; }
            lr.widthMultiplier = w;
            beams.Release(lr);
        }

        private void Update()
        {
            expired.Clear();
            foreach (var kv in attached)
                if (kv.Key == null || !kv.Key.gameObject.activeInHierarchy || Time.time > kv.Value.until) expired.Add(kv.Key);
            foreach (var t in expired)
            {
                var (ps, kind, _) = attached[t];
                attached.Remove(t);
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                pools[kind].Release(ps);
            }
        }
    }

    /// <summary>Rend un ParticleSystem au pool une fois terminé (StopAction = Callback).</summary>
    public class ParticleReturn : MonoBehaviour
    {
        private System.Action onDone;
        private ParticleSystem ps;
        public void Arm(System.Action cb) { onDone = cb; ps = GetComponent<ParticleSystem>(); }
        private void Update()
        {
            if (ps != null && onDone != null && !ps.IsAlive(true)) { var cb = onDone; onDone = null; cb(); }
        }
    }
}
