using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menu: "Human & Ghost". Exports the placeholder sprites to PNG files (Assets/Art), creates editable prefabs
/// (Assets/Prefabs) and wires everything into the current scene. Existing PNGs/prefabs are never overwritten
/// unless you choose "Regenerate", so your edits and replacement art are safe.
/// </summary>
public static class HumanGhostSetup
{
    const string ArtDir = "Assets/Art";
    const string PrefabDir = "Assets/Prefabs";
    static readonly string[] SpriteKeys = { "square", "squareLeft", "circle", "blob", "ring", "ghost", "tile", "macro" };

    [MenuItem("Human & Ghost/Setup Everything (art + prefabs + scene)")]
    public static void SetupEverything()
    {
        GenerateArtAndPrefabs(false);
        SetupScene();
    }

    [MenuItem("Human & Ghost/Regenerate Prefabs (overwrites your prefab edits)")]
    public static void Regenerate()
    {
        if (!EditorUtility.DisplayDialog("Regenerate prefabs",
            "This overwrites all prefabs in Assets/Prefabs (PNG art in Assets/Art is kept). Continue?", "Overwrite", "Cancel")) return;
        GenerateArtAndPrefabs(true);
        SetupScene();
    }

    [MenuItem("Human & Ghost/Setup Current Scene Only")]
    public static void SetupSceneMenu() { SetupScene(); }

    // ------------------------------------------------------------------
    static void GenerateArtAndPrefabs(bool overwrite)
    {
        Directory.CreateDirectory(ArtDir);
        Directory.CreateDirectory(PrefabDir);
        AssetDatabase.Refresh();

        ProceduralAssets.ResetCache();
        foreach (var key in SpriteKeys) ExportSprite(key);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Re-register the imported assets (ExportSprite does this, Refresh can invalidate references).
        foreach (var key in SpriteKeys)
            ProceduralAssets.Register(key, AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath(key)));

        ProceduralAssets.UnlitMaterial = GetOrCreateUnlitMaterial();

        SavePrefab(BuildHuman(), "Human", overwrite);
        SavePrefab(BuildGhost(), "Ghost", overwrite);
        SavePrefab(BuildEnemy(), "Enemy", overwrite);
        SavePrefab(BuildProjectile(), "Projectile", overwrite);
        SavePrefab(BuildXpOrb(), "XpOrb", overwrite);
        SavePrefab(BuildFxRing(), "FxRing", overwrite);
        SavePrefab(BuildBackground(), "Background", overwrite);
        SavePrefab(BuildHud(), "HUD", overwrite);
        SavePrefab(BuildSelection(), "UpgradeSelection", overwrite);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static string SpritePath(string key)
    {
        return ArtDir + "/" + char.ToUpper(key[0]) + key.Substring(1) + ".png";
    }

    static void ExportSprite(string key)
    {
        string path = SpritePath(key);
        if (!File.Exists(path))
        {
            Sprite src = ProceduralAssets.Get(key);
            File.WriteAllBytes(path, src.texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = src.pixelsPerUnit;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = (key == "tile" || key == "macro") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            ti.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(src.pivot.x / src.rect.width, src.pivot.y / src.rect.height);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(settings);
            ti.SaveAndReimport();
        }
        ProceduralAssets.Register(key, AssetDatabase.LoadAssetAtPath<Sprite>(path));
    }

    static Material GetOrCreateUnlitMaterial()
    {
        string path = ArtDir + "/SpriteUnlit.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    static void SavePrefab(GameObject go, string name, bool overwrite)
    {
        string path = PrefabDir + "/" + name + ".prefab";
        if (overwrite || !File.Exists(path)) PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    // ------------------------------------------------------------------ prefab builders
    static GameObject BuildHuman()
    {
        var go = new GameObject("Human");
        ProceduralAssets.AddSprite(go, ProceduralAssets.Blob, new Color(0.45f, 0.8f, 1f), 5);
        go.transform.localScale = Vector3.one * 0.9f;
        go.AddComponent<Human>();                       // RequireComponent adds the Rigidbody2D
        var rb = go.GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.mass = 3f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        go.AddComponent<CircleCollider2D>().radius = 0.4f;
        go.AddComponent<AutoShooter>();
        return go;
    }

    static GameObject BuildGhost()
    {
        var go = new GameObject("Ghost");
        ProceduralAssets.AddSprite(go, ProceduralAssets.Ghost, new Color(0.8f, 0.9f, 1f, 0.9f), 6);
        go.transform.localScale = Vector3.one * 1.1f;
        go.AddComponent<Ghost>();
        go.AddComponent<AutoShooter>();
        return go;
    }

    static GameObject BuildEnemy()
    {
        var go = new GameObject("Enemy");
        ProceduralAssets.AddSprite(go, ProceduralAssets.Blob, new Color(1f, 0.55f, 0.5f), 0);
        go.AddComponent<Enemy>();
        var rb = go.GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        go.AddComponent<CircleCollider2D>().radius = 0.5f;
        return go;
    }

    static GameObject BuildProjectile()
    {
        var go = new GameObject("Projectile");
        ProceduralAssets.AddSprite(go, ProceduralAssets.Circle, new Color(1f, 0.9f, 0.3f), 4);
        go.transform.localScale = Vector3.one * 0.28f;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;
        col.isTrigger = true;
        go.AddComponent<Projectile>();
        return go;
    }

    static GameObject BuildXpOrb()
    {
        var go = new GameObject("XpOrb");
        ProceduralAssets.AddSprite(go, ProceduralAssets.Circle, new Color(0.4f, 1f, 0.5f), 1);
        go.transform.localScale = Vector3.one * 0.3f;
        go.AddComponent<XpOrb>();
        return go;
    }

    static GameObject BuildFxRing()
    {
        var go = new GameObject("FxRing");
        ProceduralAssets.AddSprite(go, ProceduralAssets.Ring, Color.white, 8);
        go.AddComponent<FxAnimator>();
        return go;
    }

    static GameObject BuildBackground()
    {
        var root = new GameObject("Background");
        root.AddComponent<ScrollingBackground>();

        var detail = new GameObject("Detail");
        detail.transform.SetParent(root.transform, false);
        var a = ProceduralAssets.AddSprite(detail, ProceduralAssets.Tile, Color.white, -100);
        a.drawMode = SpriteDrawMode.Tiled;
        a.size = new Vector2(8f, 8f);

        var macro = new GameObject("Macro");
        macro.transform.SetParent(root.transform, false);
        var b = ProceduralAssets.AddSprite(macro, ProceduralAssets.Macro, Color.white, -99);
        b.drawMode = SpriteDrawMode.Tiled;
        b.size = new Vector2(32f, 32f);
        return root;
    }

    static GameObject BuildHud()
    {
        var go = new GameObject("HUD");
        go.AddComponent<HUD>().BuildInEditor();
        return go;
    }

    static GameObject BuildSelection()
    {
        var go = new GameObject("UpgradeSelection");
        go.AddComponent<UpgradeSelectionUI>().BuildInEditor();
        return go;
    }

    // ------------------------------------------------------------------ scene wiring
    static GameObject LoadPrefab(string name)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/" + name + ".prefab");
    }

    static void SetupScene()
    {
        var gm = Object.FindFirstObjectByType<GameManager>();
        if (gm == null)
        {
            var go = new GameObject("GameManager");
            Undo.RegisterCreatedObjectUndo(go, "Create GameManager");
            gm = go.AddComponent<GameManager>();
        }
        Undo.RecordObject(gm, "Assign Human & Ghost prefabs");
        gm.humanPrefab = LoadPrefab("Human");
        gm.ghostPrefab = LoadPrefab("Ghost");
        gm.enemyPrefab = LoadPrefab("Enemy");
        gm.projectilePrefab = LoadPrefab("Projectile");
        gm.xpOrbPrefab = LoadPrefab("XpOrb");
        gm.fxRingPrefab = LoadPrefab("FxRing");
        gm.backgroundPrefab = LoadPrefab("Background");
        gm.hudPrefab = LoadPrefab("HUD");
        gm.upgradeSelectionPrefab = LoadPrefab("UpgradeSelection");
        EditorUtility.SetDirty(gm);

        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindFirstObjectByType<Camera>();
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Undo.RegisterCreatedObjectUndo(camGo, "Create Camera");
            cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.08f, 0.11f);
        if (cam.GetComponent<DynamicCamera>() == null) Undo.AddComponent<DynamicCamera>(cam.gameObject);

        EditorSceneManager.MarkSceneDirty(gm.gameObject.scene);
        Selection.activeGameObject = gm.gameObject;
        Debug.Log("Human & Ghost: prefabs generated in Assets/Prefabs, art in Assets/Art, GameManager wired up. Press Play.");
    }
}