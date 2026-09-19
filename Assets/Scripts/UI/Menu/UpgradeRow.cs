using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Bastion.Core;

namespace Bastion.UI.Menu
{
    public class UpgradeRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descText;
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private Button buyButton;
        [SerializeField] private TMP_Text buyLabel;
        [SerializeField] private Image accent;

        private MetaUpgradeData data;
        private System.Action onChanged;

        public void Bind(MetaUpgradeData u, System.Action changed)
        {
            data = u; onChanged = changed;
            titleText.text = u.displayName; descText.text = u.description;
            if (accent != null) accent.color = u.accent;
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => { if (SaveSystem.TryBuyUpgrade(data)) onChanged?.Invoke(); });
            Refresh();
        }

        public void Refresh()
        {
            int rank = SaveSystem.GetRank(data.upgradeId);
            rankText.text = new string('●', rank) + new string('○', Mathf.Max(0, data.maxRank - rank));
            if (rank >= data.maxRank) { buyLabel.text = "Max"; buyButton.interactable = false; return; }
            int cost = data.CostForRank(rank + 1);
            buyLabel.text = $"{cost} ◆";
            buyButton.interactable = SaveSystem.Gems >= cost;
        }
    }
}
