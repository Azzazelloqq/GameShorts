using System;
using UnityEngine;

namespace Code.Core.InputManager
{
    /// <summary>
    /// Реализация системы управления вводом
    /// </summary>
    public class InputManager : IInputManager
    {
        private Joystick _joystick;
        private AxisOptions _currentMode = AxisOptions.None;
        
        public AxisOptions CurrentJoystickOptions => _currentMode;
        public bool IsJoystickActive => _currentMode != AxisOptions.None && _joystick != null;

        public void Initialize(Joystick joystick)
        {
            _joystick = joystick;
            
            if (_joystick != null)
            {
                // По умолчанию отключаем джойстик
                SetJoystickOptions(AxisOptions.None);
            }
        }

        public void SetJoystickOptions(AxisOptions mode)
        {
            if (_joystick == null)
            {
                Debug.LogWarning("InputManager: Joystick is not initialized");
                return;
            }

            _currentMode = mode;
            
            switch (mode)
            {
                case AxisOptions.Both:
                case AxisOptions.Horizontal:
                case AxisOptions.Vertical:
                    _joystick.SetAxisOptions(mode);
                    _joystick.gameObject.SetActive(true);
                    break;
                case AxisOptions.None:
                    _joystick.gameObject.SetActive(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        public Vector2 GetJoystickInput()
        {
            if (!IsJoystickActive)
                return Vector2.zero;

            Vector2 input = new Vector2(_joystick.Horizontal, _joystick.Vertical);
            
            // Для горизонтального режима ограничиваем вертикальное движение
            if (_currentMode == AxisOptions.Horizontal)
            {
                input.y = 0f;
            }
            if (_currentMode == AxisOptions.Vertical)
            {
                input.x = 0f;
            }
            
            return input;
        }
    }
}
