using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Bastion.Core;
using Bastion.Assault;
using Bastion.Enemies;

namespace Bastion.UI
{
    /// <summary>Barre du bas du mode Assaut : un bouton par monstre déployable, coût en mana, cooldown des boss.</summary>
    public class MonsterDeployBar : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Transform buttonsRoot;
        [SerializeField] private Button buttonPrefab;
        [SerializeField] private Image manaFill;

        private readonly List<(Button b, TMP_Text label, EnemyData e)> buttons = new();
        private bool built;

        private void Start()
        {
            bool assault = GameManager.Instance != null && GameManager.Instance.IsAssault;
            root.SetActive(assault);
            if (!assault) { enabled = false; return; }
            GameEvents.OnManaChanged += (m, cap) => { if (manaFill != null) manaFill.fillAmount = cap > 0 ? m / cap : 0f; };
        }

        private void Update()
        {
            var ctrl = AssaultController.Instance;
            if (ctrl == null || ctrl.Pool == null) return;
            if (!built) Build(ctrl);
            foreach (var (b, label, e) in buttons)
            {
                bool cd = ctrl.OnCooldown(e, out float remaining);
                b.interactable = ctrl.CanDeploy(e);
                label.text = cd ? $"{e.displayName}\n<size=70%>{Mathf.CeilToInt(remaining)} s</size>" : $"{e.displayName}\n<size=70%>{ctrl.CostOf(e)} ✦</size>";
            }
        }

        private void Build(AssaultController ctrl)
        {
            built = true;
            foreach (var e in ctrl.Pool)
            {
                var b = Instantiate(buttonPrefab, buttonsRoot);
                var label = b.GetComponentInChildren<TMP_Text>();
                label.richText = true;
                var img = b.GetComponent<Image>(); if (img != null) img.color = e.isBoss ? new Color(0.6f, 0.15f, 0.2f) : e.accentColor * 0.75f;
                var data = e;
                b.onClick.AddListener(() => ctrl.Deploy(data));
                buttons.Add((b, label, e));
            }
        }
    }
}
