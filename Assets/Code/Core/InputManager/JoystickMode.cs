namespace Code.Core.InputManager
{
    /// <summary>
    /// Режимы работы джойстика для разных типов игр
    /// </summary>
    public enum JoystickMode
    {
        /// <summary>
        /// Круговой джойстик - движение во все стороны
        /// </summary>
        Circular,
        
        /// <summary>
        /// Горизонтальный джойстик - только влево и вправо
        /// </summary>
        Horizontal,
        
        /// <summary>
        /// Джойстик отключен
        /// </summary>
        Disabled
    }
}
