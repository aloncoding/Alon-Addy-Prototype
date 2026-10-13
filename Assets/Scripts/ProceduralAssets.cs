using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates placeholder sprites and sound effects in code. The editor menu "Human & Ghost" exports these
/// sprites to PNG files and registers them here so generated prefabs reference real, editable assets.
/// </summary>
public static class ProceduralAssets
{
    static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    static Material unlit;

    public static Material UnlitMaterial
    {
        get
        {
            if (unlit == null)
            {
                var sh = Shader.Find("Sprites/Default");
                if (sh != null) unlit = new Material(sh);
            }
            return unlit;
        }
        set { unlit = value; }
    }

    /// <summary>Adds a SpriteRenderer with an unlit material (works in Built-in, URP and URP 2D without lights).</summary>
    public static SpriteRenderer AddSprite(GameObject go, Sprite sprite, Color color, int sortingOrder)
    {
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        var m = UnlitMaterial;
        if (m != null) sr.sharedMaterial = m;
        return sr;
    }

    // ---------- Sprite registry ----------
    public static void ResetCache() { sprites.Clear(); }
    public static void Register(string key, Sprite s) { sprites[key] = s; }

    public static Sprite Get(string key)
    {
        switch (key)
        {
            case "square": return Square;
            case "squareLeft": return SquareLeft;
            case "circle": return Circle;
            case "blob": return Blob;
            case "ring": return Ring;
            case "ghost": return Ghost;
            case "tile": return Tile;
            default: return Macro;
        }
    }

    static Sprite Cached(string key, Func<Sprite> make)
    {
        if (sprites.TryGetValue(key, out var s) && s != null) return s;
        s = make();
        sprites[key] = s;
        return s;
    }

    public static Sprite Square => Cached("square", () => MakeSquare(new Vector2(0.5f, 0.5f)));
    /// <summary>1x1 unit square whose pivot is the LEFT edge (for bars that fill left-to-right).</summary>
    public static Sprite SquareLeft => Cached("squareLeft", () => MakeSquare(new Vector2(0f, 0.5f)));
    public static Sprite Circle => Cached("circle", () => Disc(64, false));
    public static Sprite Blob => Cached("blob", () => Disc(64, true));
    public static Sprite Ring => Cached("ring", MakeRing);
    public static Sprite Ghost => Cached("ghost", MakeGhost);
    public static Sprite Tile => Cached("tile", MakeTile);
    public static Sprite Macro => Cached("macro", MakeMacro);

    static Sprite MakeSquare(Vector2 pivot)
    {
        var t = Tex(4, 4);
        var px = new Color[16];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        t.SetPixels(px);
        return Make(t, 4f, pivot);
    }

    static Texture2D Tex(int w, int h)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        t.wrapMode = TextureWrapMode.Clamp;
        return t;
    }

    static Sprite Make(Texture2D t, float ppu, Vector2 pivot)
    {
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, t.width, t.height), pivot, ppu, 0, SpriteMeshType.FullRect);
    }

    static bool Hit(float u, float v, float cx, float cy, float r)
    {
        float dx = u - cx, dy = v - cy;
        return dx * dx + dy * dy <= r * r;
    }

    static Sprite Disc(int n, bool face)
    {
        var t = Tex(n, n);
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n * 2f - 1f;
                float v = (y + 0.5f) / n * 2f - 1f;
                float d = Mathf.Sqrt(u * u + v * v);
                float a = Mathf.Clamp01((1f - d) * n * 0.5f);
                var c = new Color(1f, 1f, 1f, a);
                if (face)
                {
                    if (d > 0.82f) { c.r = 0.72f; c.g = 0.72f; c.b = 0.72f; }
                    if (Hit(u, v, -0.3f, 0.2f, 0.13f) || Hit(u, v, 0.3f, 0.2f, 0.13f)) { c.r = 0.05f; c.g = 0.05f; c.b = 0.05f; }
                }
                px[y * n + x] = c;
            }
        t.SetPixels(px);
        return Make(t, n, new Vector2(0.5f, 0.5f));
    }

    static Sprite MakeRing()
    {
        int n = 128;
        var t = Tex(n, n);
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n * 2f - 1f;
                float v = (y + 0.5f) / n * 2f - 1f;
                float d = Mathf.Sqrt(u * u + v * v);
                float a = Mathf.Clamp01((1f - d) * n * 0.5f) * Mathf.Clamp01((d - 0.88f) * n * 0.5f);
                px[y * n + x] = new Color(1f, 1f, 1f, a);
            }
        t.SetPixels(px);
        return Make(t, n, new Vector2(0.5f, 0.5f));
    }

    static Sprite MakeGhost()
    {
        int n = 64;
        var t = Tex(n, n);
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n * 2f - 1f;
                float v = (y + 0.5f) / n * 2f - 1f;
                bool inside;
                if (v >= 0.05f) inside = u * u + (v - 0.05f) * (v - 0.05f) <= 0.75f * 0.75f;
                else inside = Mathf.Abs(u) <= 0.75f && v >= -0.65f + 0.1f * Mathf.Sin(u * 9f);
                var c = inside ? Color.white : new Color(1f, 1f, 1f, 0f);
                if (inside && (Hit(u, v, -0.25f, 0.2f, 0.12f) || Hit(u, v, 0.25f, 0.2f, 0.12f) || Hit(u, v, 0f, -0.15f, 0.1f)))
                    c = new Color(0.1f, 0.1f, 0.15f, 1f);
                px[y * n + x] = c;
            }
        t.SetPixels(px);
        return Make(t, n, new Vector2(0.5f, 0.5f));
    }

    // ---------- Background ----------
    static void Put(Color[] px, int n, int x, int y, Color c)
    {
        x = ((x % n) + n) % n;
        y = ((y % n) + n) % n;
        px[y * n + x] = c;
    }

    /// <summary>128px tile @32ppu = 4x4 world units: shaded quadrants, grid lines, dots, crosses, pebbles and grass tufts.</summary>
    static Sprite MakeTile()
    {
        int n = 128;
        var t = Tex(n, n);
        t.wrapMode = TextureWrapMode.Repeat;
        var px = new Color[n * n];
        var shadeA = new Color(0.11f, 0.125f, 0.17f, 1f);
        var shadeB = new Color(0.14f, 0.155f, 0.21f, 1f);
        var line = new Color(0.22f, 0.25f, 0.34f, 1f);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                bool alt = ((x / 64) + (y / 64)) % 2 == 0;
                px[y * n + x] = alt ? shadeA : shadeB;
                if (x % 64 < 2 || y % 64 < 2) px[y * n + x] = line;
            }

        var rng = new System.Random(4242);
        var dotC = new Color(0.40f, 0.45f, 0.62f, 1f);
        var crossC = new Color(0.32f, 0.37f, 0.55f, 1f);
        var pebC = new Color(0.24f, 0.27f, 0.38f, 1f);
        var pebHi = new Color(0.36f, 0.40f, 0.54f, 1f);
        var tuftC = new Color(0.22f, 0.48f, 0.34f, 1f);

        for (int i = 0; i < 12; i++)
        {
            int x = rng.Next(n), y = rng.Next(n);
            for (int dy = 0; dy < 3; dy++) for (int dx = 0; dx < 3; dx++) Put(px, n, x + dx, y + dy, dotC);
        }
        for (int i = 0; i < 7; i++)
        {
            int x = rng.Next(n), y = rng.Next(n);
            for (int k = -4; k <= 4; k++) { Put(px, n, x + k, y, crossC); Put(px, n, x, y + k, crossC); }
        }
        for (int i = 0; i < 7; i++)
        {
            int x = rng.Next(n), y = rng.Next(n);
            for (int dy = -5; dy <= 5; dy++)
                for (int dx = -5; dx <= 5; dx++)
                    if (dx * dx + dy * dy <= 25) Put(px, n, x + dx, y + dy, (dx + 2) * (dx + 2) + (dy - 2) * (dy - 2) <= 6 ? pebHi : pebC);
        }
        for (int i = 0; i < 10; i++)
        {
            int x = rng.Next(n), y = rng.Next(n);
            int[] heights = { 5, 8, 5 };
            for (int j = 0; j < 3; j++)
                for (int k = 0; k < heights[j]; k++) Put(px, n, x + (j - 1) * 3, y + k, tuftC);
        }
        t.SetPixels(px);
        return Make(t, 32f, new Vector2(0.5f, 0.5f));
    }

    static void Blot(Color[] px, int n, int cx, int cy, float r, Color c, bool ring)
    {
        int ir = (int)r + 3;
        for (int dy = -ir; dy <= ir; dy++)
            for (int dx = -ir; dx <= ir; dx++)
            {
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a;
                if (ring) a = Mathf.Clamp01(1f - Mathf.Abs(d - r) / 1.8f);
                else { if (d > r) continue; a = 1f - d / r; }
                float na = c.a * a;
                int x = ((cx + dx) % n + n) % n;
                int y = ((cy + dy) % n + n) % n;
                if (na > px[y * n + x].a) px[y * n + x] = new Color(c.r, c.g, c.b, na);
            }
    }

    /// <summary>256px tile @16ppu = 16x16 world units: large soft blotches and crater rings (macro landmarks).</summary>
    static Sprite MakeMacro()
    {
        int n = 256;
        var t = Tex(n, n);
        t.wrapMode = TextureWrapMode.Repeat;
        var px = new Color[n * n];
        for (int i = 0; i < px.Length; i++) px[i] = new Color(1f, 1f, 1f, 0f);
        var rng = new System.Random(99);
        for (int i = 0; i < 8; i++)
        {
            bool dark = i % 2 == 0;
            var c = dark ? new Color(0f, 0f, 0f, 0.30f) : new Color(0.5f, 0.6f, 0.95f, 0.10f);
            Blot(px, n, rng.Next(n), rng.Next(n), rng.Next(20, 46), c, false);
        }
        for (int i = 0; i < 5; i++)
            Blot(px, n, rng.Next(n), rng.Next(n), rng.Next(10, 28), new Color(0.55f, 0.65f, 1f, 0.28f), true);
        t.SetPixels(px);
        return Make(t, 16f, new Vector2(0.5f, 0.5f));
    }

    // ---------- Audio ----------
    /// <param name="wave">0 = sine, 1 = square, 2 = noise</param>
    public static AudioClip Tone(string key, float freqStart, float freqEnd, float duration, float volume, int wave)
    {
        if (clips.TryGetValue(key, out var existing) && existing != null) return existing;
        int rate = 44100;
        int n = Mathf.CeilToInt(rate * duration);
        var data = new float[n];
        var rng = new System.Random(key.Length * 7919);
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float k = (float)i / n;
            float f = Mathf.Lerp(freqStart, freqEnd, k);
            phase += f / rate;
            phase -= Mathf.Floor(phase);
            float s;
            if (wave == 0) s = Mathf.Sin(phase * Mathf.PI * 2f);
            else if (wave == 1) s = phase < 0.5f ? 0.6f : -0.6f;
            else s = (float)(rng.NextDouble() * 2.0 - 1.0);
            float attack = Mathf.Min(1f, i / (rate * 0.004f));
            data[i] = s * attack * Mathf.Pow(1f - k, 1.5f) * volume;
        }
        var clip = AudioClip.Create(key, n, 1, rate, false);
        clip.SetData(data, 0);
        clips[key] = clip;
        return clip;
    }
}