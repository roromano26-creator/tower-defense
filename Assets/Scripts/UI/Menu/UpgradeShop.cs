using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Bastion.Core;

namespace Bastion.UI.Menu
{
    /// <summary>Boutique des améliorations permanentes : une ligne par MetaUpgradeData du catalogue.</summary>
    public class UpgradeShop : MonoBehaviour
    {
        [SerializeField] private UIPulse pulse;
        [SerializeField] private Transform rowsRoot;
        [SerializeField] private UpgradeRow rowPrefab;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text gemsText;

        private readonly List<UpgradeRow> rows = new();
        public System.Action OnChanged;

        private void Awake()
        {
            closeButton.onClick.AddListener(() => pulse.Show(false));
            pulse.Show(false);
        }

        public void Open()
        {
            var catalog = LevelCatalog.Load();
            if (rows.Count == 0 && catalog != null)
                foreach (var u in catalog.upgrades)
                {
                    if (u == null) continue;
                    var row = Instantiate(rowPrefab, rowsRoot);
                    row.Bind(u, Refresh);
                    rows.Add(row);
                }
            Refresh();
            pulse.Show(true);
        }

        private void Refresh()
        {
            gemsText.text = $"{SaveSystem.Gems} ◆";
            foreach (var r in rows) r.Refresh();
            OnChanged?.Invoke();
        }
    }
}
