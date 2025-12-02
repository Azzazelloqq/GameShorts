using UnityEngine;

namespace Code.Core.SafeArea
{
    /// <summary>
    /// Helper class for working with device safe areas
    /// </summary>
    public static class SafeAreaHelper
    {
        private static Rect? cachedSafeArea;
        private static Vector2Int cachedScreenSize;

        /// <summary>
        /// Get current safe area with caching
        /// </summary>
        public static Rect GetSafeArea()
        {
            var currentScreenSize = new Vector2Int(Screen.width, Screen.height);
            
            if (!cachedSafeArea.HasValue || cachedScreenSize != currentScreenSize)
            {
                cachedSafeArea = Screen.safeArea;
                cachedScreenSize = currentScreenSize;
            }

            return cachedSafeArea.Value;
        }

        /// <summary>
        /// Get safe area as normalized values (0-1 range)
        /// </summary>
        public static Rect GetNormalizedSafeArea()
        {
            var safeArea = GetSafeArea();
            return new Rect(
                safeArea.x / Screen.width,
                safeArea.y / Screen.height,
                safeArea.width / Screen.width,
                safeArea.height / Screen.height
            );
        }

        /// <summary>
        /// Get safe area insets from screen edges
        /// </summary>
        public static Vector4 GetSafeAreaInsets()
        {
            var safeArea = GetSafeArea();
            
            return new Vector4(
                safeArea.xMin,                                    // Left
                safeArea.yMin,                                    // Bottom
                Screen.width - safeArea.xMax,                     // Right
                Screen.height - safeArea.yMax                     // Top
            );
        }

        /// <summary>
        /// Check if device has a notch or cutout
        /// </summary>
        public static bool HasNotch()
        {
            var insets = GetSafeAreaInsets();
            const float threshold = 10f; // Minimum pixel difference to consider as notch
            
            return insets.x > threshold || 
                   insets.y > threshold || 
                   insets.z > threshold || 
                   insets.w > threshold;
        }

        /// <summary>
        /// Get aspect ratio of safe area
        /// </summary>
        public static float GetSafeAreaAspectRatio()
        {
            var safeArea = GetSafeArea();
            return safeArea.width / safeArea.height;
        }

        /// <summary>
        /// Convert screen point to safe area point
        /// </summary>
        public static Vector2 ScreenToSafeAreaPoint(Vector2 screenPoint)
        {
            var safeArea = GetSafeArea();
            return screenPoint - safeArea.position;
        }

        /// <summary>
        /// Convert safe area point to screen point
        /// </summary>
        public static Vector2 SafeAreaToScreenPoint(Vector2 safeAreaPoint)
        {
            var safeArea = GetSafeArea();
            return safeAreaPoint + safeArea.position;
        }

        /// <summary>
        /// Check if point is within safe area
        /// </summary>
        public static bool IsPointInSafeArea(Vector2 screenPoint)
        {
            var safeArea = GetSafeArea();
            return safeArea.Contains(screenPoint);
        }

        /// <summary>
        /// Clamp point to safe area bounds
        /// </summary>
        public static Vector2 ClampToSafeArea(Vector2 screenPoint)
        {
            var safeArea = GetSafeArea();
            
            return new Vector2(
                Mathf.Clamp(screenPoint.x, safeArea.xMin, safeArea.xMax),
                Mathf.Clamp(screenPoint.y, safeArea.yMin, safeArea.yMax)
            );
        }

        /// <summary>
        /// Clear cached safe area (force refresh on next call)
        /// </summary>
        public static void ClearCache()
        {
            cachedSafeArea = null;
            cachedScreenSize = Vector2Int.zero;
        }

        /// <summary>
        /// Get device type estimation based on aspect ratio and safe area
        /// </summary>
        public static DeviceType GetEstimatedDeviceType()
        {
            var aspectRatio = (float)Screen.width / Screen.height;
            var hasNotch = HasNotch();

            // Tablets typically have aspect ratio closer to 4:3 (1.33) or 16:10 (1.6)
            if (aspectRatio is > 1.2f and < 1.8f && !hasNotch)
            {
                return DeviceType.Tablet;
            }

            // Modern phones with notches (iPhone X and newer, most Android flagships)
            if (hasNotch)
            {
                return DeviceType.PhoneWithNotch;
            }

            // Classic phones without notches
            return DeviceType.Phone;
        }

        /// <summary>
        /// Device type enumeration
        /// </summary>
        public enum DeviceType
        {
            Phone,
            PhoneWithNotch,
            Tablet
        }
    }
}


