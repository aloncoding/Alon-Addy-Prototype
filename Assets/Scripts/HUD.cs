using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds and updates the whole HUD (GDD section 8.1): timer + health (top center), XP bar above the human,
/// power-up list (left/right sides), human ability tiles (bottom-left), ghost ability tiles (bottom-right),
/// floating damage numbers, and the game-over banner.
/// </summary>
public class HUD : MonoBehaviour
{
    public static HUD Instance { get; private set; }

    [System.Serializable]
    class CooldownTile
    {
        public Image overlay;
        public Text cdText;
        public void Set(float fraction, float remaining)
        {
            overlay.fillAmount = fraction;
            cdText.text = remaining > 0.05f ? Mathf.CeilToInt(remaining).ToString() : "";
        }
    }

    [System.Serializable]
    class PowerRow
    {
        public GameObject root;
        public Text label;
        public Image icon;
    }

    class Popup
    {
        public Text text;
        public Vector3 worldPos;
        public Color color;
        public float age, life;
    }
    [SerializeField] RectTransform powerStrip;
    const float StripWidth = 1800f;   // total width the power-ups spread across
    const float MaxRowWidth = 380f;   // a single power-up never gets wider than this
    const float RowHeight = 64f;
    const float RowGap = 16f;

    [SerializeField] Canvas canvas;
    [SerializeField] RectTransform canvasRect;
    [SerializeField] Text bestText;
    [SerializeField] Text timerText, healthText, humanStatus, ghostStatus, gameOverText;
    [SerializeField] Image healthFill;
    [SerializeField] CooldownTile meleeTile, dashTile, scareTile, tauntTile;
    [SerializeField] PowerRow[] rows;
    [SerializeField, HideInInspector] bool built;

    readonly List<Popup> popups = new List<Popup>();
    readonly Stack<Text> popupPool = new Stack<Text>();
    [SerializeField] Transform popupParent;

    [SerializeField] Transform xpBarRoot, xpFill;
    const float XpBarWidth = 1.6f;

   void Awake()
    {
        Instance = this;

        // Always rebuild from code so layout changes in Build() show up,
        // even if this HUD came from an old baked prefab.
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        Build();
        built = true;
    }

    /// <summary>Used by the editor menu to bake the HUD hierarchy into an editable prefab.</summary>
    public void BuildInEditor() { Build(); built = true; }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // ---------------- Construction ----------------
    void Build()
    {
        rows = new PowerRow[UpgradeSystem.TypeCount];
        canvas = UIBuilder.CreateCanvas(transform, "HUDCanvas", 10);
        canvasRect = (RectTransform)canvas.transform;
        Transform c = canvas.transform;
        Vector2 topC = new Vector2(0.5f, 1f);
        Color white = Color.white;

        timerText = UIBuilder.Label(c, "Timer", "00:00", 46, white, TextAnchor.UpperCenter, topC, topC, new Vector2(0f, -12f), new Vector2(300f, 60f));
        UIBuilder.AddOutline(timerText);

        BuildBestText(c);

        powerStrip = UIBuilder.Rect(c, "PowerStrip", topC, topC, new Vector2(0f, -112f), new Vector2(StripWidth, RowHeight));

        for (int i = 0; i < UpgradeSystem.TypeCount; i++)
        {
            var type = (UpgradeType)i;
            Vector2 leftMid = new Vector2(0f, 0.5f);

            // Row background (position/width are set every frame in Update)
            var bgImg = UIBuilder.Img(powerStrip, "Row_" + type, leftMid, new Vector2(0.5f, 0.5f), Vector2.zero,
                                    new Vector2(MaxRowWidth, RowHeight), new Color(0f, 0f, 0f, 0.45f));
            var rt = bgImg.rectTransform;
            var row = new PowerRow { root = rt.gameObject };

            row.icon = UIBuilder.Img(rt, "Icon", leftMid, leftMid, new Vector2(8f, 0f), new Vector2(44f, 44f), UpgradeSystem.ColorOf(type));

            row.label = UIBuilder.Label(rt, "Label", "", 20, white, TextAnchor.MiddleLeft, leftMid, leftMid, Vector2.zero, Vector2.zero);
            var lrt = row.label.rectTransform;          // stretch label to fill the rest of the row
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(60f, 2f);
            lrt.offsetMax = new Vector2(-6f, -2f);
            row.label.verticalOverflow = VerticalWrapMode.Truncate;
            row.label.resizeTextForBestFit = true;      // shrinks text when 6 power-ups are crammed in
            row.label.resizeTextMinSize = 12;
            row.label.resizeTextMaxSize = 20;
            UIBuilder.AddOutline(row.label);

            row.root.SetActive(false);
            rows[i] = row;
        }

        // ---- Bottom center: health bar ----
        Vector2 botC = new Vector2(0.5f, 0f);
        var hpBg = UIBuilder.Img(c, "HealthBg", botC, botC, new Vector2(0f, 28f), new Vector2(520f, 34f), new Color(0f, 0f, 0f, 0.6f));
        healthFill = UIBuilder.Img(hpBg.transform, "HealthFill", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(3f, 0f), new Vector2(514f, 28f), new Color(0.9f, 0.25f, 0.3f));
        healthFill.type = Image.Type.Filled;
        healthFill.fillMethod = Image.FillMethod.Horizontal;
        healthText = UIBuilder.Label(hpBg.transform, "HealthText", "", 20, white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 34f));
         
                // Bottom-left: human
        Vector2 bl = new Vector2(0f, 0f);
        humanStatus = UIBuilder.Label(c, "HumanStatus", "", 22, white, TextAnchor.LowerLeft, bl, bl, new Vector2(24f, 118f), new Vector2(520f, 60f));
        UIBuilder.AddOutline(humanStatus);
        meleeTile = MakeTile(c, "Melee", "MELEE\n[Space]", new Color(0.9f, 0.5f, 0.3f), bl, new Vector2(24f, 24f));
        dashTile = MakeTile(c, "Dash", "DASH\n[Shift]", new Color(0.4f, 0.8f, 1f), bl, new Vector2(120f, 24f));

        // Bottom-right: ghost
        Vector2 br = new Vector2(1f, 0f);
        ghostStatus = UIBuilder.Label(c, "GhostStatus", "", 22, white, TextAnchor.LowerRight, br, br, new Vector2(-24f, 118f), new Vector2(520f, 60f));
        UIBuilder.AddOutline(ghostStatus);
        tauntTile = MakeTile(c, "Taunt", "TAUNT\n[R-Click]", new Color(1f, 0.6f, 0.2f), br, new Vector2(-24f, 24f));
        scareTile = MakeTile(c, "Scare", "SCARE\n[L-Click]", new Color(0.5f, 0.9f, 1f), br, new Vector2(-120f, 24f));

        gameOverText = UIBuilder.Label(c, "GameOver", "", 64, white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 400f));
        UIBuilder.AddOutline(gameOverText);
        gameOverText.gameObject.SetActive(false);

        popupParent = UIBuilder.Rect(c, "Popups", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        // World-space XP bar above the human (rescaled with zoom so it stays the same size on screen).
        var root = new GameObject("XpBar");
        root.transform.SetParent(transform, false);
        xpBarRoot = root.transform;
        var bg = new GameObject("Bg");
        bg.transform.SetParent(xpBarRoot, false);
        bg.transform.localScale = new Vector3(XpBarWidth + 0.06f, 0.2f, 1f);
        ProceduralAssets.AddSprite(bg, ProceduralAssets.Square, new Color(0f, 0f, 0f, 0.65f), 15);
        var fill = new GameObject("Fill");
        fill.transform.SetParent(xpBarRoot, false);
        fill.transform.localPosition = new Vector3(-XpBarWidth * 0.5f, 0f, 0f);
        fill.transform.localScale = new Vector3(0f, 0.14f, 1f);
        ProceduralAssets.AddSprite(fill, ProceduralAssets.SquareLeft, new Color(0.4f, 1f, 0.5f), 16);
        xpFill = fill.transform;
    }

    CooldownTile MakeTile(Transform parent, string name, string label, Color color, Vector2 anchor, Vector2 pos)
    {
        var bg = UIBuilder.Img(parent, name, anchor, anchor, pos, new Vector2(88f, 88f), color);
        var overlay = UIBuilder.Img(bg.transform, "Cooldown", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(88f, 88f), new Color(0f, 0f, 0f, 0.7f));
        overlay.type = Image.Type.Filled;
        overlay.fillMethod = Image.FillMethod.Vertical;
        overlay.fillOrigin = (int)Image.OriginVertical.Top;
        overlay.fillAmount = 0f;
        var lbl = UIBuilder.Label(bg.transform, "Label", label, 15, Color.white, TextAnchor.LowerCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(88f, 44f));
        UIBuilder.AddOutline(lbl);
        var cd = UIBuilder.Label(bg.transform, "Seconds", "", 34, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(88f, 44f));
        UIBuilder.AddOutline(cd);
        return new CooldownTile { overlay = overlay, cdText = cd };
    }

    // ---------------- Per-frame update ----------------
    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.HumanPlayer == null || gm.GhostPlayer == null || UpgradeSystem.Instance == null) return;
        var human = gm.HumanPlayer;
        var ghost = gm.GhostPlayer;
        var up = UpgradeSystem.Instance;

        int total = Mathf.FloorToInt(gm.ElapsedTime);
        timerText.text = $"{total / 60:00}:{total % 60:00}";
        healthFill.fillAmount = human.MaxHealth > 0f ? human.Health / human.MaxHealth : 0f;
        healthText.text = $"{Mathf.CeilToInt(human.Health)} / {Mathf.CeilToInt(human.MaxHealth)}";
        bool beating = gm.BestTime > 0f && gm.ElapsedTime > gm.BestTime;
        bestText.text = beating ? $"NEW BEST! {FormatTime(gm.ElapsedTime)}" : $"BEST {FormatTime(gm.BestTime)}";

        int active = 0;
        for (int i = 0; i < rows.Length; i++) if (up.Levels[i] > 0) active++;

        float slot = active > 0 ? StripWidth / active : 0f;
        float width = Mathf.Min(MaxRowWidth, slot - RowGap);
        int k = 0;

        for (int i = 0; i < rows.Length; i++)
        {
            int lv = up.Levels[i];
            bool on = lv > 0;
            rows[i].root.SetActive(on);
            if (!on) continue;

            var rt = (RectTransform)rows[i].root.transform;
            rt.anchoredPosition = new Vector2(slot * (k + 0.5f), 0f);  // centered in its slot
            rt.sizeDelta = new Vector2(width, RowHeight);
            k++;

            var t = (UpgradeType)i;
            rows[i].label.text = $"{UpgradeSystem.NameOf(t)}  Lv {lv}\n{up.StatusText(t)}";
        }

        meleeTile.Set(human.Melee.Fraction, human.Melee.Remaining);
        dashTile.Set(human.Dash.Fraction, human.Dash.Remaining);
        scareTile.Set(ghost.Scare.Fraction, ghost.Scare.Remaining);
        tauntTile.Set(ghost.Taunt.Fraction, ghost.Taunt.Remaining);

        string boost = up.BoostActive ? $"ATTACK BOOST {Mathf.CeilToInt(up.BoostTimeLeft)}s" : "";
        humanStatus.text = StatusLine("Hold Space", human.IsCharged, human.ChargeProgress, up) + (boost != "" ? "\n" + boost : "");
        ghostStatus.text = StatusLine("Hold L-Click", ghost.IsCharged, ghost.ChargeProgress, up) + (boost != "" ? "\n" + boost : "");

        UpdateXpBar(gm, human);
        UpdatePopups();
    }

    string StatusLine(string hint, bool charged, float progress, UpgradeSystem up)
    {
        if (!up.ChargedUnlocked) return "";
        if (charged) return "CHARGED - release to fire!";
        if (progress > 0f) return $"Charging {progress * 100f:0}%";
        return $"Charged shot: {hint}";
    }

    void UpdateXpBar(GameManager gm, Human human)
    {
        var dc = DynamicCamera.Instance;
        float zs = dc != null ? dc.ZoomScale : 1f;
        xpBarRoot.position = human.transform.position + Vector3.up * (0.95f * zs);
        xpBarRoot.localScale = Vector3.one * zs;
        xpFill.localScale = new Vector3(XpBarWidth * gm.XpProgress, 0.14f, 1f);
    }

    // ---------------- Floating damage numbers ----------------
    public void AddPopup(Vector3 worldPos, string text, Color color, float life = 0.7f)
    {
        if (popups.Count >= 45) return;
        Text t;
        if (popupPool.Count > 0) t = popupPool.Pop();
        else
        {
            var rt = UIBuilder.Rect(popupParent, "Popup", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 40f));
            t = rt.gameObject.AddComponent<Text>();
            t.font = UIBuilder.DefaultFont;
            t.fontSize = 28;
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIBuilder.AddOutline(t);
        }
        t.text = text;
        t.color = color;
        t.gameObject.SetActive(true);
        popups.Add(new Popup { text = t, worldPos = worldPos, color = color, life = life });
    }

    void UpdatePopups()
    {
        var dc = DynamicCamera.Instance;
        if (dc == null) return;
        for (int i = popups.Count - 1; i >= 0; i--)
        {
            var p = popups[i];
            p.age += Time.deltaTime;
            if (p.age >= p.life)
            {
                p.text.gameObject.SetActive(false);
                popupPool.Push(p.text);
                popups.RemoveAt(i);
                continue;
            }
            Vector3 sp = dc.Cam.WorldToScreenPoint(p.worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, null, out Vector2 local);
            p.text.rectTransform.anchoredPosition = local + new Vector2(0f, p.age * 70f);
            var c = p.color;
            c.a = 1f - p.age / p.life;
            p.text.color = c;
        }
    }

    public void ShowGameOver(float survivedSeconds, bool newBest)
    {
        var gm = GameManager.Instance;
        string bestLine = newBest
            ? "<color=#FFD94D>NEW BEST!</color>"
            : $"Best {FormatTime(gm != null ? gm.BestTime : 0f)}";

        gameOverText.text = $"YOU DIED\nSurvived {FormatTime(survivedSeconds)}\n<size=36>{bestLine}</size>\n<size=32>Restarting...  (or press R)</size>";
        gameOverText.supportRichText = true;
        gameOverText.gameObject.SetActive(true);
    }
    static string FormatTime(float seconds)
    {
        int s = Mathf.FloorToInt(seconds);
        return $"{s / 60:00}:{s % 60:00}";
    }

    void BuildBestText(Transform c)
    {
        Vector2 topC = new Vector2(0.5f, 1f);
        bestText = UIBuilder.Label(c, "BestTime", "", 24, new Color(1f, 0.85f, 0.3f), TextAnchor.UpperCenter,
                                topC, topC, new Vector2(0f, -68f), new Vector2(400f, 40f));
        UIBuilder.AddOutline(bestText);
    }
}
