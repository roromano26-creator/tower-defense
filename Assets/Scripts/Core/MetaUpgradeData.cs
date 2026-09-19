using UnityEngine;

namespace Bastion.Core
{
    public enum MetaEffect { TowerDamage, TowerCost, StartingGold, StartingLives, EnemyGold, SellRefund, MonsterHealth, ManaRegen }

    /// <summary>
    /// Amélioration permanente achetée avec les gemmes gagnées par les étoiles. Un effet, plusieurs rangs.
    /// Ajouter une amélioration = créer un asset et l'inscrire dans le LevelCatalog ; aucun code.
    /// </summary>
    [CreateAssetMenu(menuName = "Bastion/Amélioration permanente", fileName = "Meta_")]
    public class MetaUpgradeData : ScriptableObject
    {
        public string upgradeId = "damage";
        public string displayName = "Forge";
        [TextArea] public string description = "+8 % de dégâts des tours par rang.";
        public MetaEffect effect = MetaEffect.TowerDamage;
        [Tooltip("Valeur par rang : 0.08 = +8 % (multiplicateurs) ou +N (or / vies).")] public float valuePerRank = 0.08f;
        public int maxRank = 5;
        [Tooltip("Coût en gemmes du rang 1, puis × costGrowth par rang.")] public int baseCost = 20;
        public float costGrowth = 1.6f;
        public Color accent = new(0.24f, 0.86f, 0.52f);

        public int CostForRank(int nextRank) => Mathf.RoundToInt(baseCost * Mathf.Pow(costGrowth, nextRank - 1));
    }
}
