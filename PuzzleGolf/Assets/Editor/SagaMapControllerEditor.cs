using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[Serializable]
public class SagaMapPathClipboardData
{
    public string marker;
    public float verticalSpacing;
    public float pathWidth;
    public float frequency;
    public SagaMapPathClipboardSegment[] segments;
}

[Serializable]
public class SagaMapPathClipboardSegment
{
    public string themeName;
    public Vector2[] pathPoints;
    public Vector2[] levelPositions;
}

[CustomEditor(typeof(SagaMapController))]
public class SagaMapControllerEditor : Editor
{
    private const string ClipboardMarker = "PuzzleGolf.SagaMapPaths.v1";

    public override void OnInspectorGUI()
    {
        SagaMapController controller = (SagaMapController)target;
        bool changed = DrawDefaultInspector();

        if (changed && IsLiveSceneController(controller))
            QueueLiveRefresh(controller);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Play Mode Path Transfer", EditorStyles.boldLabel);

        bool isLiveController = IsLiveSceneController(controller);
        using (new EditorGUI.DisabledScope(!isLiveController))
        {
            if (GUILayout.Button("Capture & Copy Live Layout"))
            {
                CopyPaths(controller);
                serializedObject.Update();
            }
        }

        bool hasPathClipboard = TryGetClipboard(out _);
        using (new EditorGUI.DisabledScope(!hasPathClipboard))
        {
            if (GUILayout.Button("Paste Layout from Clipboard to This Controller"))
                PastePaths(controller);
        }

        EditorGUILayout.HelpBox(
            GetInstructions(controller, isLiveController),
            MessageType.Info);
    }

    private static bool IsLiveSceneController(SagaMapController controller)
    {
        return Application.isPlaying && controller != null && controller.gameObject.scene.IsValid() && controller.isActiveAndEnabled;
    }

    private static string GetInstructions(SagaMapController controller, bool isLiveController)
    {
        if (isLiveController)
            return "Edit Path Points or Path Settings above; the map refreshes immediately. You can also move LevelNode_1, LevelNode_2, etc. under Content in Play Mode; the route follows their positions live. Capture & Copy Live Layout records every node position before you stop. Captured Level Positions override Path Points; clear that list to return to curve editing.";

        if (PrefabUtility.IsPartOfPrefabAsset(controller))
            return "Paste applies the copied layout to this prefab asset and saves it. Captured Level Positions override Path Points; clear a segment's list to return to curve editing. Background sprites are not included.";

        if (PrefabUtility.IsPartOfPrefabInstance(controller))
            return "Paste applies the copied layout as overrides on this prefab instance. To update the reusable prefab, select the prefab asset in Project and paste there. Clear a segment's Level Positions list to return to curve editing.";

        return "After Play Mode, paste the copied layout here and save the scene. To update a reusable prefab, select that prefab asset in Project first. Clear a segment's Level Positions list to return to curve editing.";
    }

    private static void QueueLiveRefresh(SagaMapController controller)
    {
        EditorApplication.delayCall -= controller.RefreshMapLayout;
        EditorApplication.delayCall += controller.RefreshMapLayout;
    }

    private static void CopyPaths(SagaMapController controller)
    {
        Dictionary<int, LevelNodeController> nodesByLevel = new Dictionary<int, LevelNodeController>();
        if (controller.contentContainer != null)
        {
            foreach (LevelNodeController node in controller.contentContainer.GetComponentsInChildren<LevelNodeController>(true))
            {
                if (node != null && node.LevelIndex > 0)
                    nodesByLevel[node.LevelIndex] = node;
            }
        }

        List<SagaMapPathClipboardSegment> segments = new List<SagaMapPathClipboardSegment>();
        bool capturedPositions = false;
        if (controller.segmentArt != null)
        {
            foreach (SagaMapSegmentArt art in controller.segmentArt)
            {
                if (art == null) continue;

                Vector2[] livePositions = CaptureSegmentPositions(controller, art, nodesByLevel);
                if (livePositions != null)
                {
                    art.levelPositions = livePositions;
                    capturedPositions = true;
                }

                segments.Add(new SagaMapPathClipboardSegment
                {
                    themeName = art.themeName,
                    pathPoints = art.pathPoints != null ? (Vector2[])art.pathPoints.Clone() : new Vector2[0],
                    levelPositions = art.levelPositions != null ? (Vector2[])art.levelPositions.Clone() : new Vector2[0]
                });
            }
        }

        SagaMapPathClipboardData data = new SagaMapPathClipboardData
        {
            marker = ClipboardMarker,
            verticalSpacing = controller.verticalSpacing,
            pathWidth = controller.pathWidth,
            frequency = controller.frequency,
            segments = segments.ToArray()
        };

        EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(data);
        if (capturedPositions)
            QueueLiveRefresh(controller);

        Debug.Log($"Copied the live Saga Map layout for {segments.Count} segments, including level positions. Stop Play Mode, select the prefab, then paste the layout from the Inspector.", controller);
    }

    private static Vector2[] CaptureSegmentPositions(
        SagaMapController controller,
        SagaMapSegmentArt art,
        Dictionary<int, LevelNodeController> nodesByLevel)
    {
        if (!IsLiveSceneController(controller) || string.IsNullOrEmpty(art.themeName)) return null;

        AdventureSegmentConfig segment = null;
        for (int level = 1; level <= LevelManager.MAX_LEVELS; level++)
        {
            AdventureSegmentConfig candidate = AdventureSegmentResolver.GetConfigForLevel(level);
            if (candidate != null && string.Equals(candidate.themeName, art.themeName, StringComparison.OrdinalIgnoreCase))
            {
                segment = candidate;
                break;
            }
        }

        if (segment == null) return null;

        Vector2[] positions = new Vector2[segment.endLevel - segment.startLevel + 1];
        for (int level = segment.startLevel; level <= segment.endLevel; level++)
        {
            if (!nodesByLevel.TryGetValue(level, out LevelNodeController node) ||
                !controller.TryGetNormalizedMapZonePosition(node, out positions[level - segment.startLevel]))
                return null;
        }

        return positions;
    }

    private static bool TryGetClipboard(out SagaMapPathClipboardData data)
    {
        data = null;
        string clipboard = EditorGUIUtility.systemCopyBuffer;
        if (string.IsNullOrEmpty(clipboard)) return false;

        try
        {
            data = JsonUtility.FromJson<SagaMapPathClipboardData>(clipboard);
            return data != null && data.marker == ClipboardMarker && data.segments != null;
        }
        catch (ArgumentException)
        {
            data = null;
            return false;
        }
    }

    private void PastePaths(SagaMapController controller)
    {
        if (!TryGetClipboard(out SagaMapPathClipboardData data)) return;

        int matches = CountMatchingSegments(controller, data);
        if (matches == 0)
        {
            Debug.LogWarning("No Saga Map segment names matched the paths in the clipboard.", controller);
            return;
        }

        Undo.RecordObject(controller, "Paste Saga Map Paths");
        int updated = ApplyPaths(controller, data);
        EditorUtility.SetDirty(controller);

        if (PrefabUtility.IsPartOfPrefabInstance(controller))
            PrefabUtility.RecordPrefabInstancePropertyModifications(controller);
        if (PrefabUtility.IsPartOfPrefabAsset(controller))
            AssetDatabase.SaveAssets();
        else
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);

        serializedObject.Update();
        if (IsLiveSceneController(controller))
            QueueLiveRefresh(controller);

        Debug.Log($"Pasted the layout for {updated} Saga Map segments. Background sprites were left unchanged.", controller);
    }

    private static int CountMatchingSegments(SagaMapController controller, SagaMapPathClipboardData data)
    {
        if (controller.segmentArt == null || data.segments == null) return 0;

        int matches = 0;
        foreach (SagaMapPathClipboardSegment entry in data.segments)
        {
            if (entry == null || string.IsNullOrEmpty(entry.themeName)) continue;
            foreach (SagaMapSegmentArt art in controller.segmentArt)
            {
                if (art != null && string.Equals(art.themeName, entry.themeName, StringComparison.OrdinalIgnoreCase))
                {
                    matches++;
                    break;
                }
            }
        }

        return matches;
    }

    private static int ApplyPaths(SagaMapController controller, SagaMapPathClipboardData data)
    {
        controller.verticalSpacing = data.verticalSpacing;
        controller.pathWidth = data.pathWidth;
        controller.frequency = data.frequency;

        int updated = 0;
        foreach (SagaMapPathClipboardSegment entry in data.segments)
        {
            if (entry == null || string.IsNullOrEmpty(entry.themeName)) continue;
            foreach (SagaMapSegmentArt art in controller.segmentArt)
            {
                if (art == null || !string.Equals(art.themeName, entry.themeName, StringComparison.OrdinalIgnoreCase))
                    continue;

                art.pathPoints = entry.pathPoints != null ? (Vector2[])entry.pathPoints.Clone() : new Vector2[0];
                art.levelPositions = entry.levelPositions != null ? (Vector2[])entry.levelPositions.Clone() : new Vector2[0];
                updated++;
                break;
            }
        }

        return updated;
    }
}
