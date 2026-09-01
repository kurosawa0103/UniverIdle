using System.Collections;
using I2.Loc;
using TMPro;
using UnityEngine;

/// <summary>
/// I2 词条 + TMP 逐字显示。挂在与 <see cref="TextMeshProUGUI"/> 同一物体上。
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public sealed class TextTyper : MonoBehaviour
{
    [Min(1f)]
    [SerializeField]
    private float charactersPerSecond = 48f;

    private TextMeshProUGUI _text;
    private Coroutine _typing;

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
    }

    public void ShowText(string termKey)
    {
        if (_text == null)
            return;

        if (_typing != null)
        {
            StopCoroutine(_typing);
            _typing = null;
        }

        string content = ResolveLocalized(termKey, _text != null ? _text.GetComponent<Localize>() : null);
        _text.text = content;
        _typing = StartCoroutine(TypeRoutine(content));
    }

    public static string ResolveLocalized(string termKey, Localize localize = null)
    {
        if (string.IsNullOrEmpty(termKey))
            return string.Empty;

        if (localize != null)
        {
            localize.SetTerm(termKey);
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

    public static string FixSpecialPlaceholders(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return input.Replace("{c}", ",");
    }

    private IEnumerator TypeRoutine(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            _text.maxVisibleCharacters = 0;
            _typing = null;
            yield break;
        }

        _text.ForceMeshUpdate();
        _text.maxVisibleCharacters = 0;

        float delay = 1f / Mathf.Max(1f, charactersPerSecond);
        int visible = 0;
        int total = content.Length;

        while (visible < total)
        {
            visible++;
            _text.maxVisibleCharacters = visible;
            yield return new WaitForSeconds(delay);
        }

        _typing = null;
    }
}
