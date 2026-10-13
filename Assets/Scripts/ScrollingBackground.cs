using UnityEngine;

/// <summary>
/// Tiled background layers that snap to the camera in whole-tile steps, so the pattern looks fixed in the world.
/// Every SpriteRenderer on this object or its children is treated as a layer (the prefab has a small-detail layer
/// and a large "macro" layer with craters/blotches so travel is readable at every zoom level).
/// Sprites used here must have Mesh Type = Full Rect.
/// </summary>
public class ScrollingBackground : MonoBehaviour
{
    SpriteRenderer[] layers;
    float[] tileSizes;
    Transform cam;

    public void Init(DynamicCamera dc)
    {
        cam = dc.transform;
        var found = GetComponentsInChildren<SpriteRenderer>(true);
        if (found.Length == 0) found = BuildDefaultLayers();
        layers = found;
        tileSizes = new float[layers.Length];

        float halfH = dc.maxOrthographicSize + 2f;
        float halfW = halfH * 2.6f; // covers ultrawide windows
        for (int i = 0; i < layers.Length; i++)
        {
            var sr = layers[i];
            float tile = sr.sprite != null ? sr.sprite.bounds.size.x : 2f;
            tileSizes[i] = tile;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            int nx = Mathf.CeilToInt(halfW * 2f / tile) + 2;
            int ny = Mathf.CeilToInt(halfH * 2f / tile) + 2;
            if (nx % 2 == 1) nx++;
            if (ny % 2 == 1) ny++;
            sr.size = new Vector2(nx * tile, ny * tile);
        }
        Snap();
    }

    SpriteRenderer[] BuildDefaultLayers()
    {
        var detail = new GameObject("Detail");
        detail.transform.SetParent(transform, false);
        var a = ProceduralAssets.AddSprite(detail, ProceduralAssets.Tile, Color.white, -100);
        var macro = new GameObject("Macro");
        macro.transform.SetParent(transform, false);
        var b = ProceduralAssets.AddSprite(macro, ProceduralAssets.Macro, Color.white, -99);
        return new[] { a, b };
    }

    void LateUpdate() { Snap(); }

    void Snap()
    {
        if (cam == null || layers == null) return;
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] == null) continue;
            float t = tileSizes[i];
            layers[i].transform.position = new Vector3(Mathf.Round(cam.position.x / t) * t,
                                                       Mathf.Round(cam.position.y / t) * t, 0f);
        }
    }
}