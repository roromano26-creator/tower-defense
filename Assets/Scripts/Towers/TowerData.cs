using UnityEngine;
using Bastion.Core;

namespace Bastion.Towers
{
    public enum AttackKind { Direct, AreaOfEffect, Slow, Burn, Sniper }
    public enum TargetPriority { First, Closest, Strongest, Weakest }

    /// <summary>Statistiques d'un niveau de tour. Le visuel est un prefab enfant, jamais du code.</summary>
    [System.Serializable]
    public class TowerLevel
    {
        public int cost = 60;
        public float range = 5f;
        public float damage = 10f;
        [Tooltip("Tirs par seconde.")] public float fireRate = 1f;
        [Tooltip("Rayon de zone (AreaOfEffect / Burn).")] public float splashRadius = 0f;
        [Tooltip("Facteur de vitesse appliqué (Slow) : 0.5 = moitié moins vite.")] public float slowFactor = 0.5f;
        [Tooltip("Durée du statut (Slow / Burn) en secondes.")] public float effectDuration = 2f;
        [Tooltip("Dégâts par seconde du statut Burn.")] public float dotDamagePerSecond = 0f;
        [Tooltip("Prefab du visuel de ce niveau — glissez ici le modèle importé (voir ASSETS_IMPORT.md).")]
        public GameObject visualPrefab;
    }

    [CreateAssetMenu(menuName = "Bastion/Tour", fileName = "Tower_")]
    public class TowerData : ScriptableObject
    {
        public string towerId = "crossbow";
        public string displayName = "Arbalète";
        [TextArea] public string description = "Dégâts directs, cadence élevée.";
        public Sprite icon;
        public Color accentColor = new(0.96f, 0.66f, 0.2f);

        public AttackKind attackKind = AttackKind.Direct;
        public DamageType damageType = DamageType.Physical;
        public TargetPriority defaultPriority = TargetPriority.First;
        [Tooltip("Peut viser les ennemis volants.")] public bool canTargetFlying = true;
        [Tooltip("Peut viser les ennemis au sol.")] public bool canTargetGround = true;

        [Header("Projectile")]
        public Projectile projectilePrefab;
        public float projectileSpeed = 18f;
        [Tooltip("Tir instantané (hitscan) : pas de projectile, un trait de lumière.")] public bool hitscan = false;

        [Header("Niveaux (1 → 3)")]
        public TowerLevel[] levels = new TowerLevel[3];

        [Header("Vente")]
        [Range(0f, 1f)] public float sellRefund = 0.6f;

        public int TotalInvested(int levelIndex)
        {
            int sum = 0;
            for (int i = 0; i <= levelIndex && i < levels.Length; i++) sum += levels[i].cost;
            return sum;
        }
    }
}
