using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 页签选中态：颜色单点维护；保留预制体 Image.sprite，不运行时清空。
/// </summary>
public static class DesktopPetTabVisual
{
    private static readonly Color Selected = new Color(0.32f, 0.52f, 0.72f, 1f);
    private static readonly Color Unselected = new Color(0.28f, 0.31f, 0.38f, 1f);

    public static void Apply(Button button, bool on)
    {
        if (button == null)
            return;

        Image img = button.targetGraphic as Image;
        if (img != null)
            img.color = on ? Selected : Unselected;

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.fontStyle = on ? FontStyles.Bold : FontStyles.Normal;
    }
}
