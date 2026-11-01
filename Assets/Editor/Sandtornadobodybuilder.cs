#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Crea el tornado de arena en la escena con un clic (cuerpo + partículas). Sin contorno, sin cara.
///   Tools > Sand Tornado > Crear tornado completo
///   Tools > Sand Tornado > Crear solo cuerpo
///   Tools > Sand Tornado > Crear solo partículas
/// Cuerpo: anillos apilados HUECOS e irregulares (lóbulos distintos en cada uno) con el agujero arriba;
/// por dentro se ven más oscuros. La raíz recibe SandTornadoMotion (movimiento, giro, bamboleo, squash).
/// Todo se guarda en Assets/SandTornado (mallas, materiales, texturas).
/// Puede estar en cualquier carpeta de Assets.
/// </summary>
public static class SandTornadoBodyBuilder
{
    // ---- Forma
    const int Tiers = 9;
    const float Height = 3f;
    const float BaseRadius = 0.2f;
    const float TopRadius = 1.3f;
    const float ShapeCurve = 1.4f;
    const float Bulge = 0.2f;          // lo regordete que es cada anillo
    const int Rows = 8;
    const int Sides = 32;
    const float SpinSpeed = 4f;        // para las partículas (rad/s)

    // ---- Colores
    static readonly Color SandLight = new Color(1f, 0.89f, 0.62f, 1f);
    static readonly Color SandDark = new Color(0.82f, 0.56f, 0.3f, 1f);
    static readonly Color InsideColor = new Color(0.45f, 0.28f, 0.14f, 1f);
    static readonly Color DebrisColor = new Color(0.3f, 0.17f, 0.08f, 1f);

    const string Dir = "Assets/SandTornado";

    // ------------------------------------------------------------------ menús

    [MenuItem("Tools/Sand Tornado/Crear tornado completo")]
    static void CreateAll()
    {
        Transform root = GetRoot();
        BuildBody(root);
        BuildParticles(root);
        Finish(root);
    }

    [MenuItem("GameObject/Sand Tornado/Crear tornado completo", false, 10)]
    static void CreateAllContext() { CreateAll(); }

    [MenuItem("Tools/Sand Tornado/Crear solo cuerpo")]
    static void CreateBodyOnly()
    {
        Transform root = GetRoot();
        BuildBody(root);
        Finish(root);
    }

    [MenuItem("Tools/Sand Tornado/Crear solo partículas")]
    static void CreateParticlesOnly()
    {
        Transform root = GetRoot();
        BuildParticles(root);
        Finish(root);
    }

    static Transform GetRoot()
    {
        GameObject sel = Selection.activeGameObject;
        if (sel != null && !EditorUtility.IsPersistent(sel)) return sel.transform;

        var go = new GameObject("SandTornado");
        Undo.RegisterCreatedObjectUndo(go, "Crear tornado");
        return go.transform;
    }

    static void Finish(Transform root)
    {
        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        Selection.activeGameObject = root.gameObject;
    }

    static float Radius(float t)
    {
        return Mathf.Lerp(BaseRadius, TopRadius, Mathf.Pow(Mathf.Clamp01(t), ShapeCurve));
    }

    // ------------------------------------------------------------------ cuerpo

    static void BuildBody(Transform parent)
    {
        EnsureFolders();

        var body = new GameObject("Body");
        body.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(body, "Crear cuerpo");

        Material inside = GetMaterial(Dir + "/Materials/Sand_Inside.mat", InsideColor, 1f);   // 1 = solo caras de dentro

        float tierH = Height / Tiers;
        float halfH = tierH * 0.5f + tierH * 0.25f;

        for (int i = 0; i < Tiers; i++)
        {
            float tn = (i + 0.5f) / Tiers;
            float y0n = (float)i / Tiers;
            float y1n = (float)(i + 1) / Tiers;

            float rb = Radius(y0n) * 0.9f;
            float rt = Radius(y1n) * 0.9f;
            float bump = Bulge * Radius(tn);

            // Cada anillo tiene sus propios lóbulos: así no hay dos iguales
            float f1 = Mathf.Repeat(i * 0.6180339f, 1f);
            float f2 = Mathf.Repeat(i * 0.4142136f + 0.2f, 1f);
            float f3 = Mathf.Repeat(i * 0.7071068f, 1f);
            int lobes = 2 + Mathf.FloorToInt(f1 * 3f);         // 2..4
            float amp = 0.09f + 0.12f * f2;
            float phase = f3 * Mathf.PI * 2f;
            bool top = (i == Tiers - 1);

            // Centro desplazado: al girar, el anillo "baila" en vez de girar sobre un eje perfecto
            Vector2 off = top ? Vector2.zero
                : new Vector2(Mathf.Cos(phase * 1.3f), Mathf.Sin(phase * 1.3f)) * 0.08f * Radius(tn);

            Mesh mesh = BuildTierMesh(halfH, rb, rt, bump, lobes, amp, phase, top, off);
            string meshPath = AssetDatabase.GenerateUniqueAssetPath(Dir + "/Meshes/Tier" + i + ".asset");
            AssetDatabase.CreateAsset(mesh, meshPath);

            Color c = Color.Lerp(SandDark, SandLight, Mathf.Clamp01(0.25f + 0.7f * tn));
            if (i % 2 == 1) c = Color.Lerp(c, SandDark, 0.25f);
            Material fill = GetMaterial(Dir + "/Materials/Tier_" + i + ".mat", c, 2f);          // 2 = solo caras de fuera

            var tier = new GameObject("Tier" + i);
            tier.transform.SetParent(body.transform, false);
            tier.transform.localPosition = new Vector3(0f, (i + 0.5f) * tierH, 0f);

            AddMesh(tier.transform, "Fill", mesh, fill);
            AddMesh(tier.transform, "Inside", mesh, inside);
        }

        // SandTornadoMotion (movimiento, giro, bamboleo, squash) va en la raíz.
        // Se busca por nombre para que este script compile aunque aún no esté en el proyecto.
        System.Type motionType = null;
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            motionType = asm.GetType("SandTornadoMotion");
            if (motionType != null) break;
        }
        if (motionType == null)
            Debug.LogWarning("No encuentro SandTornadoMotion: añade SandTornadoMotion.cs al proyecto y ponselo al objeto raíz.");
        else if (parent.GetComponent(motionType) == null)
            parent.gameObject.AddComponent(motionType);
    }

    static void AddMesh(Transform parent, string name, Mesh mesh, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    // Anillo hueco, abierto por arriba y por abajo. Si roundTop, los lóbulos se desvanecen hacia arriba
    // para que la boca del embudo quede circular.
    static Mesh BuildTierMesh(float halfH, float rb, float rt, float bump, int lobes, float amp,
                              float phase, bool roundTop, Vector2 offset)
    {
        var verts = new System.Collections.Generic.List<Vector3>();
        var tris = new System.Collections.Generic.List<int>();

        for (int i = 0; i <= Rows; i++)
        {
            float s = i / (float)Rows;
            float y = Mathf.Lerp(-halfH, halfH, s);
            // Max con 0: Sin(pi) puede dar un negativo diminuto y Pow(negativo, 0.6) = NaN
            float baseR = Mathf.Lerp(rb, rt, s) + bump * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * s)), 0.6f);
            float a2 = roundTop ? amp * (1f - Mathf.SmoothStep(0.25f, 1f, s)) : amp;

            for (int j = 0; j <= Sides; j++)
            {
                float a = j / (float)Sides * Mathf.PI * 2f;
                // Dos armónicos mezclados: la forma es irregular pero sigue cerrando bien la vuelta
                float wave = Mathf.Sin(lobes * a + phase + s * 1.5f)
                           + 0.5f * Mathf.Sin((lobes + 2) * a + phase * 1.7f + s * 2f);
                float r = baseR * (1f + a2 * wave / 1.5f);
                verts.Add(new Vector3(Mathf.Cos(a) * r + offset.x, y, Mathf.Sin(a) * r + offset.y));
            }
        }

        for (int i = 0; i < Rows; i++)
            for (int j = 0; j < Sides; j++)
            {
                int A = i * (Sides + 1) + j, B = A + 1, C = A + Sides + 1, D = C + 1;
                tris.Add(A); tris.Add(C); tris.Add(D);
                tris.Add(A); tris.Add(D); tris.Add(B);
            }

        var mesh = new Mesh();
        mesh.name = "TornadoTier";
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // ------------------------------------------------------------------ partículas

    static void BuildParticles(Transform parent)
    {
        EnsureFolders();

        var holder = new GameObject("Particles");
        holder.transform.SetParent(parent, false);

        Material puffMat = GetParticleMaterial("Puff", 128, PuffTex);
        Material chipMat = GetParticleMaterial("Chip", 32, ChipTex);
        Material dotMat = GetParticleMaterial("Dot", 32, DotTex);

        float coneAngle = Mathf.Atan((TopRadius - BaseRadius) / Height) * Mathf.Rad2Deg;
        Color mix = Color.Lerp(SandLight, SandDark, 0.5f);

        // TopPuffs: nubes que se abren por arriba
        var ps = NewSystem(holder.transform, "TopPuffs", puffMat, ParticleSystemSimulationSpace.Local,
                           1.2f, 1.8f, 0.5f, 1f, 7f, 60);
        ps.transform.localPosition = new Vector3(0f, Height, 0f);
        var main = ps.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
        main.startColor = new ParticleSystem.MinMaxGradient(SandLight, mix);
        var sh = ps.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Circle;
        sh.radius = TopRadius * 0.7f;
        sh.radiusThickness = 0.6f;
        sh.rotation = new Vector3(-90f, 0f, 0f);
        Spin(ps, SpinSpeed * 0.6f, 0.35f);
        Fade(ps, 0.15f, 0.6f);
        Grow(ps, 0.6f, 1.5f);
        Tumble(ps, -0.6f, 0.6f);
        Noise(ps, 0.25f, 0.5f);

        // SidePuffs: nubecitas que suben pegadas al cuerpo
        ps = NewSystem(holder.transform, "SidePuffs", puffMat, ParticleSystemSimulationSpace.Local,
                       1.5f, 2f, 0.3f, 0.6f, 10f, 100);
        main = ps.main;
        main.startSpeed = Height / 1.8f;
        main.startColor = new ParticleSystem.MinMaxGradient(SandLight, Color.Lerp(SandLight, SandDark, 0.6f));
        Cone(ps, BaseRadius, coneAngle + 6f, 0.3f);
        Spin(ps, SpinSpeed);
        Fade(ps, 0.15f, 0.65f);
        Grow(ps, 0.5f, 1.4f);
        Tumble(ps, -0.6f, 0.6f);
        Noise(ps, 0.2f, 0.5f);

        // Chunks: trocitos oscuros
        ps = NewSystem(holder.transform, "Chunks", chipMat, ParticleSystemSimulationSpace.Local,
                       1.4f, 2.2f, 0.1f, 0.2f, 6f, 50);
        main = ps.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(Height / 2.4f, Height / 1.5f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            Color.Lerp(SandDark, DebrisColor, 0.4f), Color.Lerp(SandDark, DebrisColor, 0.7f));
        Cone(ps, BaseRadius * 3f, coneAngle + 16f, 1f);
        Spin(ps, SpinSpeed * 0.9f);
        Fade(ps, 0.08f, 0.85f);
        Tumble(ps, -5f, 5f);
        Noise(ps, 0.3f, 0.6f);

        // SpeedLines: rayitas de velocidad
        ps = NewSystem(holder.transform, "SpeedLines", dotMat, ParticleSystemSimulationSpace.Local,
                       0.5f, 0.9f, 0.03f, 0.05f, 16f, 100);
        main = ps.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(Height / 1.4f, Height / 0.9f);
        Color streak = Color.Lerp(SandLight, Color.white, 0.6f); streak.a = 0.8f;
        main.startColor = streak;
        Cone(ps, BaseRadius * 1.5f, coneAngle + 12f, 1f);
        Spin(ps, SpinSpeed * 1.8f);
        Fade(ps, 0.2f, 0.6f);
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.lengthScale = 4f;
        r.velocityScale = 0.04f;
        r.cameraVelocityScale = 0f;

        // BaseDust: polvo en el suelo que se queda detrás (simulación en World)
        ps = NewSystem(holder.transform, "BaseDust", puffMat, ParticleSystemSimulationSpace.World,
                       0.7f, 1.1f, 0.35f, 0.6f, 8f, 60);
        main = ps.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.Lerp(SandLight, Color.white, 0.3f), SandLight);
        sh = ps.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Circle;
        sh.radius = BaseRadius * 3f;
        sh.radiusThickness = 1f;
        sh.rotation = new Vector3(-90f, 0f, 0f);
        Spin(ps, 0f, 0.45f);
        Fade(ps, 0.05f, 0.45f);
        Grow(ps, 1.2f, 0.3f);
        Tumble(ps, -0.8f, 0.8f);

        Undo.RegisterCreatedObjectUndo(holder, "Crear partículas");
    }

    static ParticleSystem NewSystem(Transform parent, string name, Material mat, ParticleSystemSimulationSpace space,
                                    float lifeMin, float lifeMax, float sizeMin, float sizeMax, float rate, int max)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.prewarm = true;
        main.duration = 5f;
        main.simulationSpace = space;
        main.maxParticles = max;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var em = ps.emission;
        em.rateOverTime = rate;

        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = mat;
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
        return ps;
    }

    static void Cone(ParticleSystem ps, float radius, float angle, float thickness)
    {
        var sh = ps.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Cone;
        sh.angle = angle;
        sh.radius = radius;
        sh.radiusThickness = thickness;
        sh.rotation = new Vector3(-90f, 0f, 0f);
    }

    static void Spin(ParticleSystem ps, float orbital, float lift = 0f)
    {
        var v = ps.velocityOverLifetime;
        v.enabled = true;
        v.space = ParticleSystemSimulationSpace.Local;
        v.x = 0f; v.y = lift; v.z = 0f;
        v.orbitalX = 0f; v.orbitalY = orbital; v.orbitalZ = 0f;
    }

    static void Fade(ParticleSystem ps, float fadeIn, float fadeOut)
    {
        var c = ps.colorOverLifetime;
        c.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, fadeIn),
                    new GradientAlphaKey(1f, fadeOut), new GradientAlphaKey(0f, 1f) });
        c.color = g;
    }

    static void Grow(ParticleSystem ps, float from, float to)
    {
        var s = ps.sizeOverLifetime;
        s.enabled = true;
        s.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, from, 1f, to));
    }

    static void Tumble(ParticleSystem ps, float min, float max)
    {
        var r = ps.rotationOverLifetime;
        r.enabled = true;
        r.z = new ParticleSystem.MinMaxCurve(min, max);
    }

    static void Noise(ParticleSystem ps, float strength, float frequency)
    {
        var n = ps.noise;
        n.enabled = true;
        n.strength = strength;
        n.frequency = frequency;
        n.scrollSpeed = 0.5f;
    }

    // ------------------------------------------------------------------ texturas de partículas

    // Nube blanda de borde irregular, con algo de sombra abajo a la derecha (sin contorno)
    static Color PuffTex(float u, float v)
    {
        float d = Mathf.Sqrt(u * u + v * v);
        float ang = Mathf.Atan2(v, u);
        float R = 0.8f + 0.07f * Mathf.Sin(ang * 6f + 1f) + 0.04f * Mathf.Sin(ang * 11f);
        float a = Mathf.Clamp01((R - d) * 20f);
        float g = Mathf.Lerp(1f, 0.82f, Mathf.SmoothStep(0.25f, 0.35f, u * 0.6f - v * 0.7f));
        return new Color(g, g, g, a);
    }

    static Color ChipTex(float u, float v)
    {
        float d = Mathf.Max(Mathf.Abs(u), Mathf.Abs(v));
        return new Color(1f, 1f, 1f, Mathf.Clamp01((0.75f - d) * 20f));
    }

    static Color DotTex(float u, float v)
    {
        float d = Mathf.Sqrt(u * u + v * v);
        return new Color(1f, 1f, 1f, Mathf.Clamp01((0.9f - d) * 6f));
    }

    static Texture2D GetTexture(string name, int n, System.Func<float, float, Color> fn)
    {
        string path = Dir + "/Textures/" + name + ".png";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;

        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                px[y * n + x] = fn((x + 0.5f) / n * 2f - 1f, (y + 0.5f) / n * 2f - 1f);
        tex.SetPixels(px);
        tex.Apply();

        File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), path), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);

        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null)
        {
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.mipmapEnabled = true;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // ------------------------------------------------------------------ materiales

    // cull: 2 = solo se ven las caras de fuera (Render Face Front), 1 = solo las de dentro (Render Face Back)
    static Material GetMaterial(string path, Color color, float cull)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        var mat = new Material(sh);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", cull);

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // URP Particles/Unlit, transparente con alpha blending
    static Material GetParticleMaterial(string name, int texSize, System.Func<float, float, Color> fn)
    {
        string path = Dir + "/Materials/Particle_" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Texture2D tex = GetTexture(name, texSize, fn);

        Shader sh =
            Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
            Shader.Find("Particles/Standard Unlit") ??
            Shader.Find("Sprites/Default");

        var mat = new Material(sh);
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
        if (mat.HasProperty("_Mode")) mat.SetFloat("_Mode", 2f);
        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_SrcBlendAlpha")) mat.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
        if (mat.HasProperty("_DstBlendAlpha")) mat.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // ------------------------------------------------------------------ carpetas

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets", "SandTornado");
        foreach (string f in new[] { "Materials", "Meshes", "Textures" })
            if (!AssetDatabase.IsValidFolder(Dir + "/" + f)) AssetDatabase.CreateFolder(Dir, f);
    }
}
#endif