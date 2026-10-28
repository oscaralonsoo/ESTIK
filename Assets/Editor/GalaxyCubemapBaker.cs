using UnityEngine;
using UnityEditor;

public class GalaxyCubemapBaker : EditorWindow
{
    int size = 512;
    Color colorDark = new Color(0.01f, 0.0f, 0.08f, 1f);
    Color colorA = new Color(0.45f, 0.1f, 0.9f, 1f);
    Color colorB = new Color(0.1f, 0.5f, 1.0f, 1f);
    float nebulaScale = 2f;
    float starDensity = 40f;
    float starThreshold = 0.95f;
    float seed = 0f;
    string fileName = "GalaxyCubemap";

    [MenuItem("Tools/Galaxy/Cubemap Baker")]
    static void Open()
    {
        GetWindow<GalaxyCubemapBaker>("Galaxy Baker");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Nebulosa", EditorStyles.boldLabel);
        colorDark = EditorGUILayout.ColorField("Fondo", colorDark);
        colorA = EditorGUILayout.ColorField("Nebulosa 1", colorA);
        colorB = EditorGUILayout.ColorField("Nebulosa 2", colorB);
        nebulaScale = EditorGUILayout.Slider("Escala nebulosa", nebulaScale, 0.5f, 6f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Estrellas", EditorStyles.boldLabel);
        starDensity = EditorGUILayout.Slider("Densidad", starDensity, 10f, 120f);
        starThreshold = EditorGUILayout.Slider("Cantidad (menor = más)", starThreshold, 0.9f, 0.999f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Salida", EditorStyles.boldLabel);
        size = EditorGUILayout.IntPopup("Resolución por cara", size,
            new[] { "128", "256", "512", "1024" }, new[] { 128, 256, 512, 1024 });
        seed = EditorGUILayout.FloatField("Semilla", seed);
        fileName = EditorGUILayout.TextField("Nombre del archivo", fileName);

        EditorGUILayout.Space();
        if (GUILayout.Button("Hornear cubemap", GUILayout.Height(32)))
            Bake();
    }

    void Bake()
    {
        Shader shader = Shader.Find("Custom/GalaxyBake");
        if (shader == null)
        {
            Debug.LogError("No encuentro el shader Custom/GalaxyBake. ¿Lo has añadido al proyecto?");
            return;
        }

        Material mat = new Material(shader);
        mat.SetColor("_ColorDark", colorDark);
        mat.SetColor("_ColorA", colorA);
        mat.SetColor("_ColorB", colorB);
        mat.SetFloat("_NebulaScale", nebulaScale);
        mat.SetFloat("_StarDensity", starDensity);
        mat.SetFloat("_StarThreshold", starThreshold);
        mat.SetFloat("_Seed", seed);

        // Cámara temporal que solo dibuja el skybox
        GameObject camGO = new GameObject("GalaxyBakeCamera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.cullingMask = 0;
        cam.allowHDR = false;
        cam.allowMSAA = false;
        camGO.transform.position = Vector3.zero;

        Material previousSkybox = RenderSettings.skybox;
        RenderSettings.skybox = mat;

        Cubemap cube = new Cubemap(size, TextureFormat.RGBA32, true);
        bool ok = cam.RenderToCubemap(cube);

        // Limpieza: restauramos el skybox de la escena
        RenderSettings.skybox = previousSkybox;
        DestroyImmediate(camGO);
        DestroyImmediate(mat);

        if (!ok)
        {
            Debug.LogError("RenderToCubemap ha fallado.");
            DestroyImmediate(cube);
            return;
        }

        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/" + fileName + ".asset");
        AssetDatabase.CreateAsset(cube, path);
        AssetDatabase.SaveAssets();
        Selection.activeObject = cube;
        EditorGUIUtility.PingObject(cube);
        Debug.Log("Galaxia horneada en " + path);
    }
}
