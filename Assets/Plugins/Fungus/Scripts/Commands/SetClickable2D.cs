using UnityEngine;
using System.Collections.Generic;

namespace Fungus
{
    /// <summary>
    /// Sets multiple Clickable2D components to be clickable / non-clickable.
    /// </summary>
    [CommandInfo("Sprite",
                 "Set Multiple Clickable 2D",
                 "Sets multiple Clickable2D components to be clickable / non-clickable.")]
    [AddComponentMenu("")]
    public class SetClickable2D : Command
    {
        [Tooltip("References to Clickable2D components on gameobjects")]
        [SerializeField] protected List<Clickable2D> targetClickable2Ds = new List<Clickable2D>();

        [Tooltip("Set to true to enable the components")]
        [SerializeField] protected BooleanData activeState;

        [Tooltip("Whether to use parent-child hierarchy to find Clickable2D objects")]
        [SerializeField] protected bool useParentHierarchy = false;

        [Tooltip("Optional parent object to find Clickable2D objects within its children")]
        [SerializeField] protected Transform parentTransform;

        #region Public members

        public override void OnEnter()
        {
            if (useParentHierarchy && parentTransform != null)
            {
                // 如果启用了父集查找，遍历父物体下的所有子物体，找到 Clickable2D 组件
                var clickable2DsInParent = parentTransform.GetComponentsInChildren<Clickable2D>();

                if (clickable2DsInParent != null && clickable2DsInParent.Length > 0)
                {
                    foreach (var clickable in clickable2DsInParent)
                    {
                        if (clickable != null)
                        {
                            clickable.ClickEnabled = activeState.Value;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("没有找到可点击的 Clickable2D 组件！");
                }
            }
            else if (targetClickable2Ds != null && targetClickable2Ds.Count > 0)
            {
                // 如果没有启用父集查找，使用配置的物品列表
                foreach (var clickable in targetClickable2Ds)
                {
                    if (clickable != null)
                    {
                        clickable.ClickEnabled = activeState.Value;
                    }
                }
            }
            else
            {
                Debug.LogWarning("未指定目标 Clickable2D 组件，且未启用父集查找！");
            }

            Continue();
        }

        public override string GetSummary()
        {
            if (useParentHierarchy)
            {
                if (parentTransform != null)
                {
                    return $"父物体：{parentTransform.gameObject.name} 下的所有子物体将会被操作";
                }
                else
                {
                    return "错误：父物体未设置";
                }
            }

            if (targetClickable2Ds == null || targetClickable2Ds.Count == 0)
            {
                return "错误：没有指定 Clickable2D 组件";
            }

            string summary = "目标物体：";
            foreach (var clickable in targetClickable2Ds)
            {
                if (clickable != null)
                {
                    summary += clickable.gameObject.name + ", ";
                }
            }

            return summary.TrimEnd(',', ' ');
        }

        public override Color GetButtonColor()
        {
            return new Color32(235, 191, 217, 255);
        }

        public override bool HasReference(Variable variable)
        {
            return activeState.booleanRef == variable || base.HasReference(variable);
        }

        #endregion
    }
}
