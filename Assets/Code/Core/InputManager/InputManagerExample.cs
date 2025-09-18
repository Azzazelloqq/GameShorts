using UnityEngine;

namespace Code.Core.InputManager
{
    /// <summary>
    /// Пример использования InputManager
    /// Компонент для управления джойстиком на основе текущей игры
    /// </summary>
    public class InputManagerExample : MonoBehaviour
    {
        [SerializeField] private VariableJoystick joystick;
        
        private IInputManager _inputManager;
        
        private void Awake()
        {
            _inputManager = new InputManager();
        }
        
        private void Start()
        {
            if (joystick != null)
            {
                _inputManager.Initialize(joystick);
            }
            else
            {
                Debug.LogError("InputManagerExample: Joystick reference is not assigned!");
            }
        }
        
        /// <summary>
        /// Получить текущий ввод джойстика
        /// </summary>
        public Vector2 GetInput()
        {
            return _inputManager.GetJoystickInput();
        }
        
        private void Update()
        {
            // Пример использования - логируем ввод если джойстик активен
            if (_inputManager.IsJoystickActive)
            {
                Vector2 input = _inputManager.GetJoystickInput();
                if (input.magnitude > 0.1f)
                {
                    Debug.Log($"Joystick Input: {input}, Mode: {_inputManager.CurrentJoystickOptions}");
                }
            }
        }
    }
}
