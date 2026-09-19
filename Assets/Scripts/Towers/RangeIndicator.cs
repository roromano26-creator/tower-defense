using UnityEngine;

namespace Bastion.Towers
{
    /// <summary>Disque de portée au sol, affiché à la sélection ou pendant le placement.</summary>
    public class RangeIndicator : MonoBehaviour
    {
        [SerializeField] private Transform disc;

        public void SetRadius(float r) { disc.localScale = new Vector3(r * 2f, disc.localScale.y, r * 2f); }
        public void Show(bool on) { disc.gameObject.SetActive(on); }
    }
}
