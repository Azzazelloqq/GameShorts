using UnityEngine;

namespace SurvivalDuck
{
    /// <summary>
    /// Утилита для отладки и визуализации работы PlayerController.
    /// </summary>
    public class PlayerDebug : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private bool logMovementInfo = false;
        
        [Header("Gizmo Settings")]
        [SerializeField] private Color movementDirectionColor = Color.green;
        [SerializeField] private Color velocityColor = Color.blue;
        [SerializeField] private float arrowLength = 2f;
        
        private PlayerController _playerController;
        private FloatingJoystick _joystick;
        private GUIStyle _guiStyle;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
            if (_playerController == null)
            {
                Debug.LogWarning("PlayerDebug: PlayerController not found!");
            }
        }

        private void Start()
        {
            _joystick = FindObjectOfType<FloatingJoystick>();
            
            // Настройка стиля GUI
            _guiStyle = new GUIStyle();
            _guiStyle.fontSize = 16;
            _guiStyle.normal.textColor = Color.white;
            _guiStyle.padding = new RectOffset(10, 10, 10, 10);
        }

        private void Update()
        {
            if (logMovementInfo && _playerController != null && _playerController.IsMoving())
            {
                Vector3 velocity = _playerController.GetVelocity();
                Debug.Log($"Player Moving - Velocity: {velocity.magnitude:F2} m/s, Direction: {velocity.normalized}");
            }
        }

        private void OnGUI()
        {
            if (!showDebugInfo || _playerController == null) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 300));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("=== Player Debug ===", _guiStyle);
            GUILayout.Space(10);
            
            // Информация о состоянии
            GUILayout.Label($"Is Moving: {_playerController.IsMoving()}", _guiStyle);
            GUILayout.Label($"Position: {transform.position.ToString("F2")}", _guiStyle);
            GUILayout.Label($"Rotation: {transform.eulerAngles.y:F1}°", _guiStyle);
            
            // Информация о скорости
            Vector3 velocity = _playerController.GetVelocity();
            GUILayout.Label($"Velocity: {velocity.magnitude:F2} m/s", _guiStyle);
            
            // Информация о джойстике
            if (_joystick != null)
            {
                GUILayout.Space(10);
                GUILayout.Label("=== Joystick Input ===", _guiStyle);
                GUILayout.Label($"Horizontal: {_joystick.Horizontal:F2}", _guiStyle);
                GUILayout.Label($"Vertical: {_joystick.Vertical:F2}", _guiStyle);
                Vector2 direction = _joystick.Direction;
                GUILayout.Label($"Magnitude: {direction.magnitude:F2}", _guiStyle);
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos || _playerController == null) return;

            Vector3 position = transform.position + Vector3.up * 0.1f;
            
            // Отрисовка направления движения (velocity)
            Vector3 velocity = _playerController.GetVelocity();
            if (velocity.magnitude > 0.1f)
            {
                Gizmos.color = velocityColor;
                Vector3 velocityDirection = velocity.normalized;
                Gizmos.DrawRay(position, velocityDirection * arrowLength);
                DrawArrowHead(position + velocityDirection * arrowLength, velocityDirection, 0.3f, velocityColor);
            }

            // Отрисовка направления взгляда
            Gizmos.color = Color.red;
            Vector3 forward = transform.forward;
            Gizmos.DrawRay(position, forward * (arrowLength * 0.8f));
            DrawArrowHead(position + forward * (arrowLength * 0.8f), forward, 0.25f, Color.red);

            // Если джойстик активен, показываем raw input
            if (_joystick != null)
            {
                Vector2 input = _joystick.Direction;
                if (input.magnitude > 0.1f)
                {
                    Gizmos.color = movementDirectionColor;
                    Vector3 inputDir3D = new Vector3(input.x, 0, input.y).normalized;
                    Vector3 inputWorldDir = Camera.main != null 
                        ? Camera.main.transform.TransformDirection(new Vector3(input.x, 0, input.y)).normalized
                        : inputDir3D;
                    inputWorldDir.y = 0;
                    inputWorldDir.Normalize();
                    
                    Gizmos.DrawRay(position, inputWorldDir * (arrowLength * 1.2f));
                    DrawArrowHead(position + inputWorldDir * (arrowLength * 1.2f), inputWorldDir, 0.35f, movementDirectionColor);
                }
            }

            // Круг под персонажем
            Gizmos.color = _playerController.IsMoving() ? Color.green : Color.gray;
            DrawCircle(transform.position + Vector3.up * 0.05f, 0.6f, 20);
        }

        private void DrawArrowHead(Vector3 position, Vector3 direction, float size, Color color)
        {
            Gizmos.color = color;
            Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + 30, 0) * Vector3.forward;
            Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - 30, 0) * Vector3.forward;
            
            Gizmos.DrawRay(position, right * size);
            Gizmos.DrawRay(position, left * size);
        }

        private void DrawCircle(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }

        // Публичные методы для переключения отладки
        public void ToggleDebugInfo()
        {
            showDebugInfo = !showDebugInfo;
        }

        public void ToggleGizmos()
        {
            showGizmos = !showGizmos;
        }

        public void ToggleMovementLogging()
        {
            logMovementInfo = !logMovementInfo;
        }
    }
}

