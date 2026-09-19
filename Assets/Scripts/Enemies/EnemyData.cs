using UnityEngine;
using Bastion.Core;

namespace Bastion.Enemies
{
    [CreateAssetMenu(menuName = "Bastion/Ennemi", fileName = "Enemy_")]
    public class EnemyData : ScriptableObject
    {
        public string enemyId = "goblin";
        public string displayName = "Gobelin";
        public Color accentColor = Color.green;

        [Header("Stats")]
        public float maxHealth = 40f;
        public float speed = 3f;
        [Tooltip("Armure plate soustraite à chaque coup physique (min 1 dégât).")] public float armor = 0f;
        public int goldReward = 8;
        public int livesCost = 1;
        public bool isFlying = false;
        public bool isBoss = false;
        [Tooltip("Coût de menace dans les vagues générées (mode Sans fin).")] public int threat = 2;
        [Tooltip("Hauteur de vol (0 au sol).")] public float hoverHeight = 0f;
        [Tooltip("Taille visuelle (échelle du Visual).")] public float visualScale = 1f;

        [Header("Résistances (1 = normal, 0.5 = résistant, 1.5 = vulnérable)")]
        public float physicalMultiplier = 1f;
        public float fireMultiplier = 1f;
        public float frostMultiplier = 1f;
        public float magicMultiplier = 1f;
        [Tooltip("Immunisé au ralentissement.")] public bool slowImmune = false;

        [Header("Visuel")]
        [Tooltip("Prefab du visuel — glissez ici le modèle importé (voir ASSETS_IMPORT.md).")]
        public GameObject visualPrefab;

        public float MultiplierFor(DamageType t) => t switch
        {
            DamageType.Fire => fireMultiplier,
            DamageType.Frost => frostMultiplier,
            DamageType.Magic => magicMultiplier,
            _ => physicalMultiplier,
        };
    }
}
