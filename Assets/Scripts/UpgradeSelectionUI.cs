using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Shared upgrade picker (GDD 6.3 / 8.2). Ghost: hover + Left Click. Human: A/D (or W/S) to browse, Space to confirm.
/// The first valid confirmation wins; the 'open' flag guarantees a single selection.
/// </summary>
public class UpgradeSelectionUI : MonoBehaviour
{
    [System.Serializable]
    class Card
    {
        public RectTransform rect;
        public Image bg, colorBar;
        public Text title, level, desc, next;
    }

    const float InputDelay = 0.35f; // ignore input briefly so a held key/click can't pick by accident

    [SerializeField] Card[] cards = new Card[2];
    [SerializeField] GameObject root;
    [SerializeField, HideInInspector] bool built;
    bool open;
    int index;
    UpgradeType[] options;
    System.Action<UpgradeType> onChosen;
    float openedAt;

    static readonly Color Normal = new Color(0.16f, 0.18f, 0.26f, 0.98f);
    static readonly Color Highlight = new Color(0.32f, 0.42f, 0.70f, 1f);

    void Awake()
    {
        if (!built) { Build(); built = true; }
        root.SetActive(false);
    }

    /// <summary>Used by the editor menu to bake the picker hierarchy into an editable prefab.</summary>
    public void BuildInEditor()
    {
        Build();
        built = true;
        root.SetActive(false);
    }

    void Build()
    {
        var canvas = UIBuilder.CreateCanvas(transform, "SelectionCanvas", 100);
        root = canvas.gameObject;
        Transform c = canvas.transform;

        var dim = UIBuilder.Img(c, "Dim", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.75f));
        UIBuilder.Stretch(dim.rectTransform);

        Vector2 topC = new Vector2(0.5f, 1f);
        var title = UIBuilder.Label(c, "Title", "LEVEL UP!  Choose a team upgrade", 56, Color.white, TextAnchor.UpperCenter, topC, topC, new Vector2(0f, -90f), new Vector2(1400f, 80f));
        UIBuilder.AddOutline(title);
        var hint = UIBuilder.Label(c, "Hint", "Human: A / D to browse, Space to confirm      Ghost: hover + Left Click", 28, new Color(0.8f, 0.85f, 1f), TextAnchor.UpperCenter, topC, topC, new Vector2(0f, -170f), new Vector2(1500f, 50f));
        UIBuilder.AddOutline(hint);

        for (int i = 0; i < 2; i++)
        {
            Vector2 mid = new Vector2(0.5f, 0.5f);
            var card = new Card();
            card.bg = UIBuilder.Img(c, "Card" + i, mid, mid, new Vector2(i == 0 ? -330f : 330f, -30f), new Vector2(580f, 470f), Normal);
            card.rect = card.bg.rectTransform;
            Transform t = card.bg.transform;
            Vector2 top = new Vector2(0.5f, 1f);
            card.colorBar = UIBuilder.Img(t, "Bar", top, top, Vector2.zero, new Vector2(580f, 14f), Color.white);
            card.title = UIBuilder.Label(t, "Name", "", 42, Color.white, TextAnchor.UpperCenter, top, top, new Vector2(0f, -34f), new Vector2(540f, 56f));
            card.level = UIBuilder.Label(t, "Level", "", 26, new Color(1f, 0.9f, 0.4f), TextAnchor.UpperCenter, top, top, new Vector2(0f, -96f), new Vector2(540f, 36f));
            card.desc = UIBuilder.Label(t, "Desc", "", 27, new Color(0.9f, 0.9f, 0.95f), TextAnchor.UpperCenter, top, top, new Vector2(0f, -150f), new Vector2(520f, 130f));
            card.next = UIBuilder.Label(t, "Next", "", 26, new Color(0.6f, 1f, 0.7f), TextAnchor.UpperCenter, top, top, new Vector2(0f, -300f), new Vector2(520f, 150f));
            cards[i] = card;
        }
    }

    public void Open(UpgradeType[] opts, System.Action<UpgradeType> callback)
    {
        options = opts;
        onChosen = callback;
        index = 0;
        openedAt = Time.unscaledTime;
        open = true;
        root.SetActive(true);

        var up = UpgradeSystem.Instance;
        for (int i = 0; i < 2; i++)
        {
            var t = options[i];
            int lv = up.Levels[(int)t];
            cards[i].title.text = UpgradeSystem.NameOf(t);
            cards[i].colorBar.color = UpgradeSystem.ColorOf(t);
            cards[i].level.text = lv > 0 ? $"Current: Level {lv}" : "NEW";
            cards[i].desc.text = UpgradeSystem.DescriptionOf(t);
            cards[i].next.text = lv > 0
                ? $"Now: {UpgradeSystem.EffectAt(t, lv)}\nNext (Lv {lv + 1}): {UpgradeSystem.EffectAt(t, lv + 1)}"
                : $"Level 1: {UpgradeSystem.EffectAt(t, 1)}";
        }
        RefreshHighlight();
    }

    void RefreshHighlight()
    {
        for (int i = 0; i < 2; i++) cards[i].bg.color = i == index ? Highlight : Normal;
    }

    void Update()
    {
        if (!open) return;
        if (Time.unscaledTime - openedAt < InputDelay) return;

        var kb = Keyboard.current;
        var mouse = Mouse.current;

        if (mouse != null)
        {
            Vector2 mp = mouse.position.ReadValue();
            int hovered = CardAt(mp);
            if (hovered >= 0 && mouse.delta.ReadValue().sqrMagnitude > 0f && hovered != index)
            {
                index = hovered;
                RefreshHighlight();
            }
            if (mouse.leftButton.wasPressedThisFrame && hovered >= 0) { Choose(hovered); return; }
        }

        if (kb != null)
        {
            bool prev = kb.aKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame;
            bool next = kb.dKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame;
            if (prev || next)
            {
                index = (index + 1) % 2; // two options: either direction toggles
                Sfx.Play(SfxId.Select, 0.5f, 0f);
                RefreshHighlight();
            }
            if (kb.spaceKey.wasPressedThisFrame) Choose(index);
        }
    }

    int CardAt(Vector2 screenPos)
    {
        for (int i = 0; i < 2; i++)
            if (RectTransformUtility.RectangleContainsScreenPoint(cards[i].rect, screenPos, null)) return i;
        return -1;
    }

    void Choose(int i)
    {
        if (!open) return;      // only the first valid selection counts
        open = false;
        root.SetActive(false);
        onChosen?.Invoke(options[i]);
    }
}