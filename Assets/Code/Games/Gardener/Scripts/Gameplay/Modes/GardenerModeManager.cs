using System;
using System.Collections.Generic;
using Code.Core.BaseDMDisposable.Scripts;
using R3;

namespace GameShorts.Gardener.Gameplay.Modes
{
    /// <summary>
    /// Менеджер режимов игры Gardener
    /// Управляет переключением между режимами и оповещает UI об изменениях
    /// </summary>
    internal class GardenerModeManager : BaseDisposable
    {
        private readonly Dictionary<string, IGardenerMode> _modes = new Dictionary<string, IGardenerMode>();
        private IGardenerMode _currentMode;
        private readonly ReactiveProperty<IGardenerMode> _activeModeProperty = new ReactiveProperty<IGardenerMode>();
        
        public ReadOnlyReactiveProperty<IGardenerMode> ActiveMode => _activeModeProperty;
        public IGardenerMode CurrentMode => _currentMode;
        
        /// <summary>
        /// Регистрирует новый режим в менеджере
        /// </summary>
        public void RegisterMode(IGardenerMode mode)
        {
            if (mode == null)
                throw new ArgumentNullException(nameof(mode));
                
            if (_modes.ContainsKey(mode.ModeName))
            {
                UnityEngine.Debug.LogWarning($"Mode {mode.ModeName} is already registered!");
                return;
            }
            
            _modes[mode.ModeName] = mode;
        }
        
        /// <summary>
        /// Переключает на указанный режим
        /// </summary>
        public void SwitchMode(string modeName)
        {
            if (!_modes.TryGetValue(modeName, out var newMode))
            {
                UnityEngine.Debug.LogError($"Mode {modeName} is not registered!");
                return;
            }
            
            if (_currentMode == newMode)
                return;
            
            _currentMode?.OnExit();
            _currentMode = newMode;
            _currentMode.OnEnter();
            _activeModeProperty.Value = _currentMode;
        }
        
        /// <summary>
        /// Возвращает режим по имени
        /// </summary>
        public IGardenerMode GetMode(string modeName)
        {
            return _modes.TryGetValue(modeName, out var mode) ? mode : null;
        }
        
        protected override void OnDispose()
        {
            _currentMode?.OnExit();
            
            foreach (var mode in _modes.Values)
            {
                mode?.Dispose();
            }
            
            _modes.Clear();
            _activeModeProperty?.Dispose();
        }
    }
}

