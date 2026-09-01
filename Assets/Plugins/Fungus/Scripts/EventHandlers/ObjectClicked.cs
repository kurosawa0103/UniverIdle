using UnityEngine;
using System.Collections;

namespace Fungus
{
    /// <summary>
    /// The block will execute when the user clicks or taps on the clickable object.
    /// </summary>
    [EventHandlerInfo("Sprite",
                      "Object Clicked",
                      "The block will execute when the user clicks or taps on the clickable object.")]
    [AddComponentMenu("")]
    public class ObjectClicked : EventHandler
    {
        public enum ClickPhase
        {
            MouseDown,
            MouseUp
        }

        public class ObjectClickedEvent
        {
            public Clickable2D ClickableObject;
            public ClickPhase Phase;

            public ObjectClickedEvent(Clickable2D clickableObject, ClickPhase phase)
            {
                ClickableObject = clickableObject;
                Phase = phase;
            }
        }

        [Tooltip("Object that the user can click or tap on")]
        [SerializeField] protected Clickable2D clickableObject;

        [Tooltip("Wait for a number of frames before executing the block.")]
        [SerializeField] protected int waitFrames = 1;

        [Tooltip("Which mouse phase triggers this event")]
        [SerializeField] protected ClickPhase triggerPhase = ClickPhase.MouseDown;

        protected EventDispatcher eventDispatcher;

        protected virtual void OnEnable()
        {
            eventDispatcher = FungusManager.Instance.EventDispatcher;
            eventDispatcher.AddListener<ObjectClickedEvent>(OnObjectClickedEvent);
        }

        protected virtual void OnDisable()
        {
            if (eventDispatcher != null)
            {
                eventDispatcher.RemoveListener<ObjectClickedEvent>(OnObjectClickedEvent);
            }

            eventDispatcher = null;
        }

        void OnObjectClickedEvent(ObjectClickedEvent evt)
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

        public static void RaiseClickEvent(Clickable2D obj, ClickPhase phase)
        {
            if (obj != null)
            {
                var evt = new ObjectClickedEvent(obj, phase);
                FungusManager.Instance.EventDispatcher.Raise(evt);
            }
        }

        #endregion
    }
}
