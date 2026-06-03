using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

[InitializeOnLoad]
public static class SceneRecreator
{
    static SceneRecreator()
    {
        // Use EditorApplication.delayCall to ensure AssetDatabase and other systems are fully ready
        EditorApplication.delayCall += RecreateSceneIfNeeded;
    }

    private static void RecreateSceneIfNeeded()
    {
        string sceneDir = "Assets/Scenes";
        string scenePath = Path.Combine(sceneDir, "MainScene.unity");

        if (File.Exists(scenePath))
        {
            // Scene already exists, let's make sure it's in the build settings
            AddSceneToBuildSettings(scenePath);
            return;
        }

        Debug.Log("[SceneRecreator] MainScene.unity is missing! Recreating scene...");

        // 1. Ensure Directories exist
        if (!Directory.Exists(sceneDir)) Directory.CreateDirectory(sceneDir);
        if (!Directory.Exists("Assets/Prefabs")) Directory.CreateDirectory("Assets/Prefabs");
        if (!Directory.Exists("Assets/Charts")) Directory.CreateDirectory("Assets/Charts");
        if (!Directory.Exists("Assets/Audio")) Directory.CreateDirectory("Assets/Audio");

        // 2. Generate Sprites
        ProceduralAssetGenerator.GenerateAllSprites();

        // 3. Load Sprites for Setup
        Sprite solidSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UIPanelBase.png");
        if (solidSprite == null)
        {
            solidSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/NoteBlowTap.png");
        }

        // 4. Generate Wav Audio clips if missing
        GenerateWavAudioFiles();

        // 5. Create New Scene
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
            UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
            UnityEditor.SceneManagement.NewSceneMode.Single
        );

        // 6. Configure Camera
        GameObject cameraObj = GameObject.Find("Main Camera");
        if (cameraObj == null)
        {
            cameraObj = new GameObject("Main Camera");
            cameraObj.AddComponent<Camera>();
        }
        Camera mainCam = cameraObj.GetComponent<Camera>();
        mainCam.orthographic = true;
        mainCam.orthographicSize = 5f;
        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.backgroundColor = new Color(0.08f, 0.08f, 0.09f, 1.0f); // Charcoal dark background
        cameraObj.transform.position = new Vector3(0, 0, -10f);

        // Remove default Directional Light
        GameObject dirLight = GameObject.Find("Directional Light");
        if (dirLight != null)
        {
            Object.DestroyImmediate(dirLight);
        }

        // 7. Load or Create Note Prefabs
        string blowPrefabPath = "Assets/Prefabs/NoteBlow.prefab";
        string drawPrefabPath = "Assets/Prefabs/NoteDraw.prefab";
        GameObject blowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(blowPrefabPath);
        GameObject drawPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(drawPrefabPath);

        if (blowPrefab == null || drawPrefab == null)
        {
            // Note Blow Source
            GameObject noteBlowSource = new GameObject("NoteBlow_Source");
            var blowSr = noteBlowSource.AddComponent<SpriteRenderer>();
            blowSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/NoteBlowTap.png");
            blowSr.color = new Color(0.117f, 0.564f, 1.0f); // Neon Blue
            var blowNote = noteBlowSource.AddComponent<NoteObject>();
            blowNote.requiredState = PlayerController.BreathState.Blow;
            blowNote.targetHole = 1;
            blowNote.fallSpeed = 4f;

            // Note Draw Source
            GameObject noteDrawSource = new GameObject("NoteDraw_Source");
            var drawSr = noteDrawSource.AddComponent<SpriteRenderer>();
            drawSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/NoteDrawTap.png");
            drawSr.color = new Color(1.0f, 0.549f, 0.0f); // Warm Amber
            var drawNote = noteDrawSource.AddComponent<NoteObject>();
            drawNote.requiredState = PlayerController.BreathState.Draw;
            drawNote.targetHole = 1;
            drawNote.fallSpeed = 4f;

            blowPrefab = PrefabUtility.SaveAsPrefabAsset(noteBlowSource, blowPrefabPath);
            drawPrefab = PrefabUtility.SaveAsPrefabAsset(noteDrawSource, drawPrefabPath);

            Object.DestroyImmediate(noteBlowSource);
            Object.DestroyImmediate(noteDrawSource);
        }

        // 8. Create Player & Mouth Reticle
        GameObject playerObj = new GameObject("PlayerController");
        var playerController = playerObj.AddComponent<PlayerController>();

        GameObject reticleObj = new GameObject("MouthReticle");
        reticleObj.transform.parent = playerObj.transform;
        reticleObj.transform.position = new Vector3(0, -4.0f, 0);
        var reticleSr = reticleObj.AddComponent<SpriteRenderer>();
        reticleSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/MouthReticle.png");
        reticleObj.transform.localScale = new Vector3(1.2f, 0.5f, 1.0f);

        // Assign reticle onto PlayerController
        SerializedObject playerSO = new SerializedObject(playerController);
        playerSO.FindProperty("mouthReticle").objectReferenceValue = reticleObj.transform;
        playerSO.ApplyModifiedProperties();

        // 9. Create Audio Manager
        GameObject audioManagerObj = new GameObject("AudioManager");
        var audioManager = audioManagerObj.AddComponent<AudioManager>();

        GameObject musicSourceObj = new GameObject("MusicSource");
        musicSourceObj.transform.parent = audioManagerObj.transform;
        var musicSource = musicSourceObj.AddComponent<AudioSource>();

        SerializedObject audioSO = new SerializedObject(audioManager);
        audioSO.FindProperty("musicSource").objectReferenceValue = musicSource;
        audioSO.ApplyModifiedProperties();

        // 10. Create or Load Song Chart
        string chartPath = "Assets/Charts/MaryHadALittleLamb.asset";
        SongChart chart = AssetDatabase.LoadAssetAtPath<SongChart>(chartPath);
        if (chart == null)
        {
            chart = ScriptableObject.CreateInstance<SongChart>();
            chart.trackingTitle = "Mary Had a Little Lamb";
            chart.beatsPerMinute = 120f;
            chart.trackTimeline = new List<NoteData>
            {
                new NoteData { spawnTimeOffset = 1.0f, holeLaneIndex = 5, actionType = PlayerController.BreathState.Blow, holdLength = 0f },
                new NoteData { spawnTimeOffset = 2.0f, holeLaneIndex = 4, actionType = PlayerController.BreathState.Draw, holdLength = 0f },
                new NoteData { spawnTimeOffset = 3.0f, holeLaneIndex = 3, actionType = PlayerController.BreathState.Blow, holdLength = 0f },
                new NoteData { spawnTimeOffset = 4.0f, holeLaneIndex = 4, actionType = PlayerController.BreathState.Draw, holdLength = 0f },
                new NoteData { spawnTimeOffset = 5.0f, holeLaneIndex = 5, actionType = PlayerController.BreathState.Blow, holdLength = 0f },
                new NoteData { spawnTimeOffset = 6.0f, holeLaneIndex = 5, actionType = PlayerController.BreathState.Blow, holdLength = 0f },
                new NoteData { spawnTimeOffset = 7.0f, holeLaneIndex = 5, actionType = PlayerController.BreathState.Blow, holdLength = 1.0f }
            };
            AssetDatabase.CreateAsset(chart, chartPath);
            AssetDatabase.SaveAssets();
        }

        // 11. Create Rhythm Manager
        GameObject rhythmManagerObj = new GameObject("RhythmManager");
        var rhythmManager = rhythmManagerObj.AddComponent<RhythmManager>();

        SerializedObject rhythmSO = new SerializedObject(rhythmManager);
        rhythmSO.FindProperty("playerController").objectReferenceValue = playerController;
        rhythmSO.FindProperty("activeChart").objectReferenceValue = chart;
        rhythmSO.FindProperty("noteBlowPrefab").objectReferenceValue = blowPrefab;
        rhythmSO.FindProperty("noteDrawPrefab").objectReferenceValue = drawPrefab;
        rhythmSO.ApplyModifiedProperties();

        // 12. Create UI Canvas Hierarchy
        GameObject canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Event System
        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // Breath Panel
        GameObject breathPanel = new GameObject("BreathPanel");
        breathPanel.transform.parent = canvasObj.transform;
        var breathImage = breathPanel.AddComponent<UnityEngine.UI.Image>();
        breathImage.sprite = solidSprite;
        breathImage.type = UnityEngine.UI.Image.Type.Simple;
        breathImage.color = new Color(0.117f, 0.564f, 1.0f);
        
        RectTransform panelRT = breathPanel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.12f);
        panelRT.anchorMax = new Vector2(0.5f, 0.12f);
        panelRT.pivot = new Vector2(0.5f, 0f);
        panelRT.anchoredPosition = new Vector3(0, 50f, 0);
        panelRT.sizeDelta = new Vector2(400f, 100f);
        panelRT.localScale = Vector3.one;

        GameObject breathTextObj = new GameObject("BreathText");
        breathTextObj.transform.parent = breathPanel.transform;
        var breathText = breathTextObj.AddComponent<UnityEngine.UI.Text>();
        breathText.text = "BLOW";
        breathText.alignment = TextAnchor.MiddleCenter;
        breathText.fontSize = 45;
        breathText.color = Color.white;
        RectTransform textRT = breathTextObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.anchoredPosition = Vector2.zero;
        textRT.sizeDelta = Vector2.zero;
        textRT.localScale = Vector3.one;

        // Oxygen Slider
        GameObject sliderObj = new GameObject("OxygenSlider");
        sliderObj.transform.parent = canvasObj.transform;
        var slider = sliderObj.AddComponent<UnityEngine.UI.Slider>();
        
        RectTransform sliderRT = sliderObj.GetComponent<RectTransform>();
        sliderRT.anchorMin = new Vector2(0.5f, 0.05f);
        sliderRT.anchorMax = new Vector2(0.5f, 0.05f);
        sliderRT.pivot = new Vector2(0.5f, 0.5f);
        sliderRT.anchoredPosition = new Vector3(0, 0, 0);
        sliderRT.sizeDelta = new Vector2(600f, 30f);
        sliderRT.localScale = Vector3.one;

        // Background Slider Area
        GameObject sliderBg = new GameObject("Background");
        sliderBg.transform.parent = sliderObj.transform;
        var sliderBgImage = sliderBg.AddComponent<UnityEngine.UI.Image>();
        sliderBgImage.sprite = solidSprite;
        sliderBgImage.type = UnityEngine.UI.Image.Type.Simple;
        sliderBgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        RectTransform bgRT = sliderBg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.anchoredPosition = Vector2.zero;
        bgRT.sizeDelta = Vector2.zero;
        bgRT.localScale = Vector3.one;

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.parent = sliderObj.transform;
        RectTransform faRT = fillArea.AddComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0, 0.25f);
        faRT.anchorMax = new Vector2(1, 0.75f);
        faRT.anchoredPosition = Vector2.zero;
        faRT.sizeDelta = Vector2.zero;
        faRT.localScale = Vector3.one;

        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.parent = fillArea.transform;
        var fillImage = fill.AddComponent<UnityEngine.UI.Image>();
        fillImage.sprite = solidSprite;
        fillImage.type = UnityEngine.UI.Image.Type.Simple;
        fillImage.color = Color.green;
        RectTransform fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.anchoredPosition = Vector2.zero;
        fillRT.sizeDelta = Vector2.zero;
        fillRT.localScale = Vector3.one;

        slider.targetGraphic = fillImage;
        slider.fillRect = fillRT;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.5f;

        // Score & Combo Overlay
        GameObject scoreTextObj = new GameObject("ScoreText");
        scoreTextObj.transform.parent = canvasObj.transform;
        var scoreText = scoreTextObj.AddComponent<UnityEngine.UI.Text>();
        scoreText.text = "Score: 0";
        scoreText.alignment = TextAnchor.UpperLeft;
        scoreText.fontSize = 45;
        scoreText.color = Color.white;
        RectTransform scoreRT = scoreTextObj.GetComponent<RectTransform>();
        scoreRT.anchorMin = new Vector2(0.05f, 0.95f);
        scoreRT.anchorMax = new Vector2(0.05f, 0.95f);
        scoreRT.pivot = new Vector2(0, 1f);
        scoreRT.anchoredPosition = Vector3.zero;
        scoreRT.sizeDelta = new Vector2(500, 80);
        scoreRT.localScale = Vector3.one;

        GameObject comboTextObj = new GameObject("ComboText");
        comboTextObj.transform.parent = canvasObj.transform;
        var comboText = comboTextObj.AddComponent<UnityEngine.UI.Text>();
        comboText.text = "";
        comboText.alignment = TextAnchor.MiddleCenter;
        comboText.fontSize = 60;
        comboText.color = Color.yellow;
        RectTransform comboRT = comboTextObj.GetComponent<RectTransform>();
        comboRT.anchorMin = new Vector2(0.5f, 0.6f);
        comboRT.anchorMax = new Vector2(0.5f, 0.6f);
        comboRT.pivot = new Vector2(0.5f, 0.5f);
        comboRT.anchoredPosition = Vector3.zero;
        comboRT.sizeDelta = new Vector2(500, 80);
        comboRT.localScale = Vector3.one;

        // Stun Text Indicator
        GameObject stunTextObj = new GameObject("StunText");
        stunTextObj.transform.parent = canvasObj.transform;
        var stunIndicatorText = stunTextObj.AddComponent<UnityEngine.UI.Text>();
        stunIndicatorText.text = "STUNNED!";
        stunIndicatorText.alignment = TextAnchor.MiddleCenter;
        stunIndicatorText.fontSize = 55;
        stunIndicatorText.color = Color.red;
        stunIndicatorText.gameObject.SetActive(false);
        RectTransform stunRT = stunTextObj.GetComponent<RectTransform>();
        stunRT.anchorMin = new Vector2(0.5f, 0.5f);
        stunRT.anchorMax = new Vector2(0.5f, 0.5f);
        stunRT.pivot = new Vector2(0.5f, 0.5f);
        stunRT.anchoredPosition = new Vector3(0, 150f, 0);
        stunRT.sizeDelta = new Vector2(500, 80);
        stunRT.localScale = Vector3.one;

        // 13. Game UI Manager Creation
        GameObject uiManagerObj = new GameObject("GameUIManager");
        var uiManager = uiManagerObj.AddComponent<GameUIManager>();

        SerializedObject uiSO = new SerializedObject(uiManager);
        uiSO.FindProperty("playerController").objectReferenceValue = playerController;
        uiSO.FindProperty("rhythmManager").objectReferenceValue = rhythmManager;
        uiSO.FindProperty("oxygenSlider").objectReferenceValue = slider;
        uiSO.FindProperty("oxygenFillImage").objectReferenceValue = fillImage;
        uiSO.FindProperty("stunText").objectReferenceValue = stunIndicatorText;
        uiSO.FindProperty("statePanelImage").objectReferenceValue = breathImage;
        uiSO.FindProperty("stateText").objectReferenceValue = breathText;
        uiSO.FindProperty("scoreText").objectReferenceValue = scoreText;
        uiSO.FindProperty("comboText").objectReferenceValue = comboText;
        uiSO.ApplyModifiedProperties();

        // 14. Generate 10 Virtual Harmonica Lane Guidelines
        GameObject linesContainer = new GameObject("LaneGuidelines");
        linesContainer.transform.position = Vector3.zero;
        
        for (int i = 1; i <= 10; i++)
        {
            float lineX = i - 5.5f;
            GameObject lineObj = new GameObject($"Lane_{i}_Line");
            lineObj.transform.parent = linesContainer.transform;
            lineObj.transform.position = new Vector3(lineX, 0, 1f);

            var lineSr = lineObj.AddComponent<SpriteRenderer>();
            lineSr.sprite = solidSprite;
            lineSr.color = new Color(1f, 1f, 1f, 0.05f); // Very faint guideline
            lineObj.transform.localScale = new Vector3(0.08f, 10.0f, 1f);
        }

        // Add visual target baseline at Y = -4.0f
        GameObject targetLineObj = new GameObject("TargetHitZoneLine");
        targetLineObj.transform.position = new Vector3(0f, -4.0f, 0.5f);
        var targetSr = targetLineObj.AddComponent<SpriteRenderer>();
        targetSr.sprite = solidSprite;
        targetSr.color = new Color(0.2f, 0.8f, 0.2f, 0.5f); // Vibrant light green target bar
        targetLineObj.transform.localScale = new Vector3(10.0f, 0.15f, 1f);

        // 15. Create GameManager GameObject
        GameObject gameManagerObj = new GameObject("GameManager");
        var gameManager = gameManagerObj.AddComponent<GameManager>();

        // 16. Results Canvas Panel under Canvas (Translucent Dark Background)
        GameObject resultsPanelObj = new GameObject("ResultsCanvas");
        resultsPanelObj.transform.parent = canvasObj.transform;
        
        var resultsPanelImage = resultsPanelObj.AddComponent<UnityEngine.UI.Image>();
        resultsPanelImage.sprite = solidSprite;
        resultsPanelImage.type = UnityEngine.UI.Image.Type.Simple;
        resultsPanelImage.color = new Color(0.05f, 0.05f, 0.06f, 0.95f);

        RectTransform resultsPanelRT = resultsPanelObj.GetComponent<RectTransform>();
        resultsPanelRT.anchorMin = Vector2.zero;
        resultsPanelRT.anchorMax = Vector2.one;
        resultsPanelRT.pivot = new Vector2(0.5f, 0.5f);
        resultsPanelRT.anchoredPosition = Vector2.zero;
        resultsPanelRT.sizeDelta = Vector2.zero;
        resultsPanelRT.localScale = Vector3.one;

        // Title text: RESULTS
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.parent = resultsPanelObj.transform;
        var titleText = titleObj.AddComponent<UnityEngine.UI.Text>();
        titleText.text = "RESULTS";
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontSize = 80;
        titleText.color = Color.white;
        RectTransform titleRT = titleObj.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 0.8f);
        titleRT.anchorMax = new Vector2(0.5f, 0.8f);
        titleRT.pivot = new Vector2(0.5f, 0.5f);
        titleRT.anchoredPosition = Vector2.zero;
        titleRT.sizeDelta = new Vector2(600, 100);
        titleRT.localScale = Vector3.one;

        // Score text
        GameObject resultsScoreObj = new GameObject("FinalScoreText");
        resultsScoreObj.transform.parent = resultsPanelObj.transform;
        var resultsScoreText = resultsScoreObj.AddComponent<UnityEngine.UI.Text>();
        resultsScoreText.text = "Final Score: 0";
        resultsScoreText.alignment = TextAnchor.MiddleCenter;
        resultsScoreText.fontSize = 45;
        resultsScoreText.color = Color.white;
        RectTransform resultsScoreRT = resultsScoreObj.GetComponent<RectTransform>();
        resultsScoreRT.anchorMin = new Vector2(0.5f, 0.65f);
        resultsScoreRT.anchorMax = new Vector2(0.5f, 0.65f);
        resultsScoreRT.pivot = new Vector2(0.5f, 0.5f);
        resultsScoreRT.anchoredPosition = Vector2.zero;
        resultsScoreRT.sizeDelta = new Vector2(600, 60);
        resultsScoreRT.localScale = Vector3.one;

        // Max Combo text
        GameObject resultsMaxComboObj = new GameObject("MaxComboText");
        resultsMaxComboObj.transform.parent = resultsPanelObj.transform;
        var resultsMaxComboText = resultsMaxComboObj.AddComponent<UnityEngine.UI.Text>();
        resultsMaxComboText.text = "Max Combo: 0";
        resultsMaxComboText.alignment = TextAnchor.MiddleCenter;
        resultsMaxComboText.fontSize = 45;
        resultsMaxComboText.color = Color.white;
        RectTransform resultsMaxComboRT = resultsMaxComboObj.GetComponent<RectTransform>();
        resultsMaxComboRT.anchorMin = new Vector2(0.5f, 0.55f);
        resultsMaxComboRT.anchorMax = new Vector2(0.5f, 0.55f);
        resultsMaxComboRT.pivot = new Vector2(0.5f, 0.5f);
        resultsMaxComboRT.anchoredPosition = Vector2.zero;
        resultsMaxComboRT.sizeDelta = new Vector2(600, 60);
        resultsMaxComboRT.localScale = Vector3.one;

        // Grade text
        GameObject gradeObj = new GameObject("GradeText");
        gradeObj.transform.parent = resultsPanelObj.transform;
        var gradeText = gradeObj.AddComponent<UnityEngine.UI.Text>();
        gradeText.text = "Grade: SSS\n(Sucking & Blowing Superstar)";
        gradeText.alignment = TextAnchor.MiddleCenter;
        gradeText.fontSize = 40;
        gradeText.color = Color.yellow;
        RectTransform gradeRT = gradeObj.GetComponent<RectTransform>();
        gradeRT.anchorMin = new Vector2(0.5f, 0.42f);
        gradeRT.anchorMax = new Vector2(0.5f, 0.42f);
        gradeRT.pivot = new Vector2(0.5f, 0.5f);
        gradeRT.anchoredPosition = Vector2.zero;
        gradeRT.sizeDelta = new Vector2(800, 120);
        gradeRT.localScale = Vector3.one;

        // Retry Button
        GameObject retryBtnObj = new GameObject("RetryButton");
        retryBtnObj.transform.parent = resultsPanelObj.transform;
        var retryBtn = retryBtnObj.AddComponent<UnityEngine.UI.Button>();
        var retryImg = retryBtnObj.AddComponent<UnityEngine.UI.Image>();
        retryImg.sprite = solidSprite;
        retryImg.type = UnityEngine.UI.Image.Type.Simple;
        retryImg.color = new Color(0.12f, 0.6f, 0.2f, 1f); // Vibrant Green button

        RectTransform retryRT = retryBtnObj.GetComponent<RectTransform>();
        retryRT.anchorMin = new Vector2(0.5f, 0.25f);
        retryRT.anchorMax = new Vector2(0.5f, 0.25f);
        retryRT.pivot = new Vector2(0.5f, 0.5f);
        retryRT.anchoredPosition = new Vector3(-130f, 0, 0);
        retryRT.sizeDelta = new Vector2(200f, 60f);
        retryRT.localScale = Vector3.one;

        GameObject retryTextObj = new GameObject("Text");
        retryTextObj.transform.parent = retryBtnObj.transform;
        var retryText = retryTextObj.AddComponent<UnityEngine.UI.Text>();
        retryText.text = "Retry";
        retryText.alignment = TextAnchor.MiddleCenter;
        retryText.fontSize = 32;
        retryText.color = Color.white;
        RectTransform rtRT = retryTextObj.GetComponent<RectTransform>();
        rtRT.anchorMin = Vector2.zero;
        rtRT.anchorMax = Vector2.one;
        rtRT.anchoredPosition = Vector2.zero;
        rtRT.sizeDelta = Vector2.zero;
        rtRT.localScale = Vector3.one;

        // Quit Button
        GameObject quitBtnObj = new GameObject("QuitButton");
        quitBtnObj.transform.parent = resultsPanelObj.transform;
        var quitBtn = quitBtnObj.AddComponent<UnityEngine.UI.Button>();
        var quitImg = quitBtnObj.AddComponent<UnityEngine.UI.Image>();
        quitImg.sprite = solidSprite;
        quitImg.type = UnityEngine.UI.Image.Type.Simple;
        quitImg.color = new Color(0.7f, 0.15f, 0.15f, 1f); // Vibrant Red button

        RectTransform quitRT = quitBtnObj.GetComponent<RectTransform>();
        quitRT.anchorMin = new Vector2(0.5f, 0.25f);
        quitRT.anchorMax = new Vector2(0.5f, 0.25f);
        quitRT.pivot = new Vector2(0.5f, 0.5f);
        quitRT.anchoredPosition = new Vector3(130f, 0, 0);
        quitRT.sizeDelta = new Vector2(200f, 60f);
        quitRT.localScale = Vector3.one;

        GameObject quitTextObj = new GameObject("Text");
        quitTextObj.transform.parent = quitBtnObj.transform;
        var quitText = quitTextObj.AddComponent<UnityEngine.UI.Text>();
        quitText.text = "Quit";
        quitText.alignment = TextAnchor.MiddleCenter;
        quitText.fontSize = 32;
        quitText.color = Color.white;
        RectTransform qtRT = quitTextObj.GetComponent<RectTransform>();
        qtRT.anchorMin = Vector2.zero;
        qtRT.anchorMax = Vector2.one;
        qtRT.anchoredPosition = Vector2.zero;
        qtRT.sizeDelta = Vector2.zero;
        qtRT.localScale = Vector3.one;

        // Wire Button Click Handlers
        UnityEditor.Events.UnityEventTools.AddPersistentListener(retryBtn.onClick, gameManager.RetryGame);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(quitBtn.onClick, gameManager.QuitGame);

        // Bind GameManager References
        SerializedObject managerSO = new SerializedObject(gameManager);
        managerSO.FindProperty("resultsCanvas").objectReferenceValue = resultsPanelObj;
        managerSO.FindProperty("finalScoreText").objectReferenceValue = resultsScoreText;
        managerSO.FindProperty("maxComboText").objectReferenceValue = resultsMaxComboText;
        managerSO.FindProperty("gradeText").objectReferenceValue = gradeText;
        managerSO.ApplyModifiedProperties();

        // Hide Results Panel initially
        resultsPanelObj.SetActive(false);

        // 17. Bind fonts to all UI text components
        Font defaultFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
        if (defaultFont == null)
        {
            defaultFont = AssetDatabase.LoadAssetAtPath<Font>("Packages/com.unity.searcher/Editor/Resources/FlatSkin/Font/Roboto-Regular.ttf");
        }
        if (defaultFont != null)
        {
            var allTexts = Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var txt in allTexts)
            {
                txt.font = defaultFont;
                EditorUtility.SetDirty(txt);
            }
        }

        // 18. Add CameraShake component to Camera
        var cameraShake = cameraObj.AddComponent<CameraShake>();
        SerializedObject shakeSO = new SerializedObject(cameraShake);
        shakeSO.FindProperty("playerController").objectReferenceValue = playerController;
        shakeSO.ApplyModifiedProperties();

        // 19. Add Bootstrapper
        var bootstrapper = playerObj.AddComponent<PlayTestBootstrapper>();

        // 20. Save the scene
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("[SceneRecreator] MainScene.unity has been successfully created, populated, and saved!");
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool alreadyAdded = false;
        foreach (var s in buildScenes)
        {
            if (s.path == scenePath)
            {
                alreadyAdded = true;
                break;
            }
        }

        if (!alreadyAdded)
        {
            buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
            Debug.Log($"[SceneRecreator] Added scene to Build Settings: {scenePath}");
        }
    }

    private static void GenerateWavAudioFiles()
    {
        string audioDir = "Assets/Audio";
        float[] blowFreqs = { 261.63f, 329.63f, 392.00f, 523.25f, 659.25f, 783.99f, 1046.50f, 1318.51f, 1567.98f, 2093.00f };
        float[] drawFreqs = { 293.66f, 392.00f, 493.88f, 587.33f, 698.46f, 880.00f, 987.77f, 1174.66f, 1396.91f, 1760.00f };

        // Generate Blow
        for (int i = 0; i < 10; i++)
        {
            string path = Path.Combine(audioDir, "Harmonica_Blow_" + (i + 1) + ".wav");
            if (!File.Exists(path))
            {
                float[] samples = GenerateHarmonicaSamples(blowFreqs[i], 1.2f);
                byte[] bytes = CreateWavBytes(samples, 44100);
                File.WriteAllBytes(path, bytes);
            }
        }

        // Generate Draw
        for (int i = 0; i < 10; i++)
        {
            string path = Path.Combine(audioDir, "Harmonica_Draw_" + (i + 1) + ".wav");
            if (!File.Exists(path))
            {
                float[] samples = GenerateHarmonicaSamples(drawFreqs[i], 1.2f);
                byte[] bytes = CreateWavBytes(samples, 44100);
                File.WriteAllBytes(path, bytes);
            }
        }

        // Generate Spit air rustle
        string spitPath = Path.Combine(audioDir, "spit_air_rustle.wav");
        if (!File.Exists(spitPath))
        {
            float[] samples = GenerateSpitAirSamples(0.35f);
            byte[] bytes = CreateWavBytes(samples, 44100);
            File.WriteAllBytes(spitPath, bytes);
        }

        // Generate BGM
        string bgmPath = Path.Combine(audioDir, "MaryHadALittleLamb_BGM.wav");
        if (!File.Exists(bgmPath))
        {
            float[] samples = GenerateMaryBgmSamples();
            byte[] bytes = CreateWavBytes(samples, 44100);
            File.WriteAllBytes(bgmPath, bytes);
        }

        AssetDatabase.Refresh();
    }

    private static float[] GenerateHarmonicaSamples(float freq, float duration)
    {
        int sampleRate = 44100;
        int numSamples = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[numSamples];

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = 1.0f;
            float attack = 0.05f;
            float release = 0.15f;

            if (t < attack)
            {
                envelope = t / attack;
            }
            else if (t > duration - release)
            {
                envelope = (duration - t) / release;
            }

            float vibrato = 1.0f + Mathf.Sin(t * 5.5f * Mathf.PI * 2f) * 0.005f;
            float tremolo = 1.0f - 0.15f + Mathf.Sin(t * 6.0f * Mathf.PI * 2f) * 0.15f;
            float angle = t * freq * vibrato * Mathf.PI * 2f;

            float wave = 0f;
            wave += Mathf.Sin(angle);
            wave += Mathf.Sin(angle * 2.0f) * 0.45f;
            wave += Mathf.Sin(angle * 3.0f) * 0.65f;
            wave += Mathf.Sin(angle * 4.0f) * 0.20f;
            wave += Mathf.Sin(angle * 5.0f) * 0.35f;

            wave /= 2.65f;

            float noise = (Random.value * 2f - 1f) * 0.02f;
            wave += noise;

            samples[i] = wave * envelope * tremolo * 0.6f;
        }

        return samples;
    }

    private static float[] GenerateSpitAirSamples(float duration)
    {
        int sampleRate = 44100;
        int numSamples = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[numSamples];

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = 1f;

            if (t < 0.02f) envelope = t / 0.02f;
            else envelope = (duration - t) / (duration - 0.02f);

            float noise = (Random.value * 2f - 1f);
            samples[i] = noise * envelope * 0.18f;
        }

        return samples;
    }

    private static float[] GenerateMaryBgmSamples()
    {
        int sampleRate = 44100;
        float duration = 12f;
        int numSamples = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[numSamples];

        float[] notes = { 329.63f, 293.66f, 261.63f, 293.66f, 329.63f, 329.63f, 329.63f };
        float[] times = { 1.0f, 1.5f, 2.0f, 2.5f, 3.0f, 3.5f, 4.0f };
        float[] lengths = { 0.4f, 0.4f, 0.4f, 0.4f, 0.4f, 0.4f, 0.9f };

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float bass = 0f;

            if (t >= 1.0f && t < 12.0f)
            {
                float freq1 = 130.81f; // C3
                float freq2 = 164.81f; // E3
                float freq3 = 196.00f; // G3

                if ((t >= 1.5f && t < 2.0f) || (t >= 2.5f && t < 3.0f))
                {
                    freq1 = 98.00f;  // G2
                    freq2 = 146.83f; // D3
                    freq3 = 196.00f; // G3
                }

                bass += Mathf.Sin(t * freq1 * Mathf.PI * 2f) * 0.25f;
                bass += Mathf.Sin(t * freq2 * Mathf.PI * 2f) * 0.15f;
                bass += Mathf.Sin(t * freq3 * Mathf.PI * 2f) * 0.15f;
            }

            float melody = 0f;
            for (int n = 0; n < notes.Length; n++)
            {
                float noteStart = times[n];
                float noteLen = lengths[n];

                if (t >= noteStart && t < noteStart + noteLen)
                {
                    float noteT = t - noteStart;
                    float env = Mathf.Sin(noteT / noteLen * Mathf.PI);
                    melody += Mathf.Sin(t * notes[n] * Mathf.PI * 2f) * 0.2f * env;
                }
            }

            samples[i] = (bass + melody) * 0.4f;
        }

        return samples;
    }

    private static byte[] CreateWavBytes(float[] samples, int sampleRate)
    {
        using (var stream = new System.IO.MemoryStream())
        {
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + samples.Length * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

                writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(sampleRate);
                writer.Write(sampleRate * 2);
                writer.Write((short)2);
                writer.Write((short)16);

                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(samples.Length * 2);

                for (int i = 0; i < samples.Length; i++)
                {
                    short val = (short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767);
                    writer.Write(val);
                }
            }
            return stream.ToArray();
        }
    }
}
