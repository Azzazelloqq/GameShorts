using UnityEngine;
using UnityEditor;

namespace Code.Core.SafeArea.Editor
{
    [CustomEditor(typeof(SafeAreaFitter))]
    public class SafeAreaFitterEditor : UnityEditor.Editor
    {
        private static readonly Color EnabledColor = new Color(0.5f, 1f, 0.5f, 0.3f);
        private static readonly Color DisabledColor = new Color(0.3f, 0.3f, 0.3f, 0.2f);
        private static readonly Color SafeAreaColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        
        // Serialized properties
        private SerializedProperty applyLeft;
        private SerializedProperty applyRight;
        private SerializedProperty applyTop;
        private SerializedProperty applyBottom;
        private SerializedProperty additionalPaddingTop;
        private SerializedProperty additionalPaddingBottom;
        private SerializedProperty showDebugInfo;
        
        private void OnEnable()
        {
            applyLeft = serializedObject.FindProperty("applyLeft");
            applyRight = serializedObject.FindProperty("applyRight");
            applyTop = serializedObject.FindProperty("applyTop");
            applyBottom = serializedObject.FindProperty("applyBottom");
            additionalPaddingTop = serializedObject.FindProperty("additionalPaddingTop");
            additionalPaddingBottom = serializedObject.FindProperty("additionalPaddingBottom");
            showDebugInfo = serializedObject.FindProperty("showDebugInfo");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            var fitter = (SafeAreaFitter)target;
            
            EditorGUILayout.Space();
            
            // Title
            EditorGUILayout.LabelField("Safe Area Fitter", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Этот компонент автоматически настраивает RectTransform, чтобы UI не выходил за safe area (безопасную зону без вырезов).", MessageType.Info);
            
            EditorGUILayout.Space();
            
            // Visual Safe Area Configuration
            EditorGUILayout.LabelField("Настройки Safe Area", EditorStyles.boldLabel);
            DrawVisualSafeAreaSettings();
            
            EditorGUILayout.Space();
            
            // Additional Padding
            EditorGUILayout.LabelField("Дополнительные отступы", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(additionalPaddingTop, new GUIContent("Отступ сверху", "Дополнительный отступ от верхней границы safe area"));
            EditorGUILayout.PropertyField(additionalPaddingBottom, new GUIContent("Отступ снизу", "Дополнительный отступ от нижней границы safe area"));
            
            EditorGUILayout.Space();
            
            // Debug
            EditorGUILayout.LabelField("Отладка", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(showDebugInfo, new GUIContent("Показать отладку", "Отображать информацию о safe area в консоли"));
            
            EditorGUILayout.Space();
            
            // Current State Info
            if (Application.isPlaying)
            {
                DrawRuntimeInfo();
            }
            else
            {
                DrawEditorInfo();
            }
            
            EditorGUILayout.Space();
            
            // Action Buttons
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Обновить", GUILayout.Height(30)))
            {
                fitter.ForceRefresh();
                SceneView.RepaintAll();
            }
            
            if (GUILayout.Button("Сбросить", GUILayout.Height(30)))
            {
                fitter.ResetToDefaults();
                SceneView.RepaintAll();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Quick Presets
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Быстрые пресеты", EditorStyles.boldLabel);
            DrawPresetButtons();
            
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawVisualSafeAreaSettings()
        {
            var rect = GUILayoutUtility.GetRect(200, 150);
            
            // Draw background
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 1f));
            
            // Calculate inner rect (representing screen)
            var screenRect = new Rect(rect.x + 10, rect.y + 10, rect.width - 20, rect.height - 20);
            EditorGUI.DrawRect(screenRect, new Color(0.3f, 0.3f, 0.3f, 1f));
            
            // Draw safe area preview
            var safeAreaRect = new Rect(
                screenRect.x + (applyLeft.boolValue ? 20 : 0),
                screenRect.y + (applyBottom.boolValue ? 15 : 0),
                screenRect.width - (applyLeft.boolValue ? 20 : 0) - (applyRight.boolValue ? 20 : 0),
                screenRect.height - (applyTop.boolValue ? 25 : 0) - (applyBottom.boolValue ? 15 : 0)
            );
            
            EditorGUI.DrawRect(safeAreaRect, EnabledColor);
            
            // Draw borders
            Handles.color = SafeAreaColor;
            Handles.DrawSolidRectangleWithOutline(safeAreaRect, Color.clear, SafeAreaColor);
            
            // Draw interactive buttons for each side
            var buttonSize = 60;
            var buttonHeight = 25;
            
            // Top button
            var topButtonRect = new Rect(rect.x + rect.width / 2 - buttonSize / 2, rect.y - 5, buttonSize, buttonHeight);
            applyTop.boolValue = EditorGUI.ToggleLeft(topButtonRect, "Верх", applyTop.boolValue);
            
            // Bottom button
            var bottomButtonRect = new Rect(rect.x + rect.width / 2 - buttonSize / 2, rect.y + rect.height - 20, buttonSize, buttonHeight);
            applyBottom.boolValue = EditorGUI.ToggleLeft(bottomButtonRect, "Низ", applyBottom.boolValue);
            
            // Left button
            var leftButtonRect = new Rect(rect.x - 50, rect.y + rect.height / 2 - buttonHeight / 2, 50, buttonHeight);
            applyLeft.boolValue = EditorGUI.ToggleLeft(leftButtonRect, "Лево", applyLeft.boolValue);
            
            // Right button
            var rightButtonRect = new Rect(rect.x + rect.width, rect.y + rect.height / 2 - buttonHeight / 2, 55, buttonHeight);
            applyRight.boolValue = EditorGUI.ToggleLeft(rightButtonRect, "Право", applyRight.boolValue);
            
            // Draw labels
            var labelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            EditorGUI.LabelField(safeAreaRect, "Safe Area", labelStyle);
        }

        private void DrawRuntimeInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Информация о Safe Area (Runtime):", EditorStyles.boldLabel);
            
            var safeArea = Screen.safeArea;
            EditorGUILayout.LabelField($"Размер экрана: {Screen.width} x {Screen.height}");
            EditorGUILayout.LabelField($"Safe Area: X:{safeArea.x:F0} Y:{safeArea.y:F0} W:{safeArea.width:F0} H:{safeArea.height:F0}");
            
            var insets = SafeAreaHelper.GetSafeAreaInsets();
            EditorGUILayout.LabelField($"Отступы (L,B,R,T): {insets.x:F0}, {insets.y:F0}, {insets.z:F0}, {insets.w:F0}");
            
            if (SafeAreaHelper.HasNotch())
            {
                EditorGUILayout.LabelField("⚠️ Обнаружен вырез (notch)", EditorStyles.boldLabel);
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawEditorInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Как это работает:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("• Компонент автоматически изменяет anchors у RectTransform", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("• Выберите стороны, которые нужно адаптировать под safe area", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("• В Play Mode будет симулироваться notch для тестирования", EditorStyles.wordWrappedLabel);
            
            EditorGUILayout.Space(5);
            
            var rectTransform = ((SafeAreaFitter)target).GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                EditorGUILayout.LabelField("Текущие anchors:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Min: ({rectTransform.anchorMin.x:F2}, {rectTransform.anchorMin.y:F2})");
                EditorGUILayout.LabelField($"Max: ({rectTransform.anchorMax.x:F2}, {rectTransform.anchorMax.y:F2})");
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawPresetButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("📱 Полный экран", GUILayout.Height(25)))
            {
                applyLeft.boolValue = true;
                applyRight.boolValue = true;
                applyTop.boolValue = true;
                applyBottom.boolValue = true;
            }
            
            if (GUILayout.Button("🔝 Только верх", GUILayout.Height(25)))
            {
                applyLeft.boolValue = false;
                applyRight.boolValue = false;
                applyTop.boolValue = true;
                applyBottom.boolValue = false;
            }
            
            if (GUILayout.Button("🔽 Только низ", GUILayout.Height(25)))
            {
                applyLeft.boolValue = false;
                applyRight.boolValue = false;
                applyTop.boolValue = false;
                applyBottom.boolValue = true;
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("↔️ Горизонталь", GUILayout.Height(25)))
            {
                applyLeft.boolValue = true;
                applyRight.boolValue = true;
                applyTop.boolValue = false;
                applyBottom.boolValue = false;
            }
            
            if (GUILayout.Button("↕️ Вертикаль", GUILayout.Height(25)))
            {
                applyLeft.boolValue = false;
                applyRight.boolValue = false;
                applyTop.boolValue = true;
                applyBottom.boolValue = true;
            }
            
            if (GUILayout.Button("❌ Отключить", GUILayout.Height(25)))
            {
                applyLeft.boolValue = false;
                applyRight.boolValue = false;
                applyTop.boolValue = false;
                applyBottom.boolValue = false;
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
}

