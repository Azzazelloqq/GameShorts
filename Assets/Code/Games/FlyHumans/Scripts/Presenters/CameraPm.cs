using Disposable;
using DG.Tweening;
using Disposable;
using GameShorts.FlyHumans.View;
using UnityEngine;

namespace GameShorts.FlyHumans.Presenters
{
    /// <summary>
    /// Презентер для управления камерой
    /// </summary>
    internal class CameraPm : DisposableBase
    {
        internal struct Ctx
        {
            public CameraView cameraView;
            public Transform targetTransform; // За кем следит камера (персонаж)
        }

        private readonly Ctx _ctx;
        private Vector3 _currentCameraOffset;
        private bool _isCameraAnimating;
        private Sequence _cameraSequence;
        private Quaternion _initialCameraRotation;
        private Vector3 _initialCameraOffset;

        public CameraPm(Ctx ctx)
        {
            _ctx = ctx;
            Initialize();
        }

        private void Initialize()
        {
            if (_ctx.cameraView == null || _ctx.cameraView.CameraTransform == null) return;
            
            // Запоминаем начальный offset камеры в мировых координатах
            if (_ctx.targetTransform != null)
            {
                _currentCameraOffset = _ctx.cameraView.CameraTransform.position - _ctx.targetTransform.position;
                _initialCameraOffset = _currentCameraOffset; // Сохраняем начальный offset
            }
            else
            {
                _currentCameraOffset = _ctx.cameraView.CameraFollowOffset;
                _initialCameraOffset = _ctx.cameraView.CameraFollowOffset;
            }
            
            // Запоминаем начальное вращение камеры
            _initialCameraRotation = _ctx.cameraView.CameraTransform.rotation;
            
            _isCameraAnimating = false;
        }

        /// <summary>
        /// Обновление позиции камеры (вызывается каждый кадр)
        /// </summary>
        public void UpdateCameraPosition()
        {
            if (_ctx.cameraView == null || _ctx.cameraView.CameraTransform == null || _ctx.targetTransform == null) 
                return;
            
            // Камера всегда следит за целью с текущим offset (в мировых координатах)
            Vector3 targetOffset = _isCameraAnimating ? _currentCameraOffset : _ctx.cameraView.CameraFollowOffset;
            _ctx.cameraView.CameraTransform.position = _ctx.targetTransform.position + targetOffset;
        }

        /// <summary>
        /// Запускает анимацию камеры (отдаление и поворот)
        /// </summary>
        public void AnimateCamera()
        {
            if (_ctx.cameraView == null || _ctx.cameraView.CameraTransform == null) return;
            
            _isCameraAnimating = true;
            
            Quaternion targetRotation = Quaternion.Euler(_ctx.cameraView.CameraTargetRotation);
            
            // Вычисляем промежуточную позицию (отдаленную)
            Vector3 zoomedOutOffset = _currentCameraOffset.normalized * 
                (_currentCameraOffset.magnitude + _ctx.cameraView.CameraZoomOutDistance);
            
            // Создаем последовательность анимаций
            _cameraSequence = DOTween.Sequence();
            
            // Шаг 1: Отдаляем камеру
            _cameraSequence.Append(
                DOTween.To(() => _currentCameraOffset, x => _currentCameraOffset = x, 
                    zoomedOutOffset, _ctx.cameraView.CameraZoomOutDuration)
                    .SetEase(_ctx.cameraView.CameraZoomEase)
            );
            
            // Шаг 2: Одновременно поворачиваем камеру и перемещаем к целевой позиции
            _cameraSequence.Append(
                DOTween.To(() => _currentCameraOffset, x => _currentCameraOffset = x, 
                    _ctx.cameraView.CameraFollowOffset, _ctx.cameraView.CameraRotationDuration)
                    .SetEase(_ctx.cameraView.CameraRotationEase)
            );
            _cameraSequence.Join(
                _ctx.cameraView.CameraTransform.DORotateQuaternion(targetRotation, 
                    _ctx.cameraView.CameraRotationDuration)
                    .SetEase(_ctx.cameraView.CameraRotationEase)
            );
            
            // После завершения анимации переключаемся на использование стандартного offset
            _cameraSequence.OnComplete(() => _isCameraAnimating = false);
        }

        /// <summary>
        /// Останавливает анимацию камеры
        /// </summary>
        public void StopCameraAnimation()
        {
            if (_cameraSequence != null && _cameraSequence.IsActive())
            {
                _cameraSequence.Kill();
                _cameraSequence = null;
                _isCameraAnimating = false;
            }
        }
        
        /// <summary>
        /// Сбрасывает камеру в начальное положение
        /// </summary>
        public void ResetCamera()
        {
            StopCameraAnimation();
            
            if (_ctx.cameraView == null || _ctx.cameraView.CameraTransform == null) return;
            
            // Восстанавливаем начальный offset
            _currentCameraOffset = _initialCameraOffset;
            
            // Сбрасываем вращение камеры в начальное
            _ctx.cameraView.CameraTransform.rotation = _initialCameraRotation;
            
            // Явно устанавливаем позицию камеры относительно персонажа
            if (_ctx.targetTransform != null)
            {
                _ctx.cameraView.CameraTransform.position = _ctx.targetTransform.position + _initialCameraOffset;
            }
            
            _isCameraAnimating = false;
            
            Debug.Log($"Camera reset: offset={_initialCameraOffset}, rotation={_initialCameraRotation.eulerAngles}");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            
            StopCameraAnimation();
        }
    }
}

