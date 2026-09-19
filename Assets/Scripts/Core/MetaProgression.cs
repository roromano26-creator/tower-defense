using UnityEngine;

namespace Bastion.Core
{
    /// <summary>
    /// Bonus permanents du profil, calculés à partir des rangs sauvegardés. Toutes les valeurs de
    /// gameplay qui peuvent évoluer passent par ici : dégâts, coûts, or de départ, vies, primes, revente.
    /// </summary>
    public static class MetaProgression
    {
        public static float DamageMultiplier => 1f + Sum(MetaEffect.TowerDamage);
        public static float CostMultiplier => Mathf.Max(0.4f, 1f - Sum(MetaEffect.TowerCost));
        public static int ExtraStartingGold => Mathf.RoundToInt(Sum(MetaEffect.StartingGold));
        public static int ExtraStartingLives => Mathf.RoundToInt(Sum(MetaEffect.StartingLives));
        public static float EnemyGoldMultiplier => 1f + Sum(MetaEffect.EnemyGold);
        public static float SellRefundBonus => Sum(MetaEffect.SellRefund);
        public static float MonsterHealthMultiplier => 1f + Sum(MetaEffect.MonsterHealth);   // mode Assaut
        public static float ManaRegenMultiplier => 1f + Sum(MetaEffect.ManaRegen);           // mode Assaut

        /// <summary>Coût effectif d'une tour ou d'un upgrade après réduction permanente.</summary>
        public static int Cost(int baseCost) => Mathf.Max(1, Mathf.RoundToInt(baseCost * CostMultiplier));

        private static float Sum(MetaEffect effect)
        {
            var catalog = LevelCatalog.Load();
            if (catalog == null) return 0f;
            float total = 0f;
            foreach (var u in catalog.upgrades)
                if (u != null && u.effect == effect) total += u.valuePerRank * SaveSystem.GetRank(u.upgradeId);
            return total;
        }
    }
}
