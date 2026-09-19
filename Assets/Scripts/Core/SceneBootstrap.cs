using UnityEngine;

namespace Bastion.Core
{
    /// <summary>Branche les pools statiques de la scène (popups de dégâts). Un par scène.</summary>
    [DefaultExecutionOrder(-140)]
    public class SceneBootstrap : MonoBehaviour
    {
        [SerializeField] private UI.DamagePopup damagePopupPrefab;
        [SerializeField] private Transform popupRoot;

        private void Awake()
        {
            if (damagePopupPrefab != null) UI.DamagePopup.Register(damagePopupPrefab, popupRoot != null ? popupRoot : transform);
        }
    }
}
