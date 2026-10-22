using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LevelBuilderWindow : EditorWindow
{
    private enum BUILDER_MODE
    {
        SELECT,
        PAINT,
        ERASE,
    }

    private enum DUPLICATE_MODE
    {
        PREVENT_ALL,
        SAME_PREFAB_ONLY,
        ALLOW_OVERLAP,
    }

    [SerializeField] private GameObject activePrefab;
    [SerializeField] private BUILDER_MODE mode = BUILDER_MODE.SELECT;
    [SerializeField] private DUPLICATE_MODE duplicateMode = DUPLICATE_MODE.SAME_PREFAB_ONLY;

    [SerializeField] private float gridSize = 1f;
    [SerializeField] private int rotationIndex = 0;
    [SerializeField] private float placementHeight = 0f;

    private const string RootName = "LevelGeometry";

    private Transform levelRoot;

    private bool isDragging;
    private Vector2Int lastCell;
    private int undoGroup;

    private readonly HashSet<Vector2Int> processedCells = new HashSet<Vector2Int>();

    [MenuItem("GANDT/Level Builder")]
    public static void Open()
    {
        GetWindow<LevelBuilderWindow>("GANDT Level Builder");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        EndStroke();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("GANDT Level Builder", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.Space();

        activePrefab = (GameObject)EditorGUILayout.ObjectField("Prefab", activePrefab, typeof(GameObject), false);
        gridSize = Mathf.Max(0.1f, EditorGUILayout.FloatField("Grid Size", gridSize));
        placementHeight = EditorGUILayout.FloatField("Placement Height", placementHeight);
        EditorGUILayout.Space();

        mode = (BUILDER_MODE)GUILayout.Toolbar((int)mode, new string[] { "Select", "Paint", "Erase" });
        duplicateMode = (DUPLICATE_MODE)EditorGUILayout.EnumPopup("Duplicate Placement", duplicateMode);
        
        if (EditorGUI.EndChangeCheck())
        {
            EndStroke();
            SceneView.RepaintAll();
        }
        EditorGUILayout.Space();

        if (GUILayout.Button("Rotate 90 degree"))
        {
            rotationIndex = (rotationIndex + 1) % 4;
            SceneView.RepaintAll();
        }
        EditorGUILayout.LabelField("Rotation", $"{rotationIndex * 90}");
        EditorGUILayout.Space();

        string description = mode switch
        {
            BUILDER_MODE.SELECT => "Normal Scene View controls",
            BUILDER_MODE.PAINT =>
            "Left Drag: Paint prefabs\n" +
            "Alt + Drag: Navigate Scene View",
            BUILDER_MODE.ERASE =>
            "Left Drag: Erase builder objects\n" +
            "Alt + Drag: Navigate Scene View",
            _ => ""
        };

        EditorGUILayout.HelpBox(description, MessageType.Info);
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (mode == BUILDER_MODE.SELECT)
        {
            return;
        }

        Event e = Event.current;

        if (e.rawType == EventType.MouseUp && e.button == 0 && isDragging)
        {
            EndStroke();
            e.Use();
            return;
        }

        if (e.alt || e.button == 1 || e.button == 2)
        {
            return;
        }    

        if (mode == BUILDER_MODE.PAINT && activePrefab == null)
        {
            return;
        }

        // 수정 중엔 씬 내 오브젝트 선택 방지
        if (e.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }

        if (!TryGetMouseCell(e.mousePosition, out Vector2Int cell))
        {
            return;
        }

        Vector3 position = CellToWorld(cell);

        DrawPreview(position);

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            BeginStroke(cell);
            e.Use();
        }
        else if (e.type == EventType.MouseDrag && e.button == 0 && isDragging)
        {
            ProcessLine(lastCell, cell);
            lastCell = cell;
            e.Use();
        }

        if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
        {
            sceneView.Repaint();
        }
    }

    private bool TryGetMouseCell(Vector2 mousePosition, out Vector2Int cell)
    {
        cell = default;

        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);

        Plane plane = new Plane(Vector3.up, new Vector3(0f, placementHeight, 0f));

        if (!plane.Raycast(ray, out float distance))
        {
            return false;
        }

        Vector3 point = ray.GetPoint(distance);

        cell = new Vector2Int(Mathf.RoundToInt(point.x / gridSize), Mathf.RoundToInt(point.z / gridSize));

        return true;
    }

    private Vector3 CellToWorld(Vector2Int cell)
    {
        return new Vector3(cell.x * gridSize, placementHeight, cell.y * gridSize);
    }

    private Quaternion GetRotation()
    {
        return Quaternion.Euler(0f, rotationIndex * 90f, 0f);
    }

    private Bounds GetPrefabBounds()
    {
        Renderer[] renderers =
            activePrefab.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            return new Bounds(
                activePrefab.transform.position,
                Vector3.one * gridSize
            );
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private void DrawPreview(Vector3 position)
    {
        Handles.color = mode == BUILDER_MODE.PAINT ? Color.green : Color.red;

        if (mode == BUILDER_MODE.ERASE)
        {
            Handles.DrawWireCube(position, new Vector3(gridSize, 0.05f, gridSize));
            return;
        }

        Bounds bounds = GetPrefabBounds();

        // Convert prefab bounds center to a pivot-relative offset.
        Vector3 offset = bounds.center - activePrefab.transform.position;

        Quaternion rotation = GetRotation();

        Vector3 center = position + rotation * offset;

        Matrix4x4 previousMatrix = Handles.matrix;

        Handles.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);

        Handles.DrawWireCube(Vector3.zero, bounds.size);

        Handles.matrix = previousMatrix;
    }

    private void BeginStroke(Vector2Int cell)
    {
        isDragging = true;
        lastCell = cell;

        processedCells.Clear();

        Undo.IncrementCurrentGroup();
        undoGroup = Undo.GetCurrentGroup();

        Undo.SetCurrentGroupName(mode == BUILDER_MODE.PAINT ? "GANDT Paint" : "GANDT Erase");

        ProcessCell(cell);
    }

    private void EndStroke()
    {
        if (!isDragging)
        {
            return;
        }

        isDragging = false;
        processedCells.Clear();

        Undo.CollapseUndoOperations(undoGroup);
    }

    private void ProcessLine(Vector2Int from, Vector2Int to)
    {
        // Fill skipped cells during fast mouse movement.
        int dx = Mathf.Abs(to.x - from.x);
        int dy = Mathf.Abs(to.y - from.y);

        int steps = Mathf.Max(dx, dy);

        if (steps == 0)
        {
            ProcessCell(to);
            return;
        }

        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;

            Vector2Int cell = new Vector2Int(
                Mathf.RoundToInt(Mathf.Lerp(from.x, to.x, t)),
                Mathf.RoundToInt(Mathf.Lerp(from.y, to.y, t))
            );

            ProcessCell(cell);
        }
    }

    private void ProcessCell(Vector2Int cell)
    {
        if (!processedCells.Add(cell))
        {
            return;
        }

        switch (mode)
        {
            case BUILDER_MODE.PAINT:
                PaintCell(cell);
                break;
            case BUILDER_MODE.ERASE:
                EraseCell(cell);
                break;
        }
    }

    private Transform GetLevelRoot(bool createIfMissing)
    {
        if (levelRoot != null)
        {
            return levelRoot;
        }

        GameObject rootObject = GameObject.Find(RootName);

        if (rootObject == null && createIfMissing)
        {
            rootObject = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(rootObject, "Create GANDT Level Root");
        }

        if (rootObject != null)
        {
            levelRoot = rootObject.transform;
        }
        return levelRoot;
    }

    private GameObject FindObjectAtCell(Vector2Int cell, GameObject prefabFilter = null)
    {
        Transform root = GetLevelRoot(false);

        if (root == null)
        {
            return null;
        }

        Vector3 target = CellToWorld(cell);
        const float tolerance = 0.001f;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            Vector3 position = child.position;

            bool samePosition =
                Mathf.Abs(position.x - target.x) <= tolerance &&
                Mathf.Abs(position.z - target.z) <= tolerance &&
                Mathf.Abs(position.y - target.y) <= tolerance;

            if (!samePosition)
            {
                continue;
            }

            if (prefabFilter != null)
            {
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject);
            
                if (source != prefabFilter)
                {
                    continue;
                }
            }
            return child.gameObject;
        }

        return null;
    }

    private void PaintCell(Vector2Int cell)
    {
        if (activePrefab == null)
        {
            return;
        }

        // 겹치지 않게 배치
        switch (duplicateMode)
        {
            case DUPLICATE_MODE.PREVENT_ALL:
                if (FindObjectAtCell(cell) != null)
                {
                    return;
                }
                break;
            case DUPLICATE_MODE.SAME_PREFAB_ONLY:
                if (FindObjectAtCell(cell, activePrefab) != null)
                {
                    return;
                }
                break;
            case DUPLICATE_MODE.ALLOW_OVERLAP:
                break;
        }

        Transform root = GetLevelRoot(true);

        GameObject obj = PrefabUtility.InstantiatePrefab(activePrefab) as GameObject;

        if (obj == null)
        {
            return;
        }

        Undo.RegisterCreatedObjectUndo(obj, "Paint Level Object");

        Undo.SetTransformParent(obj.transform, root, "Parant Level Object");

        obj.transform.position = CellToWorld(cell);
        obj.transform.rotation = GetRotation();

        EditorUtility.SetDirty(obj);
    }

    private void EraseCell(Vector2Int cell)
    {
        GameObject target = FindObjectAtCell(cell);

        if (target == null)
        {
            return;
        }

        Undo.DestroyObjectImmediate(target);
    }
}
