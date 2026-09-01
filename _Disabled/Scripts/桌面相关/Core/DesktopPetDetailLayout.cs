using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主面板详情栏锁宽高：避免换条目时图标/文案把栏撑开。
/// LayoutElement / RectMask2D 须焊在 MainCanvas，缺则报错不运行时补。
/// </summary>
public static class DesktopPetDetailLayout
{
    public static void Stabilize(
        Image detailIcon,
        TextMeshProUGUI detailNameText,
        TextMeshProUGUI detailDescText,
        float previewHeight,
        float descMinHeight,
        float detailFlexibleWidth)
    {
        if (detailIcon != null)
        {
            detailIcon.preserveAspect = true;
            detailIcon.type = Image.Type.Simple;
            detailIcon.raycastTarget = false;

            LayoutElement iconLe = detailIcon.GetComponent<LayoutElement>();
            if (iconLe == null)
            {
                Debug.LogError(
                    "[DetailLayout] detailIcon 缺少 LayoutElement，请改 MainCanvas 预制体。",
                    detailIcon);
            }
            else
            {
                iconLe.ignoreLayout = true;
            }

            Transform preview = detailIcon.transform.parent;
            if (preview != null)
            {
                LayoutElement previewLe = preview.GetComponent<LayoutElement>();
                if (previewLe != null)
                {
                    previewLe.minHeight = previewHeight;
                    previewLe.preferredHeight = previewHeight;
                    previewLe.flexibleHeight = 0f;
                }

                if (preview.GetComponent<RectMask2D>() == null)
                {
                    Debug.LogError(
                        "[DetailLayout] Preview 缺少 RectMask2D，请改 MainCanvas 预制体。",
                        preview);
                }

                LayoutElement detailLe = preview.parent != null
                    ? preview.parent.GetComponent<LayoutElement>()
                    : null;
                if (detailLe != null)
                {
                    detailLe.minWidth = 280f;
                    detailLe.preferredWidth = 300f;
                    if (detailLe.flexibleWidth < 0.1f)
                        detailLe.flexibleWidth = detailFlexibleWidth;
                }
            }
        }

        if (detailNameText != null)
        {
            detailNameText.enableWordWrapping = false;
            detailNameText.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement nameLe = detailNameText.GetComponent<LayoutElement>();
            if (nameLe != null)
            {
                nameLe.minHeight = 28f;
                nameLe.preferredHeight = 28f;
                nameLe.flexibleHeight = 0f;
            }
        }

        if (detailDescText != null)
        {
            detailDescText.enableWordWrapping = true;
            detailDescText.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement descLe = detailDescText.GetComponent<LayoutElement>();
            if (descLe == null)
            {
                Debug.LogError(
                    "[DetailLayout] detailDescText 缺少 LayoutElement，请改 MainCanvas 预制体。",
                    detailDescText);
            }
            else
            {
                descLe.flexibleHeight = 1f;
                descLe.minHeight = descMinHeight;
                descLe.preferredHeight = 0f;
            }
        }
    }
}
