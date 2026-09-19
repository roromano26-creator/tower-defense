using System.Collections.Generic;
using UnityEngine;
using Bastion.Enemies;

namespace Bastion.Towers
{
    /// <summary>Choix de cible parmi les ennemis à portée. Fonction pure, testable sans scène.</summary>
    public static class TowerTargeting
    {
        public static Enemy Pick(List<Enemy> candidates, TargetPriority priority, Vector3 from)
        {
            Enemy best = null; float bestScore = float.NegativeInfinity;
            foreach (var e in candidates)
            {
                float score = priority switch
                {
                    TargetPriority.First => e.DistanceTravelled,
                    TargetPriority.Closest => -(e.transform.position - from).sqrMagnitude,
                    TargetPriority.Strongest => e.Health,
                    TargetPriority.Weakest => -e.Health,
                    _ => 0f,
                };
                if (score > bestScore) { bestScore = score; best = e; }
            }
            return best;
        }
    }
}
