using UnityEngine;

namespace Bastion.UI.Menu
{
    /// <summary>Décor 3D du menu : un plateau de tours qui tourne lentement derrière l'UI, avec un léger flottement.</summary>
    public class MenuShowcase : MonoBehaviour
    {
        [SerializeField] private float rotationSpeed = 6f;
        [SerializeField] private float bob = 0.08f;
        private Vector3 basePos;
        private void Start() { basePos = transform.position; }
        private void Update()
        {
            transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
            transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * 0.8f) * bob;
        }
    }
}
