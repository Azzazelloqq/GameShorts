using UnityEngine;
using UnityEngine.UI;

namespace Code.Core.SafeArea
{
    /// <summary>
    /// Example of how to use SafeArea components
    /// </summary>
    public class SafeAreaExample : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private Text debugText;
        
        [Header("Example Settings")]
        [SerializeField] private bool createExampleUI = true;
        
        private SafeAreaCanvas safeAreaCanvas;
        
        private void Start()
        {
            SetupSafeAreaCanvas();
            
            if (createExampleUI)
            {
                CreateExampleUI();
            }
            
            UpdateDebugInfo();
        }

        private void SetupSafeAreaCanvas()
        {
            // Find or create canvas
            if (canvas == null)
            {
                canvas = FindObjectOfType<Canvas>();
                
                if (canvas == null)
                {
                    // Create new canvas
                    var canvasGO = new GameObject("SafeAreaCanvas");
                    canvas = canvasGO.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvasGO.AddComponent<CanvasScaler>();
                    canvasGO.AddComponent<GraphicRaycaster>();
                }
            }
            
            // Add SafeAreaCanvas component if not present
            safeAreaCanvas = canvas.GetComponent<SafeAreaCanvas>();
            if (safeAreaCanvas == null)
            {
                safeAreaCanvas = canvas.gameObject.AddComponent<SafeAreaCanvas>();
            }
        }

        private void CreateExampleUI()
        {
            // Create main safe area panel
            var mainPanel = safeAreaCanvas.AddSafeAreaPanel("MainSafePanel", true, true, true, true);
            
            // Add background image to show safe area
            var bgImage = mainPanel.gameObject.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.3f);
            
            // Create header panel (only top safe area)
            var headerPanel = CreateHeaderPanel();
            
            // Create bottom panel (only bottom safe area)
            var bottomPanel = CreateBottomPanel();
            
            // Create content in main panel
            CreateMainContent(mainPanel.gameObject);
        }

        private GameObject CreateHeaderPanel()
        {
            var headerGO = new GameObject("HeaderPanel");
            headerGO.transform.SetParent(canvas.transform, false);
            
            var rect = headerGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.9f);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            var image = headerGO.AddComponent<Image>();
            image.color = new Color(0.2f, 0.3f, 0.8f, 0.9f);
            
            var safeAreaFitter = headerGO.AddComponent<SafeAreaFitter>();
            // This will be configured to only apply top safe area via inspector
            
            // Add title text
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(headerGO.transform, false);
            
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(20, 0);
            titleRect.offsetMax = new Vector2(-20, 0);
            
            var titleText = titleGO.AddComponent<Text>();
            titleText.text = "Safe Area Example";
            titleText.fontSize = 24;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            
            return headerGO;
        }

        private GameObject CreateBottomPanel()
        {
            var bottomGO = new GameObject("BottomPanel");
            bottomGO.transform.SetParent(canvas.transform, false);
            
            var rect = bottomGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0.1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            var image = bottomGO.AddComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.3f, 0.9f);
            
            var safeAreaFitter = bottomGO.AddComponent<SafeAreaFitter>();
            // This will be configured to only apply bottom safe area via inspector
            
            // Add button row
            CreateButtonRow(bottomGO);
            
            return bottomGO;
        }

        private void CreateButtonRow(GameObject parent)
        {
            var buttonContainer = new GameObject("ButtonContainer");
            buttonContainer.transform.SetParent(parent.transform, false);
            
            var containerRect = buttonContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = new Vector2(10, 10);
            containerRect.offsetMax = new Vector2(-10, -10);
            
            var horizontalLayout = buttonContainer.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.spacing = 10;
            horizontalLayout.childAlignment = TextAnchor.MiddleCenter;
            horizontalLayout.childControlHeight = true;
            horizontalLayout.childControlWidth = true;
            horizontalLayout.childForceExpandHeight = true;
            horizontalLayout.childForceExpandWidth = true;
            
            // Create sample buttons
            CreateButton(buttonContainer, "Home", () => Debug.Log("Home pressed"));
            CreateButton(buttonContainer, "Settings", () => Debug.Log("Settings pressed"));
            CreateButton(buttonContainer, "Profile", () => Debug.Log("Profile pressed"));
        }

        private void CreateButton(GameObject parent, string label, System.Action onClick)
        {
            var buttonGO = new GameObject($"Button_{label}");
            buttonGO.transform.SetParent(parent.transform, false);
            
            var image = buttonGO.AddComponent<Image>();
            image.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            
            var button = buttonGO.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            var text = textGO.AddComponent<Text>();
            text.text = label;
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void CreateMainContent(GameObject parent)
        {
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(parent.transform, false);
            
            var rect = contentGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.15f);
            rect.anchorMax = new Vector2(0.9f, 0.85f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            var image = contentGO.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.1f);
            
            // Create debug text
            var debugGO = new GameObject("DebugText");
            debugGO.transform.SetParent(contentGO.transform, false);
            
            var debugRect = debugGO.AddComponent<RectTransform>();
            debugRect.anchorMin = Vector2.zero;
            debugRect.anchorMax = Vector2.one;
            debugRect.offsetMin = new Vector2(20, 20);
            debugRect.offsetMax = new Vector2(-20, -20);
            
            debugText = debugGO.AddComponent<Text>();
            debugText.fontSize = 16;
            debugText.color = Color.white;
            debugText.alignment = TextAnchor.UpperLeft;
            debugText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void UpdateDebugInfo()
        {
            if (debugText == null) return;
            
            var safeArea = SafeAreaHelper.GetSafeArea();
            var normalizedSafeArea = SafeAreaHelper.GetNormalizedSafeArea();
            var insets = SafeAreaHelper.GetSafeAreaInsets();
            var hasNotch = SafeAreaHelper.HasNotch();
            var deviceType = SafeAreaHelper.GetEstimatedDeviceType();
            var aspectRatio = SafeAreaHelper.GetSafeAreaAspectRatio();
            
            debugText.text = $"<b>Safe Area Debug Info</b>\n\n" +
                           $"Screen Size: {Screen.width} x {Screen.height}\n" +
                           $"Safe Area: {safeArea}\n" +
                           $"Normalized: {normalizedSafeArea}\n" +
                           $"Insets (L,B,R,T): {insets}\n" +
                           $"Aspect Ratio: {aspectRatio:F2}\n" +
                           $"Has Notch: {hasNotch}\n" +
                           $"Device Type: {deviceType}\n" +
                           $"Orientation: {Screen.orientation}";
        }

        private void Update()
        {
            // Update debug info periodically
            if (Time.frameCount % 30 == 0)
            {
                UpdateDebugInfo();
            }
        }
    }
}


