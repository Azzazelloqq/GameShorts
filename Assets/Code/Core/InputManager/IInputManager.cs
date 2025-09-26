using UnityEngine;

namespace Code.Core.InputManager
{
    /// <summary>
    /// Интерфейс для управления системой ввода
    /// </summary>
    public interface IInputManager
    {
        /// <summary>
        /// Устанавливает режим джойстика
        /// </summary>
        /// <param name="mode">Режим джойстика</param>
        void SetJoystickOptions(AxisOptions mode);
        
        /// <summary>
        /// Получает текущий режим джойстика
        /// </summary>
        AxisOptions CurrentJoystickOptions { get; }
        
        /// <summary>
        /// Получает значение ввода джойстика
        /// </summary>
        Vector2 GetJoystickInput();
        
        /// <summary>
        /// Проверяет, активен ли джойстик
        /// </summary>
        bool IsJoystickActive { get; }
        
        /// <summary>
        /// Инициализирует InputManager с заданным джойстиком
        /// </summary>
        /// <param name="joystick">Ссылка на джойстик</param>
        void Initialize(Joystick joystick);
    }
}
