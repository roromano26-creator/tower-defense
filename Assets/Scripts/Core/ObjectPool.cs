using System.Collections.Generic;
using UnityEngine;

namespace Bastion.Core
{
    /// <summary>
    /// Pool générique de prefabs. Aucun Instantiate pendant une vague : tout ce qui apparaît
    /// souvent (projectiles, ennemis, VFX, popups) passe par ici.
    /// </summary>
    public class ObjectPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform parent;
        private readonly Stack<T> free = new();

        public ObjectPool(T prefab, Transform parent, int prewarm = 0)
        {
            this.prefab = prefab;
            this.parent = parent;
            for (int i = 0; i < prewarm; i++) Release(Create());
        }

        private T Create()
        {
            var inst = Object.Instantiate(prefab, parent);
            inst.gameObject.SetActive(false);
            return inst;
        }

        public T Get(Vector3 position, Quaternion rotation)
        {
            var inst = free.Count > 0 ? free.Pop() : Create();
            inst.transform.SetPositionAndRotation(position, rotation);
            inst.gameObject.SetActive(true);
            return inst;
        }

        public void Release(T inst)
        {
            if (inst == null) return;
            inst.gameObject.SetActive(false);
            inst.transform.SetParent(parent, false);
            free.Push(inst);
        }
    }
}
