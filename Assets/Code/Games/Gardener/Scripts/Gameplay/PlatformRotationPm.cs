using Disposable;
using GameShorts.Gardener.Data;
using GameShorts.Gardener.View;
using UnityEngine;

namespace GameShorts.Gardener.Gameplay
{
    /// <summary>
    /// Presenter для управления вращением платформы
    /// Обрабатывает события от View и применяет вращение к Transform
    /// </summary>
    internal class PlatformRotationPm : DisposableBase
    {
        internal struct Ctx
        {
            public PlatformRotationView view;
            public Transform platformTransform;
            public GardenerGameSettings gameSettings;
            public Camera camera;
        }

        private readonly Ctx _ctx;
        private Vector3 _currentEulerAngles;
        private Vector3 _rotationCenter;

        public PlatformRotationPm(Ctx ctx)
        {
            _ctx = ctx;
            
            // Сохраняем текущее вращение платформы
            _currentEulerAngles = _ctx.platformTransform.localEulerAngles;
            
            // Вычисляем центр платформы для вращения вокруг него
            _rotationCenter = CalculateRotationCenter();
            
            // Подписываемся на события View
            _ctx.view.OnDragStarted += OnDragStarted;
            _ctx.view.OnDragDelta += OnDragDelta;
            _ctx.view.OnDragEnded += OnDragEnded;
        }

        private void OnDragStarted()
        {
            // Обновляем центр вращения при начале зажатия
            _rotationCenter = CalculateRotationCenter();
            
            // Можно добавить визуальную обратную связь при начале вращения
        }
        
        /// <summary>
        /// Вычисляет центр платформы для вращения вокруг него
        /// Использует центр bounds всех дочерних рендереров
        /// </summary>
        private Vector3 CalculateRotationCenter()
        {
            // Пытаемся найти центр через Renderer bounds
            var renderers = _ctx.platformTransform.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds combinedBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    combinedBounds.Encapsulate(renderers[i].bounds);
                }
                return combinedBounds.center;
            }
            
            // Если рендереров нет, используем позицию самого Transform
            return _ctx.platformTransform.position;
        }

        private void OnDragDelta(Vector2 delta)
        {
            // Горизонтальное движение мыши/пальца → вращение вокруг оси Y (бесконечно)
            // Инвертируем для более интуитивного управления (тянем вправо = крутится влево)
            float yawRotation = -delta.x * _ctx.gameSettings.RotationSensitivity;
            
            // Вертикальное движение мыши/пальца → наклон относительно камеры (с ограничениями)
            float pitchRotation = -delta.y * _ctx.gameSettings.RotationSensitivity;
            
            // Вычисляем предварительные углы для проверки ограничений
            Vector3 testEulerAngles = _currentEulerAngles;
            testEulerAngles.x += pitchRotation;
            testEulerAngles.x = NormalizeAngle(testEulerAngles.x);
            
            // Ограничиваем наклон по вертикали
            float clampedPitch = Mathf.Clamp(
                testEulerAngles.x, 
                _ctx.gameSettings.MinVerticalAngle, 
                _ctx.gameSettings.MaxVerticalAngle
            );
            
            // Вычисляем реальный pitchRotation с учетом ограничений
            float actualPitchRotation = clampedPitch - _currentEulerAngles.x;
            
            // Обновляем текущие углы
            _currentEulerAngles.y += yawRotation;
            _currentEulerAngles.x = clampedPitch;
            
            // Вращаем вокруг центра платформы по оси Y (глобальная ось вверх)
            if (Mathf.Abs(yawRotation) > 0.001f)
            {
                _ctx.platformTransform.RotateAround(_rotationCenter, Vector3.up, yawRotation);
            }
            
            // Вращаем вокруг центра платформы по горизонтальной оси камеры (наклон относительно камеры)
            if (Mathf.Abs(actualPitchRotation) > 0.001f && _ctx.camera != null)
            {
                _ctx.platformTransform.RotateAround(_rotationCenter, _ctx.camera.transform.right, actualPitchRotation);
            }
        }

        private void OnDragEnded()
        {
            // Можно добавить инерцию или плавное возвращение в исходное положение
        }

        /// <summary>
        /// Нормализует угол в диапазон от -180 до 180 градусов
        /// </summary>
        private float NormalizeAngle(float angle)
        {
            while (angle > 180f)
                angle -= 360f;
            while (angle < -180f)
                angle += 360f;
            return angle;
        }

        protected override void OnDispose()
        {
            // Отписываемся от событий View
            if (_ctx.view != null)
            {
                _ctx.view.OnDragStarted -= OnDragStarted;
                _ctx.view.OnDragDelta -= OnDragDelta;
                _ctx.view.OnDragEnded -= OnDragEnded;
            }
            
            base.OnDispose();
        }
    }
}

