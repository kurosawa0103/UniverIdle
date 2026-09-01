// This code is part of the Fungus library (https://github.com/snozbot/fungus)
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;

namespace Fungus
{
    /// <summary>
    /// Waits for period of time before executing the next command in the block.
    /// </summary>
    [CommandInfo("Flow", 
                 "Wait", 
                 "Waits for period of time before executing the next command in the block.")]
    [AddComponentMenu("")]
    [ExecuteInEditMode]
    public class Wait : Command
    {
        [Tooltip("Duration to wait for")]
        [SerializeField] protected FloatData _duration = new FloatData(1);
        
        [Tooltip("勾选后，点击鼠标可以跳过等待")]
        [SerializeField] protected bool skipOnMouseClick = false;

        private Coroutine waitCoroutine;

        protected virtual void OnWaitComplete()
        {
            Continue();
        }

        #region Public members

        public override void OnEnter()
        {
            // 如果启用了点击跳过，使用协程来检测鼠标点击
            if (skipOnMouseClick)
            {
                waitCoroutine = StartCoroutine(WaitWithSkip());
            }
            else
            {
                // 保持原有行为，使用 Invoke
                Invoke("OnWaitComplete", _duration.Value);
            }
        }
        
        /// <summary>
        /// 带跳过功能的等待协程
        /// </summary>
        protected virtual IEnumerator WaitWithSkip()
        {
            float elapsed = 0f;
            float duration = _duration.Value;
            
            while (elapsed < duration)
            {
                // 检测鼠标点击（左键或右键）
                if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
                {
                    // 点击跳过等待
                    if (waitCoroutine != null)
                    {
                        StopCoroutine(waitCoroutine);
                        waitCoroutine = null;
                    }
                    OnWaitComplete();
                    yield break;
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 时间到了，正常继续
            waitCoroutine = null;
            OnWaitComplete();
        }
        
        public override void OnStopExecuting()
        {
            // 停止时取消协程
            if (waitCoroutine != null)
            {
                StopCoroutine(waitCoroutine);
                waitCoroutine = null;
            }
            
            // 取消 Invoke（如果使用了）
            CancelInvoke("OnWaitComplete");
        }

        public override string GetSummary()
        {
            string summary = _duration.Value.ToString() + " seconds";
            if (skipOnMouseClick)
            {
                summary += " (可点击跳过)";
            }
            return summary;
        }

        public override Color GetButtonColor()
        {
            return new Color32(235, 191, 217, 255);
        }

        public override bool HasReference(Variable variable)
        {
            return _duration.floatRef == variable || base.HasReference(variable);
        }

        #endregion

        #region Backwards compatibility

        [HideInInspector] [FormerlySerializedAs("duration")] public float durationOLD;

        protected virtual void OnEnable()
        {
            if (durationOLD != default(float))
            {
                _duration.Value = durationOLD;
                durationOLD = default(float);
            }
        }

        #endregion
    }
}