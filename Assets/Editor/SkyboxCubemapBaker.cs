// Colócalo en una carpeta "Editor" (por ejemplo Assets/Editor/SkyboxCubemapBaker.cs).
// Menú: Tools > Cubemap Baker (Material)
// Sirve para cualquier material cuyo shader sea de tipo skybox (GalaxyBake, IceBake...).
using UnityEngine;
using UnityEditor;

public class SkyboxCubemapBaker : EditorWindow
{
    Material bakeMaterial;
    int size = 512;
    string fileName = "BakedCubemap";

    [MenuItem("Tools/Cubemap Baker (Material)")]
    static void Open()
    {
        GetWindow<SkyboxCubemapBaker>("Cubemap Baker");
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "1) Crea un material con un shader de horneado (Custom/IceBake, Custom/GalaxyBake).\n" +
            "2) Ajusta sus parámetros en el Inspector.\n" +
            "3) Arrástralo aquí y pulsa Hornear.", MessageType.Info);

        bakeMaterial = (Material)EditorGUILayout.ObjectField("Material de horneado", bakeMaterial, typeof(Material), false);
        size = EditorGUILayout.IntPopup("Resolución por cara", size,
            new[] { "128", "256", "512", "1024" }, new[] { 128, 256, 512, 1024 });
        fileName = EditorGUILayout.TextField("Nombre del archivo", fileName);

        EditorGUILayout.Space();
        EditorGUI.BeginDisabledGroup(bakeMaterial == null);
        if (GUILayout.Button("Hornear cubemap", GUILayout.Height(32)))
            Bake();
        EditorGUI.EndDisabledGroup();
    }

    void Bake()
    {
        // Cámara temporal que solo dibuja el skybox
        GameObject camGO = new GameObject("CubemapBakeCamera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.cullingMask = 0;
        cam.allowHDR = false;
        cam.allowMSAA = false;
        camGO.transform.position = Vector3.zero;

        Material previousSkybox = RenderSettings.skybox;
        RenderSettings.skybox = bakeMaterial;

        Cubemap cube = new Cubemap(size, TextureFormat.RGBA32, true);
        bool ok = cam.RenderToCubemap(cube);

        RenderSettings.skybox = previousSkybox;
        DestroyImmediate(camGO);

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
        Debug.Log("Cubemap horneado en " + path);
    }
}
