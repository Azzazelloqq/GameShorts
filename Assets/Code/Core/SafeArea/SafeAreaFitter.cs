using System;
using UnityEngine;

namespace Code.Core.SafeArea
{
    /// <summary>
    /// Component that automatically adjusts RectTransform to fit within device safe area.
    /// This ensures UI elements don't overlap with device notches, cameras, or rounded corners.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public class SafeAreaFitter : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool applyLeft = true;
        [SerializeField] private bool applyRight = true;
        [SerializeField] private bool applyTop = true;
        [SerializeField] private bool applyBottom = true;
        
        [Header("Additional Padding")]
        [SerializeField] private Vector2 additionalPaddingTop;
        [SerializeField] private Vector2 additionalPaddingBottom;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        
        private RectTransform rectTransform;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private ScreenOrientation lastOrientation;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            Refresh();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Update()
        {
            if (HasScreenParametersChanged())
            {
                Refresh();
            }
        }

        /// <summary>
        /// Check if screen parameters have changed since last update
        /// </summary>
        private bool HasScreenParametersChanged()
        {
            var safeArea = GetSafeArea();
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            var orientation = GetOrientation();

            if (safeArea != lastSafeArea || screenSize != lastScreenSize || orientation != lastOrientation)
            {
                lastSafeArea = safeArea;
                lastScreenSize = screenSize;
                lastOrientation = orientation;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Apply safe area to RectTransform
        /// </summary>
        public void Refresh()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            var safeArea = GetSafeArea();
            
            if (showDebugInfo)
            {
                Debug.Log($"[SafeAreaFitter] Screen: {Screen.width}x{Screen.height}, SafeArea: {safeArea}, Orientation: {GetOrientation()}");
            }

            ApplySafeArea(safeArea);
        }

        /// <summary>
        /// Get current safe area
        /// </summary>
        private Rect GetSafeArea()
        {
            var safeArea = Screen.safeArea;
            
            // In editor, simulate iPhone X style notch for testing
            #if UNITY_EDITOR
            if (IsSimulateNotchInEditor())
            {
                safeArea = SimulateNotch();
            }
            #endif
            
            return safeArea;
        }

        /// <summary>
        /// Apply safe area to RectTransform anchors
        /// </summary>
        private void ApplySafeArea(Rect safeArea)
        {
            if (rectTransform == null) return;

            // Convert safe area to anchor coordinates
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            // Apply settings
            if (!applyLeft) anchorMin.x = rectTransform.anchorMin.x;
            if (!applyBottom) anchorMin.y = rectTransform.anchorMin.y;
            if (!applyRight) anchorMax.x = rectTransform.anchorMax.x;
            if (!applyTop) anchorMax.y = rectTransform.anchorMax.y;

            // Apply additional padding
            anchorMin += additionalPaddingBottom;
            anchorMax -= additionalPaddingTop;

            // Set anchors
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
        }

        /// <summary>
        /// Get current screen orientation
        /// </summary>
        private ScreenOrientation GetOrientation()
        {
            #if UNITY_EDITOR
            return Screen.width > Screen.height ? ScreenOrientation.LandscapeLeft : ScreenOrientation.Portrait;
            #else
            return Screen.orientation;
            #endif
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Check if notch simulation is enabled in editor
        /// </summary>
        private bool IsSimulateNotchInEditor()
        {
            // You can add a menu item or project setting to control this
            return UnityEngine.Device.SystemInfo.deviceModel == "Unity Editor" && Application.isEditor;
        }

        /// <summary>
        /// Simulate iPhone X style notch for testing in editor
        /// </summary>
        private Rect SimulateNotch()
        {
            var orientation = GetOrientation();
            var screenWidth = Screen.width;
            var screenHeight = Screen.height;

            if (orientation == ScreenOrientation.Portrait || orientation == ScreenOrientation.PortraitUpsideDown)
            {
                // Portrait mode: notch at top, home indicator at bottom
                float notchHeight = screenHeight * 0.04f; // ~44 pixels on iPhone X
                float homeIndicatorHeight = screenHeight * 0.02f; // ~34 pixels
                return new Rect(0, homeIndicatorHeight, screenWidth, screenHeight - notchHeight - homeIndicatorHeight);
            }
            else
            {
                // Landscape mode: notch on side
                float notchWidth = screenWidth * 0.03f; // Side notch
                return new Rect(notchWidth, 0, screenWidth - notchWidth * 2, screenHeight);
            }
        }

        /// <summary>
        /// Draw debug gizmos in Scene view
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showDebugInfo || rectTransform == null) return;

            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(corners[0], corners[1]);
            Gizmos.DrawLine(corners[1], corners[2]);
            Gizmos.DrawLine(corners[2], corners[3]);
            Gizmos.DrawLine(corners[3], corners[0]);
        }
        #endif

        /// <summary>
        /// Force refresh safe area
        /// </summary>
        [ContextMenu("Force Refresh")]
        public void ForceRefresh()
        {
            lastSafeArea = Rect.zero;
            lastScreenSize = Vector2Int.zero;
            Refresh();
        }

        /// <summary>
        /// Reset to default settings
        /// </summary>
        [ContextMenu("Reset to Defaults")]
        public void ResetToDefaults()
        {
            applyLeft = true;
            applyRight = true;
            applyTop = true;
            applyBottom = true;
            additionalPaddingTop = Vector2.zero;
            additionalPaddingBottom = Vector2.zero;
            Refresh();
        }
    }
}

