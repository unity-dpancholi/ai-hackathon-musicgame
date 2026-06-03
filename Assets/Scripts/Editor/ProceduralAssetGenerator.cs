using UnityEngine;
using UnityEditor;
using System.IO;

public static class ProceduralAssetGenerator
{
    public static Color blowColor = new Color(0.117f, 0.564f, 1.0f, 1.0f); // #1E90FF
    public static Color drawColor = new Color(1.0f, 0.549f, 0.0f, 1.0f);   // #FF8C00

    [MenuItem("MouthOrgan/Generate Stylized Sprites")]
    public static void GenerateAllSprites()
    {
        string dirPath = "Assets/Sprites";
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        GenerateHarmonica(Path.Combine(dirPath, "Harmonica.png"));
        GenerateMouthReticle(Path.Combine(dirPath, "MouthReticle.png"));
        GenerateNote(Path.Combine(dirPath, "NoteBlowTap.png"), true, false);
        GenerateNote(Path.Combine(dirPath, "NoteBlowSustain.png"), true, true);
        GenerateNote(Path.Combine(dirPath, "NoteDrawTap.png"), false, false);
        GenerateNote(Path.Combine(dirPath, "NoteDrawSustain.png"), false, true);
        GenerateOxygenTrack(Path.Combine(dirPath, "OxygenTrack.png"));
        GenerateOxygenHandle(Path.Combine(dirPath, "OxygenHandle.png"));
        GenerateBackground(Path.Combine(dirPath, "GameBackground.png"));
        GenerateUIPanelBase(Path.Combine(dirPath, "UIPanelBase.png"));

        AssetDatabase.Refresh();

        // Configure Import Settings
        ConfigureImportSettings(Path.Combine(dirPath, "Harmonica.png"), 102.4f, SpriteMeshType.FullRect, Vector4.zero);
        ConfigureImportSettings(Path.Combine(dirPath, "MouthReticle.png"), 102.4f, SpriteMeshType.FullRect, Vector4.zero);
        ConfigureImportSettings(Path.Combine(dirPath, "NoteBlowTap.png"), 256.0f, SpriteMeshType.FullRect, Vector4.zero);
        ConfigureImportSettings(Path.Combine(dirPath, "NoteBlowSustain.png"), 256.0f, SpriteMeshType.FullRect, new Vector4(30, 40, 30, 40));
        ConfigureImportSettings(Path.Combine(dirPath, "NoteDrawTap.png"), 256.0f, SpriteMeshType.FullRect, Vector4.zero);
        ConfigureImportSettings(Path.Combine(dirPath, "NoteDrawSustain.png"), 256.0f, SpriteMeshType.FullRect, new Vector4(30, 40, 30, 40));
        ConfigureImportSettings(Path.Combine(dirPath, "OxygenTrack.png"), 100.0f, SpriteMeshType.FullRect, new Vector4(15, 0, 15, 0));
        ConfigureImportSettings(Path.Combine(dirPath, "OxygenHandle.png"), 100.0f, SpriteMeshType.FullRect, Vector4.zero);
        ConfigureImportSettings(Path.Combine(dirPath, "GameBackground.png"), 102.4f, SpriteMeshType.FullRect, Vector4.zero);
        ConfigureImportSettings(Path.Combine(dirPath, "UIPanelBase.png"), 100.0f, SpriteMeshType.FullRect, new Vector4(30, 30, 30, 30));

        AssetDatabase.Refresh();
        Debug.Log("Procedural game sprites generated and imported successfully!");
    }

    private static void ConfigureImportSettings(string assetPath, float ppu, SpriteMeshType meshType, Vector4 border)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;

            // Set borders if specified
            if (border != Vector4.zero)
            {
                importer.spriteBorder = border;
            }

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = meshType;
            importer.SetTextureSettings(settings);

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }
    }

    private static float SDFRoundBox(Vector2 p, Vector2 size, float radius)
    {
        Vector2 d = new Vector2(Mathf.Abs(p.x) - size.x + radius, Mathf.Abs(p.y) - size.y + radius);
        return Mathf.Min(Mathf.Max(d.x, d.y), 0.0f) + new Vector2(Mathf.Max(d.x, 0.0f), Mathf.Max(d.y, 0.0f)).magnitude - radius;
    }

    private static float SDFCircle(Vector2 p, float radius)
    {
        return p.magnitude - radius;
    }

    private static float SDFCapsule(Vector2 p, Vector2 a, Vector2 b, float r)
    {
        Vector2 pa = p - a, ba = b - a;
        float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
        return (pa - ba * h).magnitude - r;
    }

    private static void GenerateHarmonica(string path)
    {
        int width = 1024;
        int height = 256;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] colors = new Color[width * height];

        Vector2 center = new Vector2(width / 2.0f, height / 2.0f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 p = new Vector2(x, y);
                float d_body = SDFRoundBox(p - center, new Vector2(480f, 85f), 35f);
                Color pixelColor = new Color(0, 0, 0, 0);

                if (d_body <= 0)
                {
                    float t_y = (y - 43f) / 170f; // 0 to 1 inside body height
                    Color metalColor = Color.grey;

                    // Shiny chrome gradient shading
                    if (t_y > 0.85f)
                    {
                        metalColor = Color.Lerp(new Color(0.85f, 0.88f, 0.9f), Color.white, (t_y - 0.85f) / 0.15f);
                    }
                    else if (t_y > 0.5f)
                    {
                        metalColor = Color.Lerp(new Color(0.65f, 0.68f, 0.72f), new Color(0.85f, 0.88f, 0.9f), (t_y - 0.5f) / 0.35f);
                    }
                    else if (t_y > 0.15f)
                    {
                        metalColor = Color.Lerp(new Color(0.35f, 0.40f, 0.45f), new Color(0.65f, 0.68f, 0.72f), (t_y - 0.15f) / 0.35f);
                    }
                    else
                    {
                        metalColor = Color.Lerp(new Color(0.15f, 0.18f, 0.22f), new Color(0.35f, 0.40f, 0.45f), t_y / 0.15f);
                    }

                    // Gold plate brand branding plate in the middle
                    float d_plate = SDFRoundBox(p - new Vector2(512f, 195f), new Vector2(100f, 12f), 4f);
                    if (d_plate <= 0)
                    {
                        metalColor = new Color(0.95f, 0.75f, 0.15f, 1.0f); // Gold fill
                        if (d_plate > -2f)
                        {
                            metalColor = Color.black; // Gold plate outline
                        }
                    }

                    // 10 holes spacing
                    bool insideHole = false;
                    Color holeColor = Color.black;
                    for (int i = 0; i < 10; i++)
                    {
                        float h_x = (i - 4.5f) * 102.4f + 512.0f;
                        float h_y = 120.0f;
                        float d_hole = SDFRoundBox(p - new Vector2(h_x, h_y), new Vector2(25f, 35f), 8f);
                        if (d_hole <= 0)
                        {
                            insideHole = true;
                            holeColor = new Color(0.08f, 0.08f, 0.1f, 1.0f); // Deep dark cavity
                            
                            // High contrast amber gold rim
                            if (d_hole > -4f)
                            {
                                holeColor = new Color(0.95f, 0.6f, 0.0f, 1.0f);
                            }
                            break;
                        }
                    }

                    if (insideHole)
                    {
                        pixelColor = holeColor;
                    }
                    else
                    {
                        if (d_body > -8f)
                        {
                            pixelColor = Color.black; // Bold black outer border
                        }
                        else
                        {
                            pixelColor = metalColor;
                        }
                    }
                }

                colors[y * width + x] = pixelColor;
            }
        }

        tex.SetPixels(colors);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void GenerateMouthReticle(string path)
    {
        int width = 256;
        int height = 256;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] colors = new Color[width * height];

        Vector2 center = new Vector2(width / 2.0f, height / 2.0f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 p = new Vector2(x, y);
                float d_lips = SDFCircle(p - center, 75f);
                float d_hole = SDFCircle(p - center, 26f);
                Color pixelColor = new Color(0, 0, 0, 0);

                if (d_lips <= 0 && d_hole > 0)
                {
                    Color lipColor = new Color(0.95f, 0.15f, 0.15f, 1.0f); // Cherry red lips

                    // Puckered creases
                    float angle = Mathf.Atan2(p.y - 128f, p.x - 128f);
                    float crease = Mathf.Sin(angle * 12f);
                    if (crease > 0.8f)
                    {
                        lipColor = Color.Lerp(lipColor, new Color(0.6f, 0.05f, 0.05f), 0.5f);
                    }

                    // Highlight reflection shine
                    float d_shine = SDFCircle(p - new Vector2(95f, 160f), 15f);
                    if (d_shine < 0)
                    {
                        float shineAlpha = Mathf.Clamp01(-d_shine / 8f);
                        lipColor = Color.Lerp(lipColor, Color.white, shineAlpha * 0.7f);
                    }

                    if (d_lips > -6f)
                    {
                        pixelColor = Color.black; // Lips outer black stroke
                    }
                    else
                    {
                        pixelColor = lipColor;
                    }
                }
                else if (d_hole <= 0)
                {
                    Color cavityColor = new Color(0.12f, 0.02f, 0.05f, 1.0f); // Deep throat dark cavity

                    // Goofy cartoon teeth at top of mouth
                    float d_teeth = SDFRoundBox(p - new Vector2(128f, 142f), new Vector2(20f, 10f), 3f);
                    if (d_teeth <= 0)
                    {
                        cavityColor = Color.white;
                        if (Mathf.Abs(p.x - 128f) < 1.5f && p.y > 132f)
                        {
                            cavityColor = Color.black; // Split line
                        }
                        if (d_teeth > -2f)
                        {
                            cavityColor = Color.black; // Teeth outline
                        }
                    }

                    if (d_hole > -4f)
                    {
                        pixelColor = Color.black; // Hole inner border
                    }
                    else
                    {
                        pixelColor = cavityColor;
                    }
                }

                colors[y * width + x] = pixelColor;
            }
        }

        tex.SetPixels(colors);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void GenerateNote(string path, bool isBlow, bool isSustain)
    {
        int width = 256;
        int height = isSustain ? 512 : 256;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] colors = new Color[width * height];

        Vector2 center = new Vector2(width / 2.0f, height / 2.0f);
        Vector2 size = isSustain ? new Vector2(100f, 236f) : new Vector2(100f, 100f);
        float radius = 30f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 p = new Vector2(x, y);
                float d_note = SDFRoundBox(p - center, size, radius);
                Color pixelColor = new Color(0, 0, 0, 0);

                if (d_note <= 0)
                {
                    Color baseColor = isBlow ? blowColor : drawColor;

                    // Shiny cel-shaded style lighting
                    float shade = Vector2.Dot((p - center).normalized, new Vector2(0.7f, 0.7f));
                    Color cellColor = Color.Lerp(baseColor * 0.75f, baseColor * 1.25f, (shade + 1.0f) * 0.5f);

                    // Curved glossy white gloss reflection
                    Vector2 shineCenter = isSustain ? new Vector2(70f, height - 70f) : new Vector2(75f, 175f);
                    float d_shine = SDFCircle(p - shineCenter, 20f);
                    if (d_shine < 0)
                    {
                        cellColor = Color.Lerp(cellColor, Color.white, 0.5f);
                    }

                    if (d_note > -8f)
                    {
                        pixelColor = Color.black; // Bold black outline
                    }
                    else
                    {
                        pixelColor = cellColor;
                    }
                }

                colors[y * width + x] = pixelColor;
            }
        }

        tex.SetPixels(colors);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void GenerateOxygenTrack(string path)
    {
        int width = 512;
        int height = 64;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] colors = new Color[width * height];

        Vector2 center = new Vector2(width / 2.0f, height / 2.0f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 p = new Vector2(x, y);
                float d_track = SDFRoundBox(p - center, new Vector2(246f, 20f), 12f);
                Color pixelColor = new Color(0, 0, 0, 0);

                if (d_track <= 0)
                {
                    Color trackColor = new Color(0.18f, 0.24f, 0.32f, 1.0f); // Slate charcoal

                    // Tick mark divisions
                    bool isTick = false;
                    for (int x_tick = 50; x_tick < 512; x_tick += 40)
                    {
                        if (Mathf.Abs(x - x_tick) < 2f && y > 18 && y < 46)
                        {
                            isTick = true;
                            break;
                        }
                    }

                    if (isTick)
                    {
                        trackColor = new Color(0.28f, 0.38f, 0.48f, 1.0f);
                    }

                    if (d_track > -4f)
                    {
                        pixelColor = Color.black;
                    }
                    else
                    {
                        pixelColor = trackColor;
                    }
                }

                colors[y * width + x] = pixelColor;
            }
        }

        tex.SetPixels(colors);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void GenerateOxygenHandle(string path)
    {
        int width = 128;
        int height = 128;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] colors = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 p = new Vector2(x, y);
                float d_tank = SDFCapsule(p, new Vector2(64f, 35f), new Vector2(64f, 95f), 20f);
                Color pixelColor = new Color(0, 0, 0, 0);

                if (d_tank <= 0)
                {
                    Color tankColor = new Color(0.0f, 0.9f, 1.0f, 1.0f); // Neon Cyan

                    // Shiny highlight
                    float shade = (x - 44f) / 40f;
                    Color cellColor = Color.Lerp(tankColor * 0.7f, Color.white, Mathf.Clamp01(1f - Mathf.Abs(shade - 0.3f) * 3f) * 0.6f);
                    cellColor = Color.Lerp(cellColor, tankColor * 0.5f, Mathf.Clamp01(shade) * 0.4f);

                    // Silver metal cap at top
                    if (y > 85f)
                    {
                        cellColor = new Color(0.75f, 0.78f, 0.82f, 1.0f);
                        if (y > 85f && y < 88f)
                        {
                            cellColor = Color.black;
                        }
                    }

                    // Pressure dial gauge
                    float d_gauge = SDFCircle(p - new Vector2(64f, 60f), 12f);
                    if (d_gauge <= 0)
                    {
                        cellColor = Color.white;
                        if (d_gauge > -2f)
                        {
                            cellColor = Color.black;
                        }
                        else
                        {
                            // Tiny red needle pointing up-right
                            Vector2 needleVec = p - new Vector2(64f, 60f);
                            float distToNeedle = Vector2.Distance(p, new Vector2(64f, 60f) + needleVec.normalized * Mathf.Min(needleVec.magnitude, 9f));
                            if (distToNeedle < 1.5f && Vector2.Dot(needleVec, new Vector2(1f, 1.5f)) > 0)
                            {
                                cellColor = Color.red;
                            }
                        }
                    }

                    if (d_tank > -4f)
                    {
                        pixelColor = Color.black;
                    }
                    else
                    {
                        pixelColor = cellColor;
                    }
                }

                colors[y * width + x] = pixelColor;
            }
        }

        tex.SetPixels(colors);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void GenerateBackground(string path)
    {
        int width = 1024;
        int height = 1024;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] colors = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Rich indigo background gradient
                float t_bg = y / 1024.0f;
                Color bgColor = Color.Lerp(new Color(0.10f, 0.06f, 0.24f, 1.0f), new Color(0.04f, 0.05f, 0.10f, 1.0f), t_bg);

                // Rays originating from bottom-center (512, 128)
                float dx = x - 512f;
                float dy = y - 128f;
                float dist_x = Mathf.Abs(dx);

                float theta = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                if (theta < 0) theta += 360f;

                // 16 rays around the center
                float rayPattern = Mathf.Sin(theta * 16f * Mathf.Deg2Rad);
                if (rayPattern > 0)
                {
                    // Clean central area factor (notes fall corridor)
                    float centerCleanFactor = Mathf.Clamp01((dist_x - 120f) / 120f);
                    
                    // Lighter neon purple-magenta rays
                    Color rayColor = new Color(0.16f, 0.11f, 0.33f, 1.0f);
                    bgColor = Color.Lerp(bgColor, rayColor, rayPattern * centerCleanFactor * 0.45f);
                }

                colors[y * width + x] = bgColor;
            }
        }

        tex.SetPixels(colors);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        }

        private static void GenerateUIPanelBase(string path)
        {
        int width = 256;
        int height = 256;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] colors = new Color[width * height];

        Vector2 center = new Vector2(width / 2.0f, height / 2.0f);
        Vector2 size = new Vector2(100f, 100f);
        float radius = 30f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 p = new Vector2(x, y);
                float d_panel = SDFRoundBox(p - center, size, radius);
                Color pixelColor = new Color(0, 0, 0, 0);

                if (d_panel <= 0)
                {
                    Color baseColor = Color.white;

                    // Thick outer cartoon stroke
                    if (d_panel > -12f)
                    {
                        pixelColor = Color.black;
                    }
                    else
                    {
                        pixelColor = baseColor;
                    }
                }

                colors[y * width + x] = pixelColor;
            }
        }

        tex.SetPixels(colors);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        }
        }
