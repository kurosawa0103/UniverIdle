using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using I2.Loc;
using System.Text.RegularExpressions;

public class MusicPageView : MonoBehaviour
{
    private struct PooledSlotEntry
    {
        public GameObject root;
        public MusicSlot slot;
    }

    [Header("列表")]
    public Transform gridParent;
    public GameObject slotPrefab;
    public Button prevButton;
    public Button nextButton;
    public TextMeshProUGUI pageText;
    public int itemsPerPage = 15;

    [Header("分页滑动")]
    public bool enablePageSlide = true;
    [Min(0f)]
    public float pageSlideDuration = 0.35f;

    [Header("中央展示区")]
    public Image centralCoverImage;
    public TextMeshProUGUI songNameText;
    [Tooltip("封面旋转组件；留空时自动从 centralCoverImage 上查找")]
    public MusicCoverRotator coverRotator;

    [Header("点击详情（打字机）")]
    public TextMeshProUGUI nameDisplay;
    public TextMeshProUGUI descDisplay;

    [Header("背景")]
    public Image backgroundImage;
    [Tooltip("可选第二图层，用于交叉淡入淡出；留空则单图层淡出再切入")]
    public Image backgroundCrossfadeImage;
    [Range(0f, 1f)]
    public float backgroundMaxAlpha = 1f;

    [Header("播放")]
    public AudioSource bgmAudioSource;
    [Range(0f, 1f)]
    public float volume = 1f;
    [Min(0f)]
    public float fadeDuration = 0.35f;
    [Tooltip("进入场景时自动选中并播放第一首")]
    public bool autoSelectFirstOnStart = true;

    private readonly List<MusicTrackItem> allTracks = new List<MusicTrackItem>();
    private readonly List<GameObject> currentSlots = new List<GameObject>();
    private readonly List<PooledSlotEntry> pooledSlots = new List<PooledSlotEntry>();
    private readonly GameObject[] pagePanels = new GameObject[2];
    private int currentPage;
    private int totalPages = 1;
    private int activePanelIndex;
    private MusicSlot selectedSlot;
    private MusicTrackItem selectedTrack;
    private Coroutine fadeCoroutine;
    private Coroutine backgroundFadeCoroutine;
    private Coroutine pageTransitionCoroutine;
    private Sprite currentBackgroundSprite;
    private bool backgroundUsingFrontLayer = true;
    private RectTransform pageViewport;
    private RectTransform pageTrack;
    private GridLayoutGroup gridLayoutTemplate;
    private float cachedPageWidth;
    private bool isPageTransitioning;
    private AudioSource uiAudioSource;

    private void Start()
    {
        SetupCoverRotator();
        SetupBackground();
        SetupUiAudioSource();
        SetupPageSlide();
        LoadAllTracks();
        totalPages = Mathf.Max(1, Mathf.CeilToInt(allTracks.Count / (float)itemsPerPage));

        if (prevButton != null)
            prevButton.onClick.AddListener(PrevPage);
        if (nextButton != null)
            nextButton.onClick.AddListener(NextPage);

        ShowPageImmediate(0);

        if (autoSelectFirstOnStart && allTracks.Count > 0)
            StartCoroutine(SelectFirstTrackOnStart());
    }

    private IEnumerator SelectFirstTrackOnStart()
    {
        yield return null;
        CachePageWidth();

        if (currentSlots.Count == 0)
            yield break;

        var slot = currentSlots[0].GetComponentInChildren<MusicSlot>(true);
        if (slot != null)
            SelectTrack(slot, allTracks[0], playImmediate: true);
    }

    private void SetupUiAudioSource()
    {
        uiAudioSource = GetComponent<AudioSource>();
        if (uiAudioSource == null)
            uiAudioSource = gameObject.AddComponent<AudioSource>();
        uiAudioSource.playOnAwake = false;
        uiAudioSource.spatialBlend = 0f;
    }

    public void PlayUiClickSfx(AudioClip clip, float clipVolume)
    {
        if (clip == null || clipVolume <= 0f || uiAudioSource == null)
            return;

        uiAudioSource.PlayOneShot(clip, clipVolume);
    }

    private void SetupCoverRotator()
    {
        if (centralCoverImage == null)
            return;

        if (coverRotator == null)
            coverRotator = centralCoverImage.GetComponent<MusicCoverRotator>();

        if (coverRotator == null)
            coverRotator = centralCoverImage.gameObject.AddComponent<MusicCoverRotator>();

        if (coverRotator.audioSource == null)
            coverRotator.audioSource = bgmAudioSource;
    }

    private void SetupBackground()
    {
        if (backgroundImage == null)
            return;

        currentBackgroundSprite = backgroundImage.sprite;

        if (backgroundCrossfadeImage != null)
        {
            SetImageAlpha(backgroundCrossfadeImage, 0f);
            backgroundUsingFrontLayer = true;
        }
    }

    private void OnDestroy()
    {
        if (backgroundFadeCoroutine != null)
            StopCoroutine(backgroundFadeCoroutine);

        if (pageTransitionCoroutine != null)
            StopCoroutine(pageTransitionCoroutine);

        if (prevButton != null)
            prevButton.onClick.RemoveListener(PrevPage);
        if (nextButton != null)
            nextButton.onClick.RemoveListener(NextPage);
    }

    private void SetupPageSlide()
    {
        if (!enablePageSlide || gridParent == null)
            return;

        pageViewport = gridParent as RectTransform;
        if (pageViewport == null)
        {
            enablePageSlide = false;
            return;
        }

        gridLayoutTemplate = gridParent.GetComponent<GridLayoutGroup>();
        if (gridLayoutTemplate == null)
        {
            enablePageSlide = false;
            return;
        }

        if (pageViewport.GetComponent<RectMask2D>() == null)
            pageViewport.gameObject.AddComponent<RectMask2D>();

        var trackGO = new GameObject("PageTrack", typeof(RectTransform));
        trackGO.transform.SetParent(pageViewport, false);
        pageTrack = trackGO.GetComponent<RectTransform>();
        StretchRect(pageTrack);

        gridLayoutTemplate.enabled = false;
        CreatePooledPagePanels();
    }

    private void CreatePooledPagePanels()
    {
        for (int i = 0; i < pagePanels.Length; i++)
        {
            pagePanels[i] = CreateEmptyPagePanel($"PagePanel_{i}");
            pagePanels[i].SetActive(i == 0);

            for (int slotIndex = 0; slotIndex < itemsPerPage; slotIndex++)
            {
                var slotRoot = Instantiate(slotPrefab, pagePanels[i].transform);
                slotRoot.SetActive(false);

                var slot = slotRoot.GetComponentInChildren<MusicSlot>(true);
                if (slot == null)
                {
                    Debug.LogError("[MusicPageView] slotPrefab 上未找到 MusicSlot 组件。", slotPrefab);
                    Destroy(slotRoot);
                    continue;
                }

                pooledSlots.Add(new PooledSlotEntry { root = slotRoot, slot = slot });
            }
        }
    }

    private void LoadAllTracks()
    {
        allTracks.Clear();
        MusicTrackItem[] items = Resources.LoadAll<MusicTrackItem>("GameData/Music");

        foreach (var item in items)
        {
            if (item.parsedId == 0 && !string.IsNullOrEmpty(item.id))
                item.parsedId = ParseIdNumber(item.id);
        }

        System.Array.Sort(items, (a, b) =>
        {
            int orderCompare = a.sortOrder.CompareTo(b.sortOrder);
            if (orderCompare != 0)
                return orderCompare;

            if (a.parsedId != b.parsedId)
                return a.parsedId.CompareTo(b.parsedId);

            return string.Compare(a.id, b.id, System.StringComparison.Ordinal);
        });

        allTracks.AddRange(items);
        Debug.Log($"[MusicPageView] 加载 {allTracks.Count} 首曲目");
    }

    private static int ParseIdNumber(string id)
    {
        if (string.IsNullOrEmpty(id))
            return 0;

        string digits = Regex.Match(id, @"\d+").Value;
        return int.TryParse(digits, out int number) ? number : 0;
    }

    private void ShowPageImmediate(int pageIndex)
    {
        ClearSlotHighlight();

        if (UsesPageSlide())
        {
            for (int i = 0; i < pagePanels.Length; i++)
            {
                bool active = i == 0;
                pagePanels[i].SetActive(active);
                if (active)
                {
                    ResetPanelPosition(pagePanels[i]);
                    BindPagePanel(pagePanels[i], pageIndex);
                }
            }

            activePanelIndex = 0;
        }
        else
        {
            ClearInstantiatedSlots();
            PopulateSlotsIntoParent(gridParent, pageIndex, currentSlots);
        }

        currentPage = pageIndex;
        RestoreSelectionHighlight();
        UpdatePageControls();
    }

    private void ClearInstantiatedSlots()
    {
        foreach (var slot in currentSlots)
            Destroy(slot);
        currentSlots.Clear();
    }

    private bool UsesPageSlide()
    {
        return enablePageSlide && pageTrack != null && gridLayoutTemplate != null;
    }

    private void BindPagePanel(GameObject panel, int pageIndex)
    {
        int startIndex = pageIndex * itemsPerPage;
        int slotCount = Mathf.Min(itemsPerPage, Mathf.Max(0, allTracks.Count - startIndex));
        int panelIndex = System.Array.IndexOf(pagePanels, panel);
        int slotOffset = panelIndex * itemsPerPage;

        currentSlots.Clear();

        for (int i = 0; i < itemsPerPage; i++)
        {
            int pooledIndex = slotOffset + i;
            if (pooledIndex >= pooledSlots.Count)
                break;

            var entry = pooledSlots[pooledIndex];

            if (i < slotCount)
            {
                var data = allTracks[startIndex + i];
                entry.root.SetActive(true);
                entry.slot.Setup(data, this);
                currentSlots.Add(entry.root);
            }
            else
            {
                entry.root.SetActive(false);
                entry.slot.Setup(null, this);
            }
        }
    }

    private void PopulateSlotsIntoParent(Transform parent, int pageIndex, List<GameObject> slotList)
    {
        int startIndex = pageIndex * itemsPerPage;
        int endIndex = Mathf.Min(startIndex + itemsPerPage, allTracks.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            var data = allTracks[i];
            var slotGO = Instantiate(slotPrefab, parent);
            var slot = slotGO.GetComponentInChildren<MusicSlot>(true);
            if (slot == null)
            {
                Debug.LogError("[MusicPageView] slotPrefab 上未找到 MusicSlot 组件。", slotPrefab);
                Destroy(slotGO);
                continue;
            }

            slot.Setup(data, this);
            slotList.Add(slotGO);
        }
    }

    private GameObject CreateEmptyPagePanel(string panelName)
    {
        var panelGO = new GameObject(panelName, typeof(RectTransform));
        panelGO.transform.SetParent(pageTrack, false);

        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 0.5f);
        panelRect.sizeDelta = new Vector2(GetPageWidth(), 0f);
        panelRect.anchoredPosition = Vector2.zero;

        var grid = panelGO.AddComponent<GridLayoutGroup>();
        CopyGridLayoutSettings(gridLayoutTemplate, grid);
        return panelGO;
    }

    private void CachePageWidth()
    {
        if (pageViewport == null)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(pageViewport);
        cachedPageWidth = pageViewport.rect.width;

        for (int i = 0; i < pagePanels.Length; i++)
        {
            if (pagePanels[i] == null)
                continue;

            var panelRect = pagePanels[i].GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(cachedPageWidth, 0f);
        }
    }

    private float GetPageWidth()
    {
        if (cachedPageWidth > 0f)
            return cachedPageWidth;

        if (pageViewport == null)
            return 0f;

        CachePageWidth();
        return cachedPageWidth;
    }

    private static void StretchRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
    }

    private static void CopyGridLayoutSettings(GridLayoutGroup from, GridLayoutGroup to)
    {
        to.padding = from.padding;
        to.cellSize = from.cellSize;
        to.spacing = from.spacing;
        to.startCorner = from.startCorner;
        to.startAxis = from.startAxis;
        to.childAlignment = from.childAlignment;
        to.constraint = from.constraint;
        to.constraintCount = from.constraintCount;
    }

    private void StartPageTransition(int pageIndex, int direction)
    {
        if (pageTransitionCoroutine != null)
            StopCoroutine(pageTransitionCoroutine);

        pageTransitionCoroutine = StartCoroutine(SlideToPage(pageIndex, direction));
    }

    private IEnumerator SlideToPage(int pageIndex, int direction)
    {
        isPageTransitioning = true;
        ClearSlotHighlight();
        SetPageButtonsInteractable(false);

        float width = GetPageWidth();
        var outgoingPanel = pagePanels[activePanelIndex];
        int incomingPanelIndex = 1 - activePanelIndex;
        var incomingPanel = pagePanels[incomingPanelIndex];

        BindPagePanel(incomingPanel, pageIndex);
        incomingPanel.SetActive(true);

        var outgoingRect = outgoingPanel.GetComponent<RectTransform>();
        var incomingRect = incomingPanel.GetComponent<RectTransform>();
        ResetPanelPosition(outgoingPanel);
        incomingRect.anchoredPosition = new Vector2(direction * width, 0f);

        var outgoingGroup = GetOrAddCanvasGroup(outgoingPanel);
        var incomingGroup = GetOrAddCanvasGroup(incomingPanel);
        outgoingGroup.blocksRaycasts = false;
        incomingGroup.blocksRaycasts = false;

        float elapsed = 0f;
        while (elapsed < pageSlideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = pageSlideDuration > 0f ? Mathf.Clamp01(elapsed / pageSlideDuration) : 1f;
            float eased = Mathf.SmoothStep(0f, 1f, t);

            outgoingRect.anchoredPosition = new Vector2(-direction * width * eased, 0f);
            incomingRect.anchoredPosition = new Vector2(direction * width * (1f - eased), 0f);
            yield return null;
        }

        outgoingRect.anchoredPosition = new Vector2(-direction * width, 0f);
        incomingRect.anchoredPosition = Vector2.zero;

        outgoingPanel.SetActive(false);
        ResetPanelPosition(outgoingPanel);
        incomingGroup.blocksRaycasts = true;

        activePanelIndex = incomingPanelIndex;
        currentSlots.Clear();
        CollectSlotsFromPanel(incomingPanel);
        currentPage = pageIndex;
        RestoreSelectionHighlight();
        isPageTransitioning = false;
        pageTransitionCoroutine = null;
        UpdatePageControls();
    }

    private void CollectSlotsFromPanel(GameObject panel)
    {
        int panelIndex = System.Array.IndexOf(pagePanels, panel);
        int slotOffset = panelIndex * itemsPerPage;

        for (int i = 0; i < itemsPerPage; i++)
        {
            int pooledIndex = slotOffset + i;
            if (pooledIndex >= pooledSlots.Count)
                break;

            var root = pooledSlots[pooledIndex].root;
            if (root.activeSelf)
                currentSlots.Add(root);
        }
    }

    private static void ResetPanelPosition(GameObject panel)
    {
        panel.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        var group = target.GetComponent<CanvasGroup>();
        if (group == null)
            group = target.AddComponent<CanvasGroup>();
        return group;
    }

    private void SetPageButtonsInteractable(bool interactable)
    {
        if (prevButton != null)
            prevButton.interactable = interactable && currentPage > 0;
        if (nextButton != null)
            nextButton.interactable = interactable && currentPage < totalPages - 1;
    }

    private void UpdatePageControls()
    {
        if (pageText != null)
            pageText.text = $"{currentPage + 1}/{totalPages}";

        if (isPageTransitioning)
            return;

        SetPageButtonsInteractable(true);
    }

    private void PrevPage()
    {
        if (isPageTransitioning || currentPage <= 0)
            return;

        if (UsesPageSlide())
            StartPageTransition(currentPage - 1, -1);
        else
            ShowPageImmediate(currentPage - 1);
    }

    private void NextPage()
    {
        if (isPageTransitioning || currentPage >= totalPages - 1)
            return;

        if (UsesPageSlide())
            StartPageTransition(currentPage + 1, 1);
        else
            ShowPageImmediate(currentPage + 1);
    }

    private void ClearSlotHighlight()
    {
        if (selectedSlot == null)
            return;

        selectedSlot.SetSelected(false);
        selectedSlot = null;
    }

    private void RestoreSelectionHighlight()
    {
        if (selectedTrack == null)
            return;

        foreach (var slotRoot in currentSlots)
        {
            var slot = slotRoot.GetComponentInChildren<MusicSlot>(true);
            if (slot == null || slot.trackData != selectedTrack)
                continue;

            selectedSlot = slot;
            selectedSlot.SetSelected(true);
            return;
        }

        selectedSlot = null;
    }

    public void SelectTrack(MusicSlot slot, MusicTrackItem track, bool playImmediate = false)
    {
        if (track == null)
            return;

        if (selectedSlot != null && selectedSlot != slot)
            selectedSlot.SetSelected(false);

        selectedTrack = track;
        selectedSlot = slot;
        if (selectedSlot != null)
            selectedSlot.SetSelected(true);

        UpdateCentralDisplay(track);
        UpdateDetailDisplay(track);
        SwitchBackground(track.background);
        PlayTrack(track, playImmediate);
    }

    private void UpdateDetailDisplay(MusicTrackItem track)
    {
        if (!string.IsNullOrEmpty(track.displayName))
            ApplyTextWithTyper(nameDisplay, track.displayName);
        else
            ClearText(nameDisplay);

        if (!string.IsNullOrEmpty(track.description))
            ApplyTextWithTyper(descDisplay, track.description);
        else
            ClearText(descDisplay);
    }

    private void UpdateCentralDisplay(MusicTrackItem track)
    {
        if (centralCoverImage != null)
        {
            centralCoverImage.sprite = track.coverLarge;
            centralCoverImage.color = Color.white;
        }

        if (songNameText != null)
            songNameText.text = GetLocalizedDisplayName(track.displayName);
    }

    private string GetLocalizedDisplayName(string termKey)
    {
        var localize = songNameText != null ? songNameText.GetComponent<Localize>() : null;
        return TextTyper.ResolveLocalized(termKey, localize);
    }

    private static void ApplyTextWithTyper(TMP_Text textComponent, string termKey)
    {
        if (textComponent == null)
            return;

        var typer = textComponent.GetComponent<TextTyper>();
        if (typer != null)
            typer.ShowText(termKey);
        else
            textComponent.text = TextTyper.ResolveLocalized(termKey, textComponent.GetComponent<Localize>());
    }

    private static void ClearText(TMP_Text textComponent)
    {
        if (textComponent == null)
            return;

        textComponent.text = string.Empty;
        textComponent.maxVisibleCharacters = int.MaxValue;
    }

    private void SwitchBackground(Sprite newSprite)
    {
        if (backgroundImage == null)
            return;

        if (newSprite == currentBackgroundSprite)
            return;

        if (backgroundFadeCoroutine != null)
            StopCoroutine(backgroundFadeCoroutine);

        backgroundFadeCoroutine = StartCoroutine(CrossfadeBackground(newSprite));
    }

    private IEnumerator CrossfadeBackground(Sprite newSprite)
    {
        currentBackgroundSprite = newSprite;

        if (backgroundCrossfadeImage != null)
        {
            Image from = backgroundUsingFrontLayer ? backgroundImage : backgroundCrossfadeImage;
            Image to = backgroundUsingFrontLayer ? backgroundCrossfadeImage : backgroundImage;

            to.sprite = newSprite;
            SetImageAlpha(to, 0f);

            if (newSprite == null)
            {
                yield return FadeImageAlpha(from, GetImageAlpha(from), 0f, fadeDuration);
                backgroundFadeCoroutine = null;
                yield break;
            }

            float fromStart = GetImageAlpha(from);
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                SetImageAlpha(from, Mathf.Lerp(fromStart, 0f, t));
                SetImageAlpha(to, Mathf.Lerp(0f, backgroundMaxAlpha, t));
                yield return null;
            }

            SetImageAlpha(from, 0f);
            SetImageAlpha(to, backgroundMaxAlpha);
            backgroundUsingFrontLayer = !backgroundUsingFrontLayer;
        }
        else
        {
            float halfDuration = fadeDuration * 0.5f;
            float currentAlpha = GetImageAlpha(backgroundImage);

            if (currentAlpha > 0f)
                yield return FadeImageAlpha(backgroundImage, currentAlpha, 0f, halfDuration);

            backgroundImage.sprite = newSprite;

            if (newSprite == null)
            {
                backgroundFadeCoroutine = null;
                yield break;
            }

            yield return FadeImageAlpha(backgroundImage, 0f, backgroundMaxAlpha, halfDuration);
        }

        backgroundFadeCoroutine = null;
    }

    private static float GetImageAlpha(Image image)
    {
        return image != null ? image.color.a : 0f;
    }

    private static void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
            return;

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private static IEnumerator FadeImageAlpha(Image image, float from, float to, float duration)
    {
        if (image == null)
            yield break;

        if (duration <= 0f)
        {
            SetImageAlpha(image, to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetImageAlpha(image, Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetImageAlpha(image, to);
    }

    private void PlayTrack(MusicTrackItem track, bool playImmediate = false)
    {
        if (bgmAudioSource == null)
        {
            Debug.LogWarning("[MusicPageView] 未绑定 BGM AudioSource。");
            return;
        }

        if (track.audioClip == null)
        {
            Debug.LogWarning($"[MusicPageView] 曲目 {track.id} 未绑定音频。");
            return;
        }

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        if (playImmediate && !bgmAudioSource.isPlaying)
        {
            bgmAudioSource.clip = track.audioClip;
            bgmAudioSource.volume = volume;
            bgmAudioSource.Play();
            return;
        }

        fadeCoroutine = StartCoroutine(FadeSwitchBgm(track.audioClip));
    }

    private IEnumerator FadeSwitchBgm(AudioClip clip)
    {
        if (bgmAudioSource.isPlaying)
        {
            yield return FadeVolume(bgmAudioSource, bgmAudioSource.volume, 0f, fadeDuration);
            bgmAudioSource.Stop();
        }

        bgmAudioSource.clip = clip;
        bgmAudioSource.volume = 0f;
        bgmAudioSource.Play();

        yield return FadeVolume(bgmAudioSource, 0f, volume, fadeDuration);
        fadeCoroutine = null;
    }

    private static IEnumerator FadeVolume(AudioSource source, float from, float to, float duration)
    {
        if (source == null)
            yield break;

        if (duration <= 0f)
        {
            source.volume = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            source.volume = Mathf.Lerp(from, to, t);
            yield return null;
        }

        source.volume = to;
    }
}
