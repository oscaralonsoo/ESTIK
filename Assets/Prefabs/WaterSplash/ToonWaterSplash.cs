using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using MMC = UnityEngine.ParticleSystem.MinMaxCurve;
using MMG = UnityEngine.ParticleSystem.MinMaxGradient;

/// <summary>
/// Salpicadura de agua estilo toon para cuando la bola cae al agua.
///
/// DOS FORMAS DE USARLO
///  A) Prefab suelto (recomendado):
///       menú  Tools > Toon > Create Water Splash Prefab
///       Crea "Assets/ToonWaterSplash_Baked/ToonWaterSplash.prefab" con todo guardado
///       (hijos, texturas y materiales como assets). Instancia con:
///           Instantiate(prefab, posicionEnLaSuperficie, Quaternion.identity);
///       Se reproduce solo y se destruye a los 3 s.
///  B) En el objeto de agua (con Collider "Is Trigger"): salta al entrar la bola.
///       Si quieres que no se genere en runtime, haz clic derecho en el componente
///       > "Bake: crear hijos persistentes + guardar assets" (con el juego parado).
///
/// 4 capas, todas de un solo disparo (burst):
///  - Rings : dos anillos que se expanden y se desvanecen sobre la superficie.
///  - Foam  : círculos de espuma que salen hacia fuera y se encogen.
///  - Drops : gotas que saltan hacia arriba, estiradas por la velocidad, y caen.
///  - Blobs : 4-6 gotas grandes que forman la "corona" central.
/// </summary>
[DisallowMultipleComponent]
public class ToonWaterSplash : MonoBehaviour
{
    [Header("DISPARO AUTOMATICO (opcional)")]
    [SerializeField] private bool playOnBallEnter = true;
    [Tooltip("Usa la cara superior del Collider de este objeto como altura de la superficie.")]
    [SerializeField] private bool useColliderTopAsSurface = true;
    [Tooltip("Altura de la superficie si NO usas el collider.")]
    [SerializeField] private float surfaceY = 0f;
    [SerializeField] private float cooldown = 0.25f;

    [Header("MODO PREFAB")]
    [Tooltip("Reproduce el efecto al instanciarse, en la posición de este objeto.")]
    [SerializeField] private bool playOnStart = false;
    [Tooltip("Destruye este objeto N segundos después de reproducirse (0 = nunca).")]
    [SerializeField] private float destroyAfterSeconds = 0f;

    [Header("TAMAÑO")]
    [Tooltip("Multiplica tamaños de gotas, espuma y radios. Ajústalo al tamaño de tu bola.")]
    [SerializeField, Range(0.1f, 4f)] private float splashScale = 1f;
    [Tooltip("Diámetro final del anillo grande (unidades de mundo).")]
    [SerializeField] private float ringDiameter = 3f;
    [Tooltip("Separación sobre el agua para que las partículas no se pisen con la superficie.")]
    [SerializeField] private float heightOffset = 0.03f;

    [Header("SALTO")]
    [SerializeField] private float jumpSpeed = 5.5f;
    [SerializeField] private float gravityMultiplier = 2f;
    [SerializeField] private int dropCount = 16;
    [SerializeField] private int blobCount = 5;

    [Header("TIEMPO")]
    [Tooltip("Actívalo si quieres que el efecto ignore el slow-motion de tu juego.")]
    [SerializeField] private bool useUnscaledTime = false;

    [Header("COLORES")]
    [SerializeField] private Color waterLight = new Color(0.45f, 0.85f, 1f, 1f);
    [SerializeField] private Color waterDeep = new Color(0.15f, 0.55f, 0.95f, 1f);
    [SerializeField] private Color foamWhite = new Color(1f, 1f, 1f, 1f);

    // Estado "horneado" (guardado en la escena/prefab)
    [SerializeField, HideInInspector] private Transform bakedRoot;
    [SerializeField, HideInInspector] private List<ParticleSystem> bakedSystems = new List<ParticleSystem>();

    private Transform root;
    private readonly List<ParticleSystem> systems = new List<ParticleSystem>();
    private readonly List<Object> created = new List<Object>();
    private float lastPlayTime = -10f;
    private bool persistent; // true mientras se hace el Bake en el editor

    private void Awake()
    {
        // Si ya está horneado, usamos los hijos guardados y no generamos nada en runtime
        if (bakedRoot != null && bakedSystems != null && bakedSystems.Count > 0)
        {
            root = bakedRoot;
            systems.Clear();
            systems.AddRange(bakedSystems);
        }
        else
        {
            Build();
        }
    }

    private void Start()
    {
        if (!playOnStart) return;

        Play(transform.position);
        if (destroyAfterSeconds > 0f) Destroy(gameObject, destroyAfterSeconds);
    }

    private void OnDestroy() => Cleanup();

    // ????????????????????????? Disparo ?????????????????????????

    private void OnTriggerEnter(Collider other)
    {
        if (!playOnBallEnter) return;
        if (other.GetComponentInParent<BallController>() == null) return;
        if (Time.unscaledTime - lastPlayTime < cooldown) return;

        Vector3 p = other.attachedRigidbody != null
            ? other.attachedRigidbody.position
            : other.transform.position;

        Collider myCol = GetComponent<Collider>();
        p.y = (useColliderTopAsSurface && myCol != null) ? myCol.bounds.max.y : surfaceY;

        Play(p);
    }

    /// <summary>Lanza la salpicadura en un punto de la superficie del agua (mundo).</summary>
    public void Play(Vector3 surfacePosition)
    {
        if (root == null) return;
        lastPlayTime = Time.unscaledTime;

        root.SetPositionAndRotation(surfacePosition + Vector3.up * heightOffset, Quaternion.identity);

        foreach (var ps in systems)
        {
            ps.Clear(true);
            ps.Play(true);
        }
    }

    /// <summary>Reconstruye todo en runtime (úsalo si cambias tamaños, colores o cantidades).</summary>
    [ContextMenu("Rebuild")]
    public void Rebuild()
    {
        if (bakedRoot != null)
        {
            Debug.LogWarning("[ToonWaterSplash] Está horneado. Cambia los valores y vuelve a hacer Bake (con el juego parado).");
            return;
        }
        Build();
    }

    // ????????????????????????? Construcción ?????????????????????????

    private void Build()
    {
        Cleanup();

        root = new GameObject("ToonWaterSplash_Root").transform;
        if (persistent)
        {
            // Bake: el root cuelga de este objeto para que se guarde con la escena/prefab
            root.SetParent(transform, false);
        }
        else
        {
            // Runtime: sin padre, así no hereda la rotación/escala del objeto de agua
            created.Add(root.gameObject);
        }

        Texture2D circleTex = Tex(MakeShapeTexture(64, 2f), "ToonSplash_Circle");
        Texture2D ringTex = Tex(MakeRingTexture(128, 0.72f), "ToonSplash_Ring");
        Material circleMat = MakeMaterial(circleTex, "ToonSplash_Circle");
        Material ringMat = MakeMaterial(ringTex, "ToonSplash_Ring");

        BuildRing("RingOuter", ringMat, ringDiameter, 0f, foamWhite);
        BuildRing("RingInner", ringMat, ringDiameter * 0.6f, 0.1f, waterLight);
        BuildFoam(circleMat);
        BuildDrops(circleMat);
        BuildBlobs(circleMat);
    }

    private void BuildRing(string name, Material mat, float diameter, float delay, Color color)
    {
        var ps = CreateSystem(name, mat, out ParticleSystemRenderer r);
        r.renderMode = ParticleSystemRenderMode.HorizontalBillboard;

        var main = ps.main;
        ApplyCommon(ref main, 4);
        main.startLifetime = new MMC(0.7f);
        main.startSpeed = new MMC(0f);
        main.startSize = new MMC(diameter * splashScale);
        main.startColor = new MMG(color);

        SetBurst(ps, delay, 1);
        var sh = ps.shape;
        sh.enabled = false;

        // Se expande rápido al principio y frena (ease-out)
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new MMC(1f, new AnimationCurve(
            new Keyframe(0f, 0.12f),
            new Keyframe(0.3f, 0.80f),
            new Keyframe(1f, 1f)));

        // Se mantiene sólido y se desvanece en la segunda mitad
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.45f), new GradientAlphaKey(0f, 1f) });
        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new MMG(grad);
    }

    private void BuildFoam(Material mat)
    {
        var ps = CreateSystem("Foam", mat, out ParticleSystemRenderer r);
        r.renderMode = ParticleSystemRenderMode.HorizontalBillboard;

        var main = ps.main;
        ApplyCommon(ref main, 40);
        main.startLifetime = new MMC(0.6f, 1.0f);
        main.startSpeed = new MMC(0.4f, 1.2f);
        main.startSize = new MMC(0.15f * splashScale, 0.35f * splashScale);
        main.startColor = new MMG(foamWhite);

        SetBurst(ps, 0f, 12);

        // Círculo horizontal (rotado -90° en X), emite hacia fuera sobre la superficie
        var sh = ps.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Circle;
        sh.rotation = new Vector3(-90f, 0f, 0f);
        sh.radius = ringDiameter * 0.18f * splashScale;
        sh.radiusThickness = 1f;

        SetPopShrink(ps);
    }

    private void BuildDrops(Material mat)
    {
        var ps = CreateSystem("Drops", mat, out ParticleSystemRenderer r);
        r.renderMode = ParticleSystemRenderMode.Stretch; // se estiran con la velocidad
        r.lengthScale = 1f;
        r.velocityScale = 0.03f;
        r.cameraVelocityScale = 0f;

        var main = ps.main;
        ApplyCommon(ref main, 60);
        main.startLifetime = new MMC(0.7f, 1.1f);
        main.startSpeed = new MMC(jumpSpeed * 0.6f, jumpSpeed * 1.1f);
        main.startSize = new MMC(0.10f * splashScale, 0.26f * splashScale);
        main.startColor = new MMG(waterLight, foamWhite);
        main.gravityModifier = gravityMultiplier;

        SetBurst(ps, 0f, dropCount);
        SetupUpwardCone(ps, 28f, 0.12f * splashScale);

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new MMC(1f, new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.7f, 1f),
            new Keyframe(1f, 0f)));
    }

    private void BuildBlobs(Material mat)
    {
        var ps = CreateSystem("Blobs", mat, out ParticleSystemRenderer r);

        var main = ps.main;
        ApplyCommon(ref main, 20);
        main.startLifetime = new MMC(0.5f, 0.8f);
        main.startSpeed = new MMC(jumpSpeed * 0.4f, jumpSpeed * 0.7f);
        main.startSize = new MMC(0.28f * splashScale, 0.5f * splashScale);
        main.startColor = new MMG(waterDeep, waterLight);
        main.gravityModifier = gravityMultiplier;

        SetBurst(ps, 0f, blobCount);
        SetupUpwardCone(ps, 14f, 0.08f * splashScale);

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new MMC(1f, new AnimationCurve(
            new Keyframe(0f, 0.4f),
            new Keyframe(0.15f, 1f),
            new Keyframe(0.7f, 1f),
            new Keyframe(1f, 0f)));
    }

    // ????????????????????????? Helpers de partículas ?????????????????????????

    private ParticleSystem CreateSystem(string name, Material mat, out ParticleSystemRenderer renderer)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // parar antes de configurar

        renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

        systems.Add(ps);
        return ps;
    }

    private void ApplyCommon(ref ParticleSystem.MainModule main, int maxParticles)
    {
        main.duration = 1f;
        main.loop = false;
        main.playOnAwake = false;
        main.prewarm = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.useUnscaledTime = useUnscaledTime;
        main.gravityModifier = 0f;
        main.maxParticles = maxParticles;
    }

    private void SetBurst(ParticleSystem ps, float time, int count)
    {
        var em = ps.emission;
        em.enabled = true;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(time, (short)count) });
    }

    // Cono apuntando hacia ARRIBA (rotación -90° en X: +Z pasa a +Y)
    private void SetupUpwardCone(ParticleSystem ps, float angle, float radius)
    {
        var sh = ps.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Cone;
        sh.rotation = new Vector3(-90f, 0f, 0f);
        sh.angle = angle;
        sh.radius = radius;
        sh.radiusThickness = 1f;
    }

    // Aparece con un "pop" y se encoge hasta desaparecer
    private void SetPopShrink(ParticleSystem ps)
    {
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new MMC(1f, new AnimationCurve(
            new Keyframe(0f, 0.3f),
            new Keyframe(0.2f, 1f),
            new Keyframe(1f, 0f)));
    }

    // ????????????????????????? Texturas y material ?????????????????????????

    /// <summary>En runtime la registra para destruirla luego; en Bake la guarda como PNG en Assets.</summary>
    private Texture2D Tex(Texture2D tex, string fileName)
    {
#if UNITY_EDITOR
        if (persistent) return SaveTextureAsset(tex, fileName);
#endif
        created.Add(tex);
        return tex;
    }

    /// <summary>Forma rellena de borde duro. n=2 círculo.</summary>
    private Texture2D MakeShapeTexture(int size, float n)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "ToonSplash_Shape" };
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size * 2f - 1f;
                float v = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Pow(Mathf.Pow(Mathf.Abs(u), n) + Mathf.Pow(Mathf.Abs(v), n), 1f / n);
                float alpha = Mathf.Clamp01((0.95f - d) * size * 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }

        tex.SetPixels32(px);
        tex.Apply(true);
        return tex;
    }

    /// <summary>Anillo de borde duro. innerRadius = radio interior (0-1): más alto = más fino.</summary>
    private Texture2D MakeRingTexture(int size, float innerRadius)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "ToonSplash_Ring" };
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size * 2f - 1f;
                float v = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(u * u + v * v);

                float outer = Mathf.Clamp01((0.95f - d) * size * 0.5f);
                float inner = Mathf.Clamp01((d - innerRadius) * size * 0.5f);
                float alpha = outer * inner;

                px[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }

        tex.SetPixels32(px);
        tex.Apply(true);
        return tex;
    }

    private Material MakeMaterial(Texture2D tex, string name)
    {
        // En runtime puro: añade "Universal Render Pipeline/Particles/Unlit" a
        // Project Settings > Graphics > Always Included Shaders, o Shader.Find devolverá null.
        // Con Bake el material guarda la referencia al shader y no hace falta.
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");

        var mat = new Material(sh) { name = name };

        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);

        if (mat.HasProperty("_Surface")) // URP: forzar superficie transparente
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

#if UNITY_EDITOR
        if (persistent) return SaveMaterialAsset(mat, name);
#endif
        created.Add(mat);
        return mat;
    }

    // ????????????????????????? Limpieza ?????????????????????????

    private void Cleanup()
    {
        foreach (var o in created)
        {
            if (o == null) continue;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }
        created.Clear();
        systems.Clear();
        root = null;
    }

    // ????????????????????????? Bake / Prefab (solo editor) ?????????????????????????
#if UNITY_EDITOR
    private const string BakeFolder = "Assets/ToonWaterSplash_Baked";

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(BakeFolder))
            AssetDatabase.CreateFolder("Assets", "ToonWaterSplash_Baked");
    }

    private Texture2D SaveTextureAsset(Texture2D tex, string fileName)
    {
        EnsureFolder();
        string path = BakeFolder + "/" + fileName + ".png";

        System.IO.File.WriteAllBytes(System.IO.Path.GetFullPath(path), tex.EncodeToPNG());
        DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private Material SaveMaterialAsset(Material mat, string name)
    {
        EnsureFolder();
        string path = BakeFolder + "/" + name + ".mat";

        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(mat, existing);
            DestroyImmediate(mat);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    [ContextMenu("Bake: crear hijos persistentes + guardar assets")]
    private void Bake()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[ToonWaterSplash] Haz el Bake con el juego parado.");
            return;
        }

        BakeInternal();

        EditorUtility.SetDirty(this);
        if (gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(gameObject.scene);
        Debug.Log("[ToonWaterSplash] Bake hecho. Texturas y materiales guardados en " + BakeFolder);
    }

    [ContextMenu("Quitar bake")]
    private void ClearBakedMenu()
    {
        ClearBaked();
        EditorUtility.SetDirty(this);
        if (gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }

    private void ClearBaked()
    {
        if (bakedRoot != null) DestroyImmediate(bakedRoot.gameObject);
        bakedRoot = null;
        if (bakedSystems != null) bakedSystems.Clear();
        root = null;
        systems.Clear();
    }

    private void BakeInternal()
    {
        ClearBaked();

        persistent = true;
        try { Build(); }
        finally { persistent = false; }

        bakedRoot = root;
        bakedSystems = new List<ParticleSystem>(systems);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Toon/Create Water Splash Prefab")]
    private static void CreatePrefabMenu()
    {
        EnsureFolder();

        var go = new GameObject("ToonWaterSplash");
        var comp = go.AddComponent<ToonWaterSplash>();
        comp.playOnBallEnter = false;
        comp.playOnStart = true;
        comp.destroyAfterSeconds = 3f;
        comp.BakeInternal();

        string path = BakeFolder + "/ToonWaterSplash.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);

        AssetDatabase.SaveAssets();
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log("[ToonWaterSplash] Prefab creado en " + path);
    }
#endif
}