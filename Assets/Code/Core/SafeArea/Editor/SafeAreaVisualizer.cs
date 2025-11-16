using UnityEngine;
using UnityEditor;

namespace Code.Core.SafeArea.Editor
{
    /// <summary>
    /// Visualizes safe area in Scene View for better understanding
    /// </summary>
    [InitializeOnLoad]
    public static class SafeAreaVisualizer
    {
        private static bool isEnabled = true;
        private static readonly Color SafeAreaColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);
        private static readonly Color SafeAreaBorderColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        private static readonly Color UnsafeAreaColor = new Color(1f, 0.2f, 0.2f, 0.2f);
        
        static SafeAreaVisualizer()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!isEnabled) return;
            
            // Find all SafeAreaFitter components in the scene
            var fitters = Object.FindObjectsOfType<SafeAreaFitter>();
            
            foreach (var fitter in fitters)
            {
                if (fitter == null || !fitter.gameObject.activeInHierarchy) continue;
                
                DrawSafeAreaGizmo(fitter);
            }
            
            // Draw overlay info if any SafeAreaCanvas is selected
            var selectedCanvas = Selection.activeGameObject?.GetComponent<SafeAreaCanvas>();
            if (selectedCanvas != null)
            {
                DrawSafeAreaOverlay(sceneView);
            }
        }

        private static void DrawSafeAreaGizmo(SafeAreaFitter fitter)
        {
            var rectTransform = fitter.GetComponent<RectTransform>();
            if (rectTransform == null) return;
            
            // Get world corners
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            
            // Draw safe area bounds
            Handles.color = SafeAreaBorderColor;
            
            // Draw outline
            Handles.DrawLine(corners[0], corners[1], 2f);
            Handles.DrawLine(corners[1], corners[2], 2f);
            Handles.DrawLine(corners[2], corners[3], 2f);
            Handles.DrawLine(corners[3], corners[0], 2f);
            
            // Draw fill
            Handles.color = SafeAreaColor;
            Handles.DrawAAConvexPolygon(corners);
            
            // Draw corner indicators
            Handles.color = SafeAreaBorderColor;
            float cornerSize = 10f;
            
            foreach (var corner in corners)
            {
                Handles.DrawWireDisc(corner, Vector3.forward, cornerSize);
            }
            
            // Draw label
            var center = (corners[0] + corners[2]) / 2f;
            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            
            Handles.Label(center, "Safe Area", labelStyle);
            
            // Draw status indicators for each side
            DrawSideIndicators(fitter, corners);
        }

        private static void DrawSideIndicators(SafeAreaFitter fitter, Vector3[] corners)
        {
            var applyLeftField = typeof(SafeAreaFitter).GetField("applyLeft", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var applyRightField = typeof(SafeAreaFitter).GetField("applyRight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var applyTopField = typeof(SafeAreaFitter).GetField("applyTop", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var applyBottomField = typeof(SafeAreaFitter).GetField("applyBottom", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            bool applyLeft = applyLeftField != null && (bool)applyLeftField.GetValue(fitter);
            bool applyRight = applyRightField != null && (bool)applyRightField.GetValue(fitter);
            bool applyTop = applyTopField != null && (bool)applyTopField.GetValue(fitter);
            bool applyBottom = applyBottomField != null && (bool)applyBottomField.GetValue(fitter);
            
            var iconStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            
            // Left indicator
            if (applyLeft)
            {
                var leftCenter = (corners[0] + corners[1]) / 2f;
                iconStyle.normal.textColor = Color.green;
                Handles.Label(leftCenter + Vector3.left * 20, "◀", iconStyle);
            }
            
            // Right indicator
            if (applyRight)
            {
                var rightCenter = (corners[2] + corners[3]) / 2f;
                iconStyle.normal.textColor = Color.green;
                Handles.Label(rightCenter + Vector3.right * 20, "▶", iconStyle);
            }
            
            // Top indicator
            if (applyTop)
            {
                var topCenter = (corners[1] + corners[2]) / 2f;
                iconStyle.normal.textColor = Color.green;
                Handles.Label(topCenter + Vector3.up * 20, "▲", iconStyle);
            }
            
            // Bottom indicator
            if (applyBottom)
            {
                var bottomCenter = (corners[0] + corners[3]) / 2f;
                iconStyle.normal.textColor = Color.green;
                Handles.Label(bottomCenter + Vector3.down * 20, "▼", iconStyle);
            }
        }

        private static void DrawSafeAreaOverlay(SceneView sceneView)
        {
            Handles.BeginGUI();
            
            var rect = new Rect(10, 10, 250, 120);
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            
            GUILayout.BeginArea(rect);
            GUILayout.Space(5);
            
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
            
            GUILayout.Label("Safe Area Info", titleStyle);
            GUILayout.Space(5);
            
            if (Application.isPlaying)
            {
                var safeArea = SafeAreaHelper.GetSafeArea();
                var insets = SafeAreaHelper.GetSafeAreaInsets();
                var hasNotch = SafeAreaHelper.HasNotch();
                
                EditorGUILayout.LabelField($"Screen: {Screen.width}x{Screen.height}");
                EditorGUILayout.LabelField($"Safe: {safeArea.width:F0}x{safeArea.height:F0}");
                EditorGUILayout.LabelField($"Insets: L:{insets.x:F0} R:{insets.z:F0}");
                EditorGUILayout.LabelField($"        T:{insets.w:F0} B:{insets.y:F0}");
                
                if (hasNotch)
                {
                    EditorGUILayout.LabelField("⚠️ Notch detected!", EditorStyles.boldLabel);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see runtime info", MessageType.Info);
            }
            
            GUILayout.EndArea();
            
            Handles.EndGUI();
        }

        [MenuItem("Window/Safe Area/Toggle Visualization")]
        private static void ToggleVisualization()
        {
            isEnabled = !isEnabled;
            SceneView.RepaintAll();
            
            Debug.Log($"Safe Area Visualization: {(isEnabled ? "Enabled" : "Disabled")}");
        }

        [MenuItem("Window/Safe Area/Toggle Visualization", true)]
        private static bool ToggleVisualizationValidate()
        {
            Menu.SetChecked("Window/Safe Area/Toggle Visualization", isEnabled);
            return true;
        }

        [MenuItem("Window/Safe Area/Create Safe Area Canvas")]
        private static void CreateSafeAreaCanvas()
        {
            // Create Canvas
            var canvasGO = new GameObject("SafeAreaCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            var canvasScaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasScaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // Add SafeAreaCanvas
            var safeCanvas = canvasGO.AddComponent<SafeAreaCanvas>();
            
            // Select the created object
            Selection.activeGameObject = canvasGO;
            EditorGUIUtility.PingObject(canvasGO);
            
            Debug.Log("Created Safe Area Canvas with default settings");
        }

        [MenuItem("Window/Safe Area/Documentation")]
        private static void OpenDocumentation()
        {
            var path = "Assets/Code/Core/SafeArea/SETUP_GUIDE.md";
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (asset != null)
            {
                AssetDatabase.OpenAsset(asset);
            }
            else
            {
                Debug.LogWarning($"Documentation not found at {path}");
            }
        }
    }
}


