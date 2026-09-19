using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Bastion.Core;
using Bastion.Towers;

namespace Bastion.UI
{
    /// <summary>Panneau de la tour sélectionnée : stats, upgrade, vente, priorité de cible.</summary>
    public class TowerUpgradePanel : MonoBehaviour
    {
        [SerializeField] private UIPulse pulse;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private TMP_Text upgradeLabel;
        [SerializeField] private Button sellButton;
        [SerializeField] private TMP_Text sellLabel;
        [SerializeField] private Button priorityButton;
        [SerializeField] private TMP_Text priorityLabel;
        [SerializeField] private Button closeButton;
        [SerializeField] private TowerPlacer placer;

        private Tower current;

        private void Awake()
        {
            GameEvents.OnTowerSelected += Show;
            GameEvents.OnGoldChanged += _ => Refresh();
            upgradeButton.onClick.AddListener(() => { if (TowerManager.Instance.TryUpgrade(current)) Refresh(); });
            sellButton.onClick.AddListener(() => { TowerManager.Instance.Sell(current); placer.ClearSelection(); });
            priorityButton.onClick.AddListener(CyclePriority);
            closeButton.onClick.AddListener(() => placer.ClearSelection());
            pulse.Show(false);
        }

        private void Show(Tower t)
        {
            current = t;
            pulse.Show(t != null);
            Refresh();
        }

        private void Refresh()
        {
            if (current == null) return;
            var d = current.Data; var l = current.Level;
            titleText.text = $"{d.displayName}  <size=70%>niv. {current.LevelIndex + 1}/{d.levels.Length}</size>";
            string extra = d.attackKind switch
            {
                AttackKind.AreaOfEffect => $" · Zone {l.splashRadius:0.#}",
                AttackKind.Slow => $" · Ralentit ×{l.slowFactor:0.00} pendant {l.effectDuration:0.#} s",
                AttackKind.Burn => $" · Brûlure {l.dotDamagePerSecond:0}/s pendant {l.effectDuration:0.#} s",
                _ => "",
            };
            statsText.text = $"Dégâts {l.damage:0} · Portée {l.range:0.#} · Cadence {l.fireRate:0.#}/s{extra}";
            if (current.CanUpgrade)
            {
                var n = d.levels[current.LevelIndex + 1];
                upgradeLabel.text = $"Améliorer · {current.UpgradeCost} or\n<size=70%>Dégâts {n.damage:0} · Portée {n.range:0.#}</size>";
                upgradeButton.interactable = ResourceManager.Instance.CanAfford(current.UpgradeCost);
            }
            else { upgradeLabel.text = "Niveau max"; upgradeButton.interactable = false; }
            sellLabel.text = $"Vendre · +{current.SellValue} or";
            priorityLabel.text = "Cible : " + (current.Priority switch
            {
                TargetPriority.First => "premier",
                TargetPriority.Closest => "plus proche",
                TargetPriority.Strongest => "plus fort",
                _ => "plus faible",
            });
        }

        private void CyclePriority()
        {
            if (current == null) return;
            current.Priority = (TargetPriority)(((int)current.Priority + 1) % 4);
            Refresh();
        }
    }
}
