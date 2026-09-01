using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 只影响本组件配置的 Button + 文字。
/// 选中态由 selected / SetSelected 控制，不会联动其它按钮。
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonHoverTextColor : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("文字（留空则在 Button 子物体中查找）")]
    public TMP_Text labelText;
    public Text legacyText;

    [Header("选中（只影响本按钮）")]
    public bool selected;

    [Header("文字颜色")]
    public Color normalColor = Color.black;
    public Color hoverColor = Color.white;
    public Color selectedColor = Color.white;
    public Color pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    public Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    [Header("响应状态")]
    public bool respondToHover = true;
    public bool respondToPress = true;

    private Button button;
    private bool isPointerInside;
    private bool isPressed;
    private bool lastInteractable = true;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (labelText == null)
            labelText = button.GetComponentInChildren<TMP_Text>(true);

        if (legacyText == null && labelText == null)
            legacyText = button.GetComponentInChildren<Text>(true);
    }

    private void OnEnable()
    {
        isPointerInside = false;
        isPressed = false;
        lastInteractable = button == null || button.interactable;
        RefreshTextColor();
    }

    private void Update()
    {
        if (button == null)
            return;

        bool interactable = button.interactable;
        if (interactable == lastInteractable)
            return;

        lastInteractable = interactable;
        if (!interactable)
        {
            isPointerInside = false;
            isPressed = false;
        }

        RefreshTextColor();
    }

    public void SetSelected(bool value)
    {
        if (selected == value)
            return;

        selected = value;
        RefreshTextColor();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button == null || !button.interactable)
            return;

        isPointerInside = true;
        RefreshTextColor();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
        isPressed = false;
        RefreshTextColor();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button == null || !button.interactable)
            return;

        isPressed = true;
        RefreshTextColor();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (button == null || !button.interactable)
            return;

        isPressed = false;
        RefreshTextColor();
    }

    private void RefreshTextColor()
    {
        Color color = ResolveColor();

        if (labelText != null)
            labelText.color = color;
        else if (legacyText != null)
            legacyText.color = color;
    }

    private Color ResolveColor()
    {
        if (button != null && !button.interactable)
            return disabledColor;

        if (respondToPress && isPressed)
            return pressedColor;

        if (respondToHover && isPointerInside)
            return hoverColor;

        if (selected)
            return selectedColor;

        return normalColor;
    }
}
