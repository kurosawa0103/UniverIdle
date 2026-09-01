using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using I2.Loc;

[RequireComponent(typeof(Button))]
public class MusicSlot : MonoBehaviour
{
    [HideInInspector]
    public MusicTrackItem trackData;

    [Header("UI 引用")]
    public Image coverImage;
    public TextMeshProUGUI songNameText;
    public GameObject selectedIndicator;

    [Header("音效")]
    [Tooltip("点击槽位时播放，直接拖入 AudioClip；留空则不播")]
    public AudioClip clickSfxClip;
    [Range(0f, 1f)]
    public float clickSfxVolume = 1f;

    private MusicPageView pageView;
    private Button btn;
    private Localize nameLocalize;
    private bool nameLocalizeResolved;

    private void Awake()
    {
        if (btn == null)
            btn = GetComponent<Button>();

        btn.onClick.AddListener(OnSlotClicked);
        DisableChildRaycasts();
        ResolveNameLocalize();
    }

    private void DisableChildRaycasts()
    {
        if (songNameText != null)
            songNameText.raycastTarget = false;

        if (selectedIndicator == null)
            return;

        foreach (var image in selectedIndicator.GetComponentsInChildren<Image>(true))
            image.raycastTarget = false;
    }

    private void ResolveNameLocalize()
    {
        if (nameLocalizeResolved)
            return;

        nameLocalize = songNameText != null ? songNameText.GetComponent<Localize>() : null;
        nameLocalizeResolved = true;
    }

    public void Setup(MusicTrackItem item, MusicPageView view)
    {
        trackData = item;
        pageView = view;
        SetSelected(false);
        Refresh();
    }

    public void Refresh()
    {
        if (trackData == null)
        {
            if (coverImage != null)
                coverImage.color = new Color(1f, 1f, 1f, 0f);
            if (songNameText != null)
                songNameText.text = string.Empty;
            if (btn != null)
                btn.interactable = false;
            return;
        }

        if (coverImage != null)
        {
            coverImage.sprite = trackData.coverSmall;
            coverImage.color = Color.white;
        }

        if (songNameText != null)
            songNameText.text = GetLocalizedDisplayName(trackData.displayName);

        if (btn != null)
            btn.interactable = trackData.audioClip != null;
    }

    public void SetSelected(bool selected)
    {
        if (selectedIndicator != null)
            selectedIndicator.SetActive(selected);

        RefreshCoverColor();
    }

    private void RefreshCoverColor()
    {
        if (coverImage == null || trackData == null)
            return;

        coverImage.color = Color.white;
    }

    private void ResetButtonVisualState()
    {
        if (btn == null)
            return;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        btn.OnDeselect(null);
        RefreshCoverColor();
    }

    private void OnSlotClicked()
    {
        if (trackData == null || pageView == null)
            return;

        pageView.PlayUiClickSfx(clickSfxClip, clickSfxVolume);
        pageView.SelectTrack(this, trackData);
        ResetButtonVisualState();
    }

    private string GetLocalizedDisplayName(string termKey)
    {
        if (string.IsNullOrEmpty(termKey))
            return string.Empty;

        ResolveNameLocalize();
        if (nameLocalize != null)
        {
            nameLocalize.SetTerm(termKey);
            string translated = LocalizationManager.GetTranslation(termKey);
            if (!string.IsNullOrEmpty(translated))
                return FixSpecialPlaceholders(translated);
        }
        else
        {
            string translated = LocalizationManager.GetTranslation(termKey);
            if (!string.IsNullOrEmpty(translated) && translated != termKey)
                return FixSpecialPlaceholders(translated);
        }

        return FixSpecialPlaceholders(termKey);
    }

    private static string FixSpecialPlaceholders(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return input.Replace("{c}", ",");
    }
}
