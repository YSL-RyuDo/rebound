using System.IO;
using Rebound;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ReboundSceneBuilder
{
    const string Root = "Assets/Rebound";
    const string ScenePath = Root + "/Scenes/Game.unity";

    [MenuItem("REBOUND/Build Windows Prototype")]
    public static void BuildWindows()
    {
        if (!File.Exists(ScenePath)) Build();
        Directory.CreateDirectory("Builds/Windows");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath }, locationPathName = "Builds/Windows/REBOUND.exe",
            target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
        });
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.Exception("REBOUND Windows build failed: " + report.summary.result);
        Debug.Log("REBOUND: Windows build succeeded.");
    }

    [MenuItem("REBOUND/Create or Open Phase 1 Game")]
    public static void CreateOrOpen()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (File.Exists(ScenePath)) { EditorSceneManager.OpenScene(ScenePath); return; }
        Build();
    }

    // Also callable with -executeMethod ReboundSceneBuilder.Build in batch mode.
    public static void Build()
    {
        if (File.Exists(ScenePath)) { Debug.Log("REBOUND: existing Game scene preserved."); return; }
        foreach (string folder in new[] { "Art", "Data", "Prefabs", "Scenes" }) Directory.CreateDirectory(Root + "/" + folder);
        AssetDatabase.Refresh();
        string spritePath = Root + "/Art/PrototypeSquare.png";
        if (!File.Exists(spritePath))
        {
            var texture = new Texture2D(16, 16);
            Color[] pixels = new Color[256];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels); texture.Apply();
            File.WriteAllBytes(spritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(spritePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(spritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }
        Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        var settings = AssetDatabase.LoadAssetAtPath<PrototypeSettings>(Root + "/Data/PrototypeSettings.asset");
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<PrototypeSettings>();
            AssetDatabase.CreateAsset(settings, Root + "/Data/PrototypeSettings.asset");
        }
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(0, 0, -10);
        camera.orthographic = true; camera.orthographicSize = GameManager.HalfHeight;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.025f, 0.04f, 0.08f);
        var game = new GameObject("GameManager").AddComponent<GameManager>();
        game.settings = settings; game.gameCamera = camera;
        var backdrop = new GameObject("Arena - original prototype geometry");
        for (int x = -8; x <= 8; x++)
            Shape("Grid", square, new Vector2(x, 0), new Vector2(0.012f, 10), new Color(0.055f, 0.09f, 0.14f), -10).transform.parent = backdrop.transform;
        for (int y = -4; y <= 4; y++)
            Shape("Grid", square, new Vector2(0, y), new Vector2(18, 0.012f), new Color(0.055f, 0.09f, 0.14f), -10).transform.parent = backdrop.transform;
        Shape("Movement boundary", square, new Vector2(-GameManager.HalfWidth + 2 * GameManager.HalfWidth * settings.movementWidth, 0),
            new Vector2(0.025f, 10), new Color(0.08f, 0.27f, 0.34f), -5).transform.parent = backdrop.transform;
        Shape("Danger line", square, new Vector2(0, -4.65f), new Vector2(18, 0.035f), new Color(0.9f, 0.2f, 0.3f), -5).transform.parent = backdrop.transform;

        var projectileShape = Shape("Projectile", square, Vector2.zero, new Vector2(0.3f, 0.1f), new Color(1f, 0.85f, 0.35f), 3);
        Body(projectileShape.gameObject, RigidbodyType2D.Dynamic);
        projectileShape.gameObject.AddComponent<BoxCollider2D>().isTrigger = true;
        projectileShape.gameObject.AddComponent<Projectile>();
        var projectilePrefab = SavePrefab<Projectile>(projectileShape.gameObject, "Projectile");
        var enemyShape = Shape("Enemy", square, Vector2.zero, new Vector2(0.65f, 0.65f), new Color(1f, 0.3f, 0.4f), 2);
        Body(enemyShape.gameObject, RigidbodyType2D.Kinematic);
        enemyShape.gameObject.AddComponent<BoxCollider2D>().isTrigger = true;
        enemyShape.gameObject.AddComponent<EnemyController>();
        var enemyPrefab = SavePrefab<EnemyController>(enemyShape.gameObject, "Enemy");

        var pool = new GameObject("ProjectilePool").AddComponent<ProjectilePool>();
        pool.game = game; pool.prefab = projectilePrefab;
        var spawner = new GameObject("EnemySpawner").AddComponent<EnemySpawner>();
        spawner.game = game; spawner.prefab = enemyPrefab; game.spawner = spawner;
        var playerShape = Shape("Player", square, game.SafePosition, new Vector2(0.55f, 0.55f), Color.cyan, 4);
        var playerObject = playerShape.gameObject;
        Body(playerObject, RigidbodyType2D.Dynamic);
        playerObject.AddComponent<BoxCollider2D>().isTrigger = true;
        var health = playerObject.AddComponent<PlayerHealth>(); health.game = game; health.visual = playerShape;
        var energy = playerObject.AddComponent<FallEnergySystem>(); energy.settings = settings;
        var rebound = playerObject.AddComponent<ReboundAttack>(); rebound.game = game; rebound.energy = energy;
        rebound.wave = Shape("Rebound flash", square, Vector2.zero, Vector2.one, Color.clear, 1);
        rebound.wave.enabled = false;
        var controller = playerObject.AddComponent<PlayerController>();
        controller.game = game; controller.health = health; controller.energy = energy; controller.rebound = rebound;
        var weapon = playerObject.AddComponent<WeaponController>(); weapon.game = game; weapon.pool = pool;
        game.player = controller; game.health = health; game.energy = energy;
        game.gameObject.AddComponent<GameHud>().game = game;
        PlayerSettings.companyName = "Independent Prototype";
        PlayerSettings.productName = "REBOUND";
        PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
        EditorSceneManager.SaveScene(scene, ScenePath);
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!scenes.Exists(s => s.path == ScenePath)) scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("REBOUND: Phase 1 Game scene and assets generated successfully.");
    }

    static SpriteRenderer Shape(string name, Sprite sprite, Vector2 position, Vector2 size, Color color, int order)
    {
        var renderer = new GameObject(name).AddComponent<SpriteRenderer>();
        renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = order;
        renderer.transform.position = position; renderer.transform.localScale = size;
        return renderer;
    }
    static void Body(GameObject obj, RigidbodyType2D type)
    {
        var body = obj.AddComponent<Rigidbody2D>();
        body.bodyType = type; body.gravityScale = 0;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }
    static T SavePrefab<T>(GameObject obj, string name) where T : Component
    {
        string path = Root + "/Prefabs/" + name + ".prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
        Object.DestroyImmediate(obj);
        return prefab.GetComponent<T>();
    }
}
