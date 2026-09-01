using UnityEngine;
using System.Collections;

namespace Fungus
{
    /// <summary>
    /// The block will execute when the mouse enters or exits the target object.
    /// </summary>
    [EventHandlerInfo("Sprite",
                      "Object Hovered",
                      "The block will execute when the mouse enters or exits the target object.")]
    [AddComponentMenu("")]
    public class ObjectHovered : EventHandler
    {
        public enum HoverPhase
        {
            MouseEnter,
            MouseExit
        }

        public class ObjectHoveredEvent
        {
            public Clickable2D ClickableObject;
            public HoverPhase Phase;

            public ObjectHoveredEvent(Clickable2D clickableObject, HoverPhase phase)
            {
                ClickableObject = clickableObject;
                Phase = phase;
            }
        }

        [Tooltip("Object that the user can hover on")]
        [SerializeField] protected Clickable2D clickableObject;

        [Tooltip("Wait for a number of frames before executing the block.")]
        [SerializeField] protected int waitFrames = 1;

        [Tooltip("Which hover phase triggers this event")]
        [SerializeField] protected HoverPhase triggerPhase = HoverPhase.MouseEnter;

        protected EventDispatcher eventDispatcher;

        protected virtual void OnEnable()
        {
            eventDispatcher = FungusManager.Instance.EventDispatcher;
            eventDispatcher.AddListener<ObjectHoveredEvent>(OnObjectHoveredEvent);
        }

        protected virtual void OnDisable()
        {
            if (eventDispatcher != null)
            {
                eventDispatcher.RemoveListener<ObjectHoveredEvent>(OnObjectHoveredEvent);
            }

            eventDispatcher = null;
        }

        void OnObjectHoveredEvent(ObjectHoveredEvent evt)
        {
            if (evt.ClickableObject == this.clickableObject && evt.Phase == this.triggerPhase)
            {
                StartCoroutine(DoExecuteBlock(waitFrames));
            }
        }

        protected virtual IEnumerator DoExecuteBlock(int numFrames)
        {
            int count = Mathf.Max(numFrames, 1);
            while (count-- > 0)
            {
                yield return new WaitForEndOfFrame();
            }

            ExecuteBlock();
        }

        public override string GetSummary()
        {
            return clickableObject != null ? $"{clickableObject.name} ({triggerPhase})" : "None";
        }

        #region Public Trigger Callers

        public static void RaiseHoverEvent(Clickable2D obj, HoverPhase phase)
        {
            if (obj != null)
            {
                var evt = new ObjectHoveredEvent(obj, phase);
                FungusManager.Instance.EventDispatcher.Raise(evt);
            }
        }

        #endregion
    }
}
