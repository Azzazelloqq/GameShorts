using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Core.SafeArea
{
    /// <summary>
    /// Canvas-level safe area manager that can handle multiple panels with different safe area settings
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class SafeAreaCanvas : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool autoRefreshChildren = true;
        [SerializeField] private float refreshInterval = 0.5f; // Check for changes every 0.5 seconds
        
        [Header("Default Panel Settings")]
        [SerializeField] private bool createDefaultPanel = true;
        [SerializeField] private string defaultPanelName = "SafeAreaPanel";
        
        private Canvas canvas;
        private CanvasScaler canvasScaler;
        private List<SafeAreaFitter> managedFitters = new List<SafeAreaFitter>();
        private float lastRefreshTime;
        
        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            canvasScaler = GetComponent<CanvasScaler>();
            
            if (createDefaultPanel && transform.childCount == 0)
            {
                CreateDefaultSafeAreaPanel();
            }
            
            CollectSafeAreaFitters();
        }

        private void OnEnable()
        {
            RefreshAll();
        }

        private void Update()
        {
            if (autoRefreshChildren && Time.time - lastRefreshTime > refreshInterval)
            {
                RefreshAll();
                lastRefreshTime = Time.time;
            }
        }

        /// <summary>
        /// Create a default safe area panel
        /// </summary>
        private void CreateDefaultSafeAreaPanel()
        {
            var panelGO = new GameObject(defaultPanelName);
            panelGO.transform.SetParent(transform, false);
            
            var rectTransform = panelGO.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            var safeAreaFitter = panelGO.AddComponent<SafeAreaFitter>();
            managedFitters.Add(safeAreaFitter);
        }

        /// <summary>
        /// Collect all SafeAreaFitter components in children
        /// </summary>
        public void CollectSafeAreaFitters()
        {
            managedFitters.Clear();
            var fitters = GetComponentsInChildren<SafeAreaFitter>(true);
            managedFitters.AddRange(fitters);
        }

        /// <summary>
        /// Refresh all managed SafeAreaFitter components
        /// </summary>
        public void RefreshAll()
        {
            foreach (var fitter in managedFitters)
            {
                if (fitter != null)
                {
                    fitter.Refresh();
                }
            }
        }

        /// <summary>
        /// Add a new safe area panel with specific settings
        /// </summary>
        public SafeAreaFitter AddSafeAreaPanel(string panelName, bool applyLeft = true, bool applyRight = true, 
            bool applyTop = true, bool applyBottom = true)
        {
            var panelGO = new GameObject(panelName);
            panelGO.transform.SetParent(transform, false);
            
            var rectTransform = panelGO.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            var safeAreaFitter = panelGO.AddComponent<SafeAreaFitter>();
            
            // Apply settings via reflection or make fields public
            System.Reflection.FieldInfo applyLeftField = typeof(SafeAreaFitter).GetField("applyLeft", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            System.Reflection.FieldInfo applyRightField = typeof(SafeAreaFitter).GetField("applyRight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            System.Reflection.FieldInfo applyTopField = typeof(SafeAreaFitter).GetField("applyTop", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            System.Reflection.FieldInfo applyBottomField = typeof(SafeAreaFitter).GetField("applyBottom", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (applyLeftField != null) applyLeftField.SetValue(safeAreaFitter, applyLeft);
            if (applyRightField != null) applyRightField.SetValue(safeAreaFitter, applyRight);
            if (applyTopField != null) applyTopField.SetValue(safeAreaFitter, applyTop);
            if (applyBottomField != null) applyBottomField.SetValue(safeAreaFitter, applyBottom);
            
            managedFitters.Add(safeAreaFitter);
            safeAreaFitter.Refresh();
            
            return safeAreaFitter;
        }

        /// <summary>
        /// Remove a SafeAreaFitter from management
        /// </summary>
        public void RemoveSafeAreaFitter(SafeAreaFitter fitter)
        {
            if (managedFitters.Contains(fitter))
            {
                managedFitters.Remove(fitter);
            }
        }

        /// <summary>
        /// Get canvas safe area in canvas space
        /// </summary>
        public Rect GetCanvasSafeArea()
        {
            var safeArea = SafeAreaHelper.GetSafeArea();
            
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return safeArea;
            }
            else if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera != null)
            {
                // Convert screen safe area to canvas space
                Vector2 min = canvas.worldCamera.ScreenToViewportPoint(safeArea.min);
                Vector2 max = canvas.worldCamera.ScreenToViewportPoint(safeArea.max);
                
                var canvasRect = (canvas.transform as RectTransform).rect;
                return new Rect(
                    min.x * canvasRect.width,
                    min.y * canvasRect.height,
                    (max.x - min.x) * canvasRect.width,
                    (max.y - min.y) * canvasRect.height
                );
            }
            
            return safeArea;
        }

        /// <summary>
        /// Force refresh all components
        /// </summary>
        [ContextMenu("Force Refresh All")]
        public void ForceRefreshAll()
        {
            SafeAreaHelper.ClearCache();
            CollectSafeAreaFitters();
            RefreshAll();
        }

        /// <summary>
        /// Log safe area information
        /// </summary>
        [ContextMenu("Log Safe Area Info")]
        public void LogSafeAreaInfo()
        {
            var safeArea = SafeAreaHelper.GetSafeArea();
            var normalizedSafeArea = SafeAreaHelper.GetNormalizedSafeArea();
            var insets = SafeAreaHelper.GetSafeAreaInsets();
            var hasNotch = SafeAreaHelper.HasNotch();
            var deviceType = SafeAreaHelper.GetEstimatedDeviceType();
            
            Debug.Log($"[SafeAreaCanvas] Screen: {Screen.width}x{Screen.height}");
            Debug.Log($"[SafeAreaCanvas] Safe Area: {safeArea}");
            Debug.Log($"[SafeAreaCanvas] Normalized Safe Area: {normalizedSafeArea}");
            Debug.Log($"[SafeAreaCanvas] Insets (L,B,R,T): {insets}");
            Debug.Log($"[SafeAreaCanvas] Has Notch: {hasNotch}");
            Debug.Log($"[SafeAreaCanvas] Device Type: {deviceType}");
            Debug.Log($"[SafeAreaCanvas] Managed Fitters: {managedFitters.Count}");
        }
    }
}


