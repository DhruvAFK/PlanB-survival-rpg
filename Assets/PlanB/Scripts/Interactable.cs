using System.Collections.Generic;
using UnityEngine;

namespace PlanB
{
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();
        public float Range = 1.85f;
        public abstract string Prompt { get; }
        public virtual bool Available { get { return true; } }
        public abstract void Interact();
        protected virtual void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        protected virtual void OnDisable() { All.Remove(this); }
        protected virtual void OnDestroy() { All.Remove(this); }
        protected void SetSprite(string key)
        {
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.sprite = SpriteLibrary.Get(key);
        }
    }
}

