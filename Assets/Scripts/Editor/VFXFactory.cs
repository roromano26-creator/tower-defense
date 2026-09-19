using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Bastion.VFX;

namespace Bastion.EditorTools
{
    /// <summary>
    /// Particules placeholders par VFXKind. Toutes additives sur une texture disque douce : avec le
    /// bloom d'URP elles lisent comme des effets finis. Remplaçables par n'importe quel ParticleSystem.
    /// </summary>
    public static class VFXFactory
    {
        public static VFXEntry[] All()
        {
            return new[]
            {
                Entry(VFXKind.MuzzleFlash, Burst("MuzzleFlash", 6, 0.15f, 0.35f, 2.5f, "#FFE39A", 0.25f)),
                Entry(VFXKind.Impact, Burst("Impact", 12, 0.3f, 0.2f, 4f, "#FFFFFF", 0.2f, gravity: 1.5f)),
                Entry(VFXKind.ImpactArea, Ring("ImpactArea", 28, 0.45f, 0.35f, 5f, "#FFC070", 0.35f)),
                Entry(VFXKind.Death, Burst("Death", 18, 0.6f, 0.3f, 3.5f, "#FFFFFF", 0.35f, gravity: 2f)),
                Entry(VFXKind.BossDeath, Ring("BossDeath", 80, 1.2f, 0.6f, 7f, "#FF6A3A", 0.6f), 2),
                Entry(VFXKind.Build, Rise("Build", 24, 0.8f, 0.2f, "#3DDC84")),
                Entry(VFXKind.Upgrade, Rise("Upgrade", 36, 1.0f, 0.25f, "#FFD65A")),
                Entry(VFXKind.Sell, Rise("Sell", 16, 0.6f, 0.2f, "#FFF3A0")),
                Entry(VFXKind.FrostStatus, Loop("FrostStatus", 8, 0.7f, 0.12f, "#9FE0FF"), 12),
                Entry(VFXKind.BurnStatus, Loop("BurnStatus", 14, 0.5f, 0.16f, "#FF8A2A"), 12),
            };
        }

        private static VFXEntry Entry(VFXKind kind, ParticleSystem ps, int prewarm = 8) => new() { kind = kind, prefab = ps, prewarm = prewarm };

        private static ParticleSystem Base(string name, bool loop, float duration)
        {
            var go = new GameObject($"VFX_{name}");
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = loop; main.duration = duration; main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.None;
            main.maxParticles = 128;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = MaterialLibrary.ParticleAdd;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 0.6f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var sol = ps.sizeOverLifetime; sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 1, 1, 0));
            return ps;
        }

        private static ParticleSystem Save(ParticleSystem ps)
        {
            string path = $"{BastionPaths.PrefVFX}/{ps.gameObject.name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) { Object.DestroyImmediate(ps.gameObject); return existing.GetComponent<ParticleSystem>(); }
            return BastionPaths.SavePrefab(ps.gameObject, path).GetComponent<ParticleSystem>();
        }

        /// <summary>Éclat sphérique one-shot.</summary>
        private static ParticleSystem Burst(string name, int count, float life, float size, float speed, string hex, float radius, float gravity = 0f)
        {
            var ps = Base(name, false, 0.3f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startColor = MaterialLibrary.Hex(hex);
            main.gravityModifier = gravity;
            var em = ps.emission; em.rateOverTime = 0; em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = radius;
            return Save(ps);
        }

        /// <summary>Onde de choc horizontale (zone d'effet, mort de boss).</summary>
        private static ParticleSystem Ring(string name, int count, float life, float size, float speed, string hex, float radius)
        {
            var ps = Base(name, false, 0.3f);
            var main = ps.main;
            main.startLifetime = life; main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startSpeed = speed; main.startColor = MaterialLibrary.Hex(hex);
            var em = ps.emission; em.rateOverTime = 0; em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = radius; sh.arc = 360f;
            sh.rotation = new Vector3(90, 0, 0);
            var lim = ps.limitVelocityOverLifetime; lim.enabled = true; lim.dampen = 0.35f;
            return Save(ps);
        }

        /// <summary>Étincelles montantes (construction, upgrade, vente).</summary>
        private static ParticleSystem Rise(string name, int count, float life, float size, string hex)
        {
            var ps = Base(name, false, 0.5f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            main.startColor = MaterialLibrary.Hex(hex);
            var em = ps.emission; em.rateOverTime = 0; em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 20f; sh.radius = 0.6f; sh.rotation = new Vector3(-90, 0, 0);
            return Save(ps);
        }

        /// <summary>Boucle de statut attachée à un ennemi (givre, brûlure).</summary>
        private static ParticleSystem Loop(string name, float rate, float life, float size, string hex)
        {
            var ps = Base(name, true, 1f);
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = life; main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.2f); main.startColor = MaterialLibrary.Hex(hex);
            var em = ps.emission; em.rateOverTime = rate;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.35f;
            return Save(ps);
        }

        /// <summary>Trait de tir instantané (tour arcane).</summary>
        public static LineRenderer Beam()
        {
            string path = $"{BastionPaths.PrefVFX}/VFX_Beam.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<LineRenderer>();
            var go = new GameObject("VFX_Beam");
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = MaterialLibrary.AuraAdd;
            lr.widthMultiplier = 0.12f; lr.positionCount = 2; lr.useWorldSpace = true;
            lr.shadowCastingMode = ShadowCastingMode.Off; lr.receiveShadows = false;
            lr.numCapVertices = 4;
            return BastionPaths.SavePrefab(go, path).GetComponent<LineRenderer>();
        }
    }
}
