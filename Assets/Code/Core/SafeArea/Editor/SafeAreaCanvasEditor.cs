using UnityEngine;
using UnityEditor;

namespace Code.Core.SafeArea.Editor
{
    [CustomEditor(typeof(SafeAreaCanvas))]
    public class SafeAreaCanvasEditor : UnityEditor.Editor
    {
        private SerializedProperty autoRefreshChildren;
        private SerializedProperty refreshInterval;
        private SerializedProperty createDefaultPanel;
        private SerializedProperty defaultPanelName;
        
        private bool showManagedFitters = true;
        
        private void OnEnable()
        {
            autoRefreshChildren = serializedObject.FindProperty("autoRefreshChildren");
            refreshInterval = serializedObject.FindProperty("refreshInterval");
            createDefaultPanel = serializedObject.FindProperty("createDefaultPanel");
            defaultPanelName = serializedObject.FindProperty("defaultPanelName");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            var safeCanvas = (SafeAreaCanvas)target;
            
            // Header
            EditorGUILayout.LabelField("Safe Area Canvas", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Управляет всеми SafeAreaFitter компонентами на этом Canvas. " +
                                   "Автоматически обновляет их при изменении ориентации экрана.", MessageType.Info);
            
            EditorGUILayout.Space();
            
            // Settings
            EditorGUILayout.LabelField("Настройки", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(autoRefreshChildren, new GUIContent("Авто-обновление", 
                "Автоматически обновлять все SafeAreaFitter при изменении экрана"));
            
            if (autoRefreshChildren.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(refreshInterval, new GUIContent("Интервал обновления", 
                    "Как часто проверять изменения (в секундах)"));
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Default Panel
            EditorGUILayout.LabelField("Панель по умолчанию", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(createDefaultPanel, new GUIContent("Создать панель", 
                "Создать панель с SafeAreaFitter при старте"));
            
            if (createDefaultPanel.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(defaultPanelName, new GUIContent("Имя панели"));
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Managed Fitters
            showManagedFitters = EditorGUILayout.Foldout(showManagedFitters, "Управляемые компоненты", true);
            if (showManagedFitters)
            {
                DrawManagedFitters(safeCanvas);
            }
            
            EditorGUILayout.Space();
            
            // Action Buttons
            EditorGUILayout.LabelField("Действия", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("🔄 Обновить все", GUILayout.Height(30)))
            {
                safeCanvas.ForceRefreshAll();
                SceneView.RepaintAll();
            }
            
            if (GUILayout.Button("🔍 Найти компоненты", GUILayout.Height(30)))
            {
                safeCanvas.CollectSafeAreaFitters();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("📊 Информация", GUILayout.Height(30)))
            {
                safeCanvas.LogSafeAreaInfo();
            }
            
            if (GUILayout.Button("➕ Добавить панель", GUILayout.Height(30)))
            {
                ShowAddPanelMenu(safeCanvas);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Safe Area Info
            if (Application.isPlaying)
            {
                DrawSafeAreaInfo();
            }
            
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawManagedFitters(SafeAreaCanvas safeCanvas)
        {
            EditorGUI.indentLevel++;
            
            var fitters = safeCanvas.GetComponentsInChildren<SafeAreaFitter>(true);
            
            if (fitters.Length == 0)
            {
                EditorGUILayout.HelpBox("Нет SafeAreaFitter компонентов", MessageType.Info);
            }
            else
            {
                foreach (var fitter in fitters)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    // Status icon
                    var statusIcon = fitter.gameObject.activeInHierarchy ? "✅" : "❌";
                    EditorGUILayout.LabelField(statusIcon, GUILayout.Width(25));
                    
                    // Name as clickable label
                    if (GUILayout.Button(fitter.gameObject.name, EditorStyles.linkLabel))
                    {
                        Selection.activeGameObject = fitter.gameObject;
                        EditorGUIUtility.PingObject(fitter.gameObject);
                    }
                    
                    // Quick actions
                    if (GUILayout.Button("Настроить", GUILayout.Width(70)))
                    {
                        Selection.activeGameObject = fitter.gameObject;
                    }
                    
                    if (GUILayout.Button("🔄", GUILayout.Width(25)))
                    {
                        fitter.ForceRefresh();
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            EditorGUI.indentLevel--;
        }

        private void ShowAddPanelMenu(SafeAreaCanvas safeCanvas)
        {
            var menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("Полная Safe Area"), false, () => 
            {
                var panel = safeCanvas.AddSafeAreaPanel("FullSafePanel", true, true, true, true);
                Selection.activeGameObject = panel.gameObject;
            });
            
            menu.AddItem(new GUIContent("Header (только верх)"), false, () => 
            {
                var panel = safeCanvas.AddSafeAreaPanel("HeaderPanel", false, false, true, false);
                Selection.activeGameObject = panel.gameObject;
            });
            
            menu.AddItem(new GUIContent("Footer (только низ)"), false, () => 
            {
                var panel = safeCanvas.AddSafeAreaPanel("FooterPanel", false, false, false, true);
                Selection.activeGameObject = panel.gameObject;
            });
            
            menu.AddItem(new GUIContent("Контент (верх и низ)"), false, () => 
            {
                var panel = safeCanvas.AddSafeAreaPanel("ContentPanel", false, false, true, true);
                Selection.activeGameObject = panel.gameObject;
            });
            
            menu.AddItem(new GUIContent("Боковые панели (лево и право)"), false, () => 
            {
                var panel = safeCanvas.AddSafeAreaPanel("SidePanel", true, true, false, false);
                Selection.activeGameObject = panel.gameObject;
            });
            
            menu.ShowAsContext();
        }

        private void DrawSafeAreaInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Safe Area информация:", EditorStyles.boldLabel);
            
            var safeArea = SafeAreaHelper.GetSafeArea();
            var normalizedSafeArea = SafeAreaHelper.GetNormalizedSafeArea();
            var insets = SafeAreaHelper.GetSafeAreaInsets();
            var hasNotch = SafeAreaHelper.HasNotch();
            var deviceType = SafeAreaHelper.GetEstimatedDeviceType();
            
            EditorGUILayout.LabelField($"Экран: {Screen.width} x {Screen.height}");
            EditorGUILayout.LabelField($"Safe Area: {safeArea}");
            EditorGUILayout.LabelField($"Отступы (L,B,R,T): {insets}");
            EditorGUILayout.LabelField($"Есть вырез: {(hasNotch ? "Да ⚠️" : "Нет ✅")}");
            EditorGUILayout.LabelField($"Тип устройства: {GetDeviceTypeLabel(deviceType)}");
            
            EditorGUILayout.EndVertical();
        }

        private string GetDeviceTypeLabel(SafeAreaHelper.DeviceType deviceType)
        {
            switch (deviceType)
            {
                case SafeAreaHelper.DeviceType.PhoneWithNotch:
                    return "📱 Телефон с вырезом";
                case SafeAreaHelper.DeviceType.Tablet:
                    return "🖥️ Планшет";
                default:
                    return "📱 Телефон";
            }
        }
    }
}


