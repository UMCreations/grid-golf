using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(HandcraftedLevelSO))]
public class HandcraftedLevelEditor : Editor
{
    private HandcraftedLevelSO level;
    private TileType selectedType = TileType.Standard;
    private int selectedPower = 1;

    private int minFill = 1;
    private int maxFill = 5;

    private bool includeIce = true;
    private bool includeSand = true;
    private bool includeBoost = true;

    private void OnEnable()
    {
        level = (HandcraftedLevelSO)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(20);
        GUILayout.Label("GRID DESIGNER", EditorStyles.boldLabel);

        // Auto-initialize if null
        if (level.tileTypes == null || level.tilePowers == null || level.tileTypes.Length != level.width * level.height)
        {
            EditorGUILayout.HelpBox("Grid needs initialization or resizing.", MessageType.Warning);
            if (GUILayout.Button("Initialize Grid"))
            {
                level.ResetLayout();
                EditorUtility.SetDirty(level);
            }
            return; // Don't try to draw the grid yet
        }

        if (GUILayout.Button("Reset/Resize Grid"))
        {
            if (EditorUtility.DisplayDialog("Reset Grid", "Are you sure you want to reset the grid layout?", "Yes", "No"))
            {
                level.ResetLayout();
                EditorUtility.SetDirty(level);
            }
        }

        GUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        selectedType = (TileType)EditorGUILayout.EnumPopup("Painting Type", selectedType);
        selectedPower = EditorGUILayout.IntField("Painting Power", selectedPower);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label("RANDOM FILL (Apply to power 0)", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        minFill = EditorGUILayout.IntField("Min", minFill);
        maxFill = EditorGUILayout.IntField("Max", maxFill);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("RANDOM FILL", GUILayout.Height(30)))
        {
            Undo.RecordObject(level, "Random Fill Handcrafted Level");
            for (int i = 0; i < level.tilePowers.Length; i++)
            {
                if (level.tilePowers[i] == 0)
                {
                    level.tilePowers[i] = Random.Range(minFill, maxFill + 1);
                }
            }
            EditorUtility.SetDirty(level);
        }

        GUILayout.Space(10);
        
        // Render Grid
        float buttonSize = 40f;
        
        for (int y = level.height - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < level.width; x++)
            {
                int index = y * level.width + x;
                if (index >= level.tileTypes.Length) continue;

                TileType currentType = level.tileTypes[index];
                int currentPower = level.tilePowers[index];

                // Visual cues for specific tiles
                Color originalColor = GUI.color;
                if (level.startPosition == new Vector2Int(x, y)) GUI.color = Color.green;
                else if (level.holePosition == new Vector2Int(x, y)) GUI.color = Color.red;
                else
                {
                    switch (currentType)
                    {
                        case TileType.Ice:   GUI.color = new Color(0.6f, 0.85f, 1f); break;
                        case TileType.Sand:  GUI.color = new Color(1f, 0.9f, 0.5f); break;
                        case TileType.Boost: GUI.color = new Color(0.5f, 1f, 0.5f); break;
                        case TileType.Water: GUI.color = new Color(0.2f, 0.4f, 1f); break;
                        case TileType.Wall:  GUI.color = Color.grey; break;
                    }
                }

                string label = $"{currentPower}\n{currentType.ToString().Substring(0, 1)}";
                if (GUILayout.Button(label, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                {
                    level.tileTypes[index] = selectedType;
                    level.tilePowers[index] = selectedPower;
                    
                    // Auto-assign start/hole if painting those types
                    if (selectedType == TileType.Start) level.startPosition = new Vector2Int(x, y);
                    if (selectedType == TileType.Hole)  level.holePosition = new Vector2Int(x, y);
                    
                    EditorUtility.SetDirty(level);
                }
                GUI.color = originalColor;
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
