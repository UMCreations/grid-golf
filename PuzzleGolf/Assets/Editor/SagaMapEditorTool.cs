using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class SagaMapEditorTool : EditorWindow
{
    private GameObject nodePrefab;
    private int totalLevels = 100;
    private float verticalSpacing = 200f;
    private float pathWidth = 300f;
    private float frequency = 0.1f;

    [MenuItem("Tools/Saga Map/Saga Map Setup Tool")]
    public static void ShowWindow()
    {
        GetWindow<SagaMapEditorTool>("Saga Map Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Saga Map UI Setup", EditorStyles.boldLabel);
        
        nodePrefab = (GameObject)EditorGUILayout.ObjectField("Level Node Prefab", nodePrefab, typeof(GameObject), false);
        totalLevels = EditorGUILayout.IntField("Total Levels", totalLevels);
        verticalSpacing = EditorGUILayout.FloatField("Vertical Spacing", verticalSpacing);
        pathWidth = EditorGUILayout.FloatField("Path Width (Amplitude)", pathWidth);
        frequency = EditorGUILayout.FloatField("Path Frequency", frequency);

        if (GUILayout.Button("Create Saga Map Canvas"))
        {
            CreateSagaMapCanvas();
        }

        if (GUILayout.Button("Generate Level Nodes (In Selection)"))
        {
            GenerateNodesInSelection();
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Create Basic Level Node Template"))
        {
            CreateLevelNodeTemplate();
        }
    }

    private void CreateSagaMapCanvas()
    {
        // 1. Create Canvas
        GameObject canvasGO = new GameObject("SagaMap_Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        // 2. Add SagaMapController
        SagaMapController controller = canvasGO.AddComponent<SagaMapController>();

        // 3. Create Background
        GameObject bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(canvasGO.transform, false);
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgGO.GetComponent<Image>().color = new Color(0.1f, 0.15f, 0.2f); // Dark blueish

        // 4. Create ScrollView
        GameObject scrollViewGO = new GameObject("SagaScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollViewGO.transform.SetParent(canvasGO.transform, false);
        RectTransform scrollRectTransform = scrollViewGO.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0.1f, 0.1f);
        scrollRectTransform.anchorMax = new Vector2(0.9f, 0.9f);
        scrollRectTransform.sizeDelta = Vector2.zero;

        ScrollRect scrollRect = scrollViewGO.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        // 5. Create Viewport
        GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportGO.transform.SetParent(scrollViewGO.transform, false);
        RectTransform viewRect = viewportGO.GetComponent<RectTransform>();
        viewRect.anchorMin = Vector2.zero;
        viewRect.anchorMax = Vector2.one;
        viewRect.sizeDelta = Vector2.zero;
        viewportGO.GetComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = viewRect;

        // 6. Create Content
        GameObject contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(viewportGO.transform, false);
        RectTransform contentRect = contentGO.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero; // Force reset position to top
        contentRect.sizeDelta = new Vector2(0, totalLevels * verticalSpacing);
        scrollRect.content = contentRect;

        // 7. Create Back Button
        GameObject backBtnGO = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
        backBtnGO.transform.SetParent(canvasGO.transform, false);
        RectTransform backRect = backBtnGO.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0, 1);
        backRect.anchorMax = new Vector2(0, 1);
        backRect.pivot = new Vector2(0, 1);
        backRect.anchoredPosition = new Vector2(50, -50);
        backRect.sizeDelta = new Vector2(160, 60);
        
        // Add text to button
        GameObject btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTextGO.transform.SetParent(backBtnGO.transform, false);
        TextMeshProUGUI btnText = btnTextGO.GetComponent<TextMeshProUGUI>();
        btnText.text = "BACK";
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.fontSize = 24;
        btnText.color = Color.white;
        btnText.rectTransform.sizeDelta = Vector2.zero;
        btnText.rectTransform.anchorMin = Vector2.zero;
        btnText.rectTransform.anchorMax = Vector2.one;

        // Link references to controller
        controller.contentContainer = contentRect;
        controller.scrollRect = scrollRect;
        controller.backButton = backBtnGO.GetComponent<Button>();
        if (nodePrefab != null) controller.nodePrefab = nodePrefab.GetComponent<LevelNodeController>();

        Selection.activeGameObject = canvasGO;
        Debug.Log("Saga Map Canvas created and setup successfully!");
    }

    private void GenerateNodesInSelection()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null || selected.GetComponent<RectTransform>() == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select the 'Content' RectTransform of your Saga Map.", "OK");
            return;
        }

        if (nodePrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign a Level Node Prefab first.", "OK");
            return;
        }

        // Clear existing children?
        if (EditorUtility.DisplayDialog("Confirm", "Do you want to clear existing children in " + selected.name + " before generating?", "Yes", "No"))
        {
            for (int i = selected.transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(selected.transform.GetChild(i).gameObject);
            }
        }

        RectTransform contentRect = selected.GetComponent<RectTransform>();
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, totalLevels * verticalSpacing);
        contentRect.pivot = new Vector2(0.5f, 1f); // Ensure pivot is at top

        for (int i = 1; i <= totalLevels; i++)
        {
            GameObject node = (GameObject)PrefabUtility.InstantiatePrefab(nodePrefab, selected.transform);
            RectTransform nodeRect = node.GetComponent<RectTransform>();
            nodeRect.anchorMin = new Vector2(0.5f, 1f);
            nodeRect.anchorMax = new Vector2(0.5f, 1f);
            nodeRect.pivot = new Vector2(0.5f, 0.5f);
            
            // Calculate Saga Path Position (Sinusoidal)
            float xPos = Mathf.Sin(i * frequency) * pathWidth;
            float yPos = -((i - 1) * verticalSpacing) - (verticalSpacing * 0.5f);

            nodeRect.anchoredPosition = new Vector2(xPos, yPos);
            node.name = $"LevelNode_{i}";

            LevelNodeController nodeCtrl = node.GetComponent<LevelNodeController>();
            if (nodeCtrl != null)
            {
                // In Editor, we can set default labels
                SerializedObject so = new SerializedObject(nodeCtrl);
                var textProp = so.FindProperty("levelNumberText");
                if (textProp != null && textProp.objectReferenceValue != null)
                {
                    ((TextMeshProUGUI)textProp.objectReferenceValue).text = i.ToString();
                }
                so.ApplyModifiedProperties();
            }
        }

        EditorUtility.SetDirty(selected);
        Debug.Log($"Generated {totalLevels} nodes in {selected.name} with Saga Path layout.");
    }

    private void CreateLevelNodeTemplate()
    {
        // 1. Create Base Object
        GameObject nodeGO = new GameObject("LevelNode_Template", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LevelNodeController));
        RectTransform nodeRect = nodeGO.GetComponent<RectTransform>();
        nodeRect.sizeDelta = new Vector2(100, 100);

        LevelNodeController controller = nodeGO.GetComponent<LevelNodeController>();
        controller.backgroundImage = nodeGO.GetComponent<Image>();
        controller.nodeButton = nodeGO.GetComponent<Button>();

        // 2. Add Level Number Text
        GameObject textGO = new GameObject("NumberText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(nodeGO.transform, false);
        TextMeshProUGUI tmpText = textGO.GetComponent<TextMeshProUGUI>();
        tmpText.text = "0";
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.fontSize = 32;
        tmpText.color = Color.black;
        tmpText.rectTransform.anchorMin = Vector2.zero;
        tmpText.rectTransform.anchorMax = Vector2.one;
        tmpText.rectTransform.sizeDelta = Vector2.zero;
        controller.levelNumberText = tmpText;

        // 3. Add Locked Icon
        GameObject lockedGO = new GameObject("LockedIcon", typeof(RectTransform), typeof(Image));
        lockedGO.transform.SetParent(nodeGO.transform, false);
        RectTransform lockedRect = lockedGO.GetComponent<RectTransform>();
        lockedRect.sizeDelta = new Vector2(40, 40);
        lockedRect.anchoredPosition = new Vector2(0, 0);
        lockedGO.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        controller.lockedIcon = lockedGO;
        lockedGO.SetActive(false);

        // 4. Add Star Container
        GameObject starsGO = new GameObject("Stars", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        starsGO.transform.SetParent(nodeGO.transform, false);
        RectTransform starsRect = starsGO.GetComponent<RectTransform>();
        starsRect.anchorMin = new Vector2(0, 0);
        starsRect.anchorMax = new Vector2(1, 0);
        starsRect.pivot = new Vector2(0.5f, 0);
        starsRect.anchoredPosition = new Vector2(0, -30);
        starsRect.sizeDelta = new Vector2(0, 30);
        
        HorizontalLayoutGroup layout = starsGO.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;
        layout.spacing = 5;

        controller.starContainer = starsGO;
        controller.stars = new Image[3];

        for (int i = 0; i < 3; i++)
        {
            GameObject star = new GameObject($"Star_{i}", typeof(RectTransform), typeof(Image));
            star.transform.SetParent(starsGO.transform, false);
            star.GetComponent<RectTransform>().sizeDelta = new Vector2(25, 25);
            controller.stars[i] = star.GetComponent<Image>();
        }

        Selection.activeGameObject = nodeGO;
        Debug.Log("Created a basic Level Node Template. Please customize visuals and save it as a prefab!");
    }
}
