// DuplicateAndDragHoldPlanes.cs
// Перетаскивание КОПИИ только при удержании Ctrl (Win)/Cmd (mac) + ЛКМ,
// с выбором плоскости перемещения: XZ / XY / YZ / Camera.
// Клавиши: 1=XZ, 2=XY, 3=YZ, 4=Camera, Tab=cycle.

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class DuplicateAndDragHoldPlanes
{
    private enum DragPlaneMode { XZ, XY, YZ, Camera }
    private static DragPlaneMode _mode = DragPlaneMode.XZ;

    private static bool _dragging;
    private static bool _duplicatedThisDrag;

    private static Plane _dragPlane;
    private static Vector3 _pivot;
    private static Vector3 _lastWorld;

    private static readonly List<Transform> Targets = new();
    private static readonly List<Vector3> InitialOffsets = new();

    static DuplicateAndDragHoldPlanes()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sv)
    {
        var e = Event.current;

        // HUD
        Handles.BeginGUI();
        var rect = new Rect(10, 10, 700, 36);
        GUI.contentColor = new Color(1f, 1f, 1f, 0.9f);
        GUI.Label(rect,
            $"Hold Ctrl/Cmd + LMB → Duplicate & Drag | Plane: {_mode}  (1:XZ  2:XY  3:YZ  4:Camera  Tab:cycle)",
            EditorStyles.miniBoldLabel);
        Handles.EndGUI();

        if (e.alt || Tools.current == Tool.View) return; // не конфликтуем с навигацией

        // Горячие клавиши смены плоскости — работают всегда (чтобы готовить режим до drag)
        if (e.type == EventType.KeyDown && !e.isKey && e.control && !e.command && !e.alt)
        {
            if (TryChangeModeByKey(e.keyCode, sv))
            {
                e.Use();
                // если идет drag — перестраиваем плоскость без рывка
                if (_dragging) RebuildPlane(sv, keepUnderCursor: true);
            }
        }
        else if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Tab))
        {
            CycleMode();
            e.Use();
            if (_dragging) RebuildPlane(sv, keepUnderCursor: true);
        }

        int id = GUIUtility.GetControlID(FocusType.Passive);

        switch (e.GetTypeForControl(id))
        {
            case EventType.MouseDown:
                if (e.button == 0 && IsModifierHeld(e))
                {
                    if (Selection.transforms is { Length: > 0 } && StartDrag(sv, e))
                    {
                        GUIUtility.hotControl = id;
                        e.Use();
                    }
                }
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl == id && _dragging)
                {
                    // если отпустили модификатор — завершаем
                    if (!IsModifierHeld(e))
                    {
                        EndDrag();
                        GUIUtility.hotControl = 0;
                        e.Use();
                        break;
                    }

                    if (!_duplicatedThisDrag)
                        DoDuplicate();

                    UpdateDrag(sv, e);
                    e.Use();
                }
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl == id)
                {
                    EndDrag();
                    GUIUtility.hotControl = 0;
                    e.Use();
                }
                break;

            case EventType.Layout:
                if (_dragging || (e.button == 0 && IsModifierHeld(e)))
                    HandleUtility.AddDefaultControl(id);
                break;
        }
    }

    private static bool IsModifierHeld(Event e)
    {
#if UNITY_EDITOR_OSX
        return e.command;   // Cmd на macOS
#else
        return e.control;   // Ctrl на Windows/Linux
#endif
    }

    private static bool TryChangeModeByKey(KeyCode key, SceneView sv)
    {
        var old = _mode;
        switch (key)
        {
            case KeyCode.Alpha1: _mode = DragPlaneMode.XZ; break;
            case KeyCode.Alpha2: _mode = DragPlaneMode.XY; break;
            case KeyCode.Alpha3: _mode = DragPlaneMode.YZ; break;
            case KeyCode.Alpha4: _mode = DragPlaneMode.Camera; break;
            default: return false;
        }
        if (old != _mode)
            ShowNote($"Plane: {_mode}");
        return true;
    }

    private static void CycleMode()
    {
        _mode = (DragPlaneMode)(((int)_mode + 1) % 4);
        ShowNote($"Plane: {_mode}");
    }

    private static bool StartDrag(SceneView sv, Event e)
    {
        _dragging = true;
        _duplicatedThisDrag = false;

        Targets.Clear();
        InitialOffsets.Clear();

        foreach (var t in Selection.transforms)
            if (t) Targets.Add(t);
        if (Targets.Count == 0) return false;

        // Пивот — центр выделения
        _pivot = Vector3.zero;
        foreach (var t in Targets) _pivot += t.position;
        _pivot /= Mathf.Max(1, Targets.Count);

        BuildPlane(sv, _pivot);

        if (!RayToPlane(sv, e.mousePosition, out var hit)) { _dragging = false; return false; }
        _lastWorld = hit;

        foreach (var t in Targets)
            InitialOffsets.Add(t.position - _pivot);

        return true;
    }

    private static void DoDuplicate()
    {
        Unsupported.DuplicateGameObjectsUsingPasteboard();

        Targets.Clear();
        InitialOffsets.Clear();
        foreach (var t in Selection.transforms)
            if (t) Targets.Add(t);

        _pivot = Vector3.zero;
        foreach (var t in Targets) _pivot += t.position;
        _pivot /= Mathf.Max(1, Targets.Count);

        foreach (var t in Targets)
            InitialOffsets.Add(t.position - _pivot);

        _duplicatedThisDrag = true;
        ShowNote("Duplicated ✨");
    }

    private static void UpdateDrag(SceneView sv, Event e)
    {
        if (!RayToPlane(sv, e.mousePosition, out var hit)) return;

        var delta = hit - _lastWorld;
        _lastWorld = hit;

        _pivot += delta;

        for (int i = 0; i < Targets.Count; i++)
        {
            var t = Targets[i];
            if (!t) continue;

            var pos = _pivot + InitialOffsets[i];

            // удерживаем относительную высоту внутри выбранной плоскости:
            // switch (_mode)
            // {
            //     case DragPlaneMode.XZ: pos.y = _pivot.y + InitialOffsets[i].y; break;
            //     case DragPlaneMode.XY: pos.z = _pivot.z + InitialOffsets[i].z; break;
            //     case DragPlaneMode.YZ: pos.x = _pivot.x + InitialOffsets[i].x; break;
            //     case DragPlaneMode.Camera:
            //         // для "Camera" ничего не фиксируем — точка уже лежит в плоскости камеры
            //         break;
            // }

            Undo.RecordObject(t, "Duplicate & Drag");
            t.position = pos;
            EditorUtility.SetDirty(t);
        }

        SceneView.RepaintAll();
    }

    private static void EndDrag()
    {
        _dragging = false;
        _duplicatedThisDrag = false;
    }

    private static void BuildPlane(SceneView sv, Vector3 throughPoint)
    {
        switch (_mode)
        {
            case DragPlaneMode.XZ: _dragPlane = new Plane(Vector3.up, new Vector3(0, throughPoint.y, 0)); break;
            case DragPlaneMode.XY: _dragPlane = new Plane(Vector3.forward, new Vector3(0, 0, throughPoint.z)); break;
            case DragPlaneMode.YZ: _dragPlane = new Plane(Vector3.right, new Vector3(throughPoint.x, 0, 0)); break;
            case DragPlaneMode.Camera:
                var cam = sv.camera != null ? sv.camera.transform : null;
                var normal = cam ? cam.forward : Vector3.forward;
                _dragPlane = new Plane(normal, throughPoint);
                break;
        }
    }

    private static void RebuildPlane(SceneView sv, bool keepUnderCursor)
    {
        if (!keepUnderCursor)
        {
            BuildPlane(sv, _pivot);
            return;
        }

        // Перестроить плоскость так, чтобы текущая точка под курсором осталась "якорем"
        if (RayToPlane(sv, Event.current.mousePosition, out var oldHit))
        {
            BuildPlane(sv, _pivot);
            if (RayToPlane(sv, Event.current.mousePosition, out var newHit))
            {
                // подвинем пивот на разницу, чтобы не было скачка
                var offset = oldHit - newHit;
                _pivot += offset;
                _lastWorld = oldHit;
            }
        }
        else
        {
            BuildPlane(sv, _pivot);
        }
    }

    private static bool RayToPlane(SceneView sv, Vector2 guiMousePos, out Vector3 hit)
    {
        var ray = HandleUtility.GUIPointToWorldRay(guiMousePos);
        if (_dragPlane.Raycast(ray, out float enter))
        {
            hit = ray.GetPoint(enter);
            return true;
        }
        hit = default;
        return false;
    }

    private static void ShowNote(string text)
    {
        foreach (var sv in SceneView.sceneViews)
            if (sv is SceneView view) view.ShowNotification(new GUIContent(text), .2f);
    }
}
#endif
