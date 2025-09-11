using UnityEngine;

namespace Code.Core.InputManager
{
    /// <summary>
    /// Реализация системы управления вводом
    /// </summary>
    public class InputManager : IInputManager
    {
        private VariableJoystick _joystick;
        private JoystickMode _currentMode = JoystickMode.Disabled;
        
        public JoystickMode CurrentJoystickMode => _currentMode;
        public bool IsJoystickActive => _currentMode != JoystickMode.Disabled && _joystick != null;

        public void Initialize(VariableJoystick joystick)
        {
            _joystick = joystick;
            
            if (_joystick != null)
            {
                // По умолчанию отключаем джойстик
                SetJoystickMode(JoystickMode.Disabled);
            }
        }

        public void SetJoystickMode(JoystickMode mode)
        {
            if (_joystick == null)
            {
                Debug.LogWarning("InputManager: Joystick is not initialized");
                return;
            }

            _currentMode = mode;
            
            switch (mode)
            {
                case JoystickMode.Circular:
                    _joystick.SetMode(JoystickType.Fixed);
                    _joystick.gameObject.SetActive(true);
                    break;
                    
                case JoystickMode.Horizontal:
                    _joystick.SetMode(JoystickType.Fixed);
                    _joystick.gameObject.SetActive(true);
                    // Для горизонтального режима можно добавить дополнительную логику
                    break;
                    
                case JoystickMode.Disabled:
                    _joystick.gameObject.SetActive(false);
                    break;
            }
        }

        public Vector2 GetJoystickInput()
        {
            if (!IsJoystickActive)
                return Vector2.zero;

            Vector2 input = new Vector2(_joystick.Horizontal, _joystick.Vertical);
            
            // Для горизонтального режима ограничиваем вертикальное движение
            if (_currentMode == JoystickMode.Horizontal)
            {
                input.y = 0f;
            }
            
            return input;
        }
    }
}
