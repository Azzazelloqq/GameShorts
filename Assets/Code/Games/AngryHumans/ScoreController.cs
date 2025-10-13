using System;
using System.Collections;
using UnityEngine;
using TMPro;
using DG.Tweening;

namespace Code.Games.AngryHumans
{
    /// <summary>
    /// Контроллер для управления отображением счета в игре
    /// </summary>
    public class ScoreController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField]
        [Tooltip("Текст для отображения текущего счета")]
        private TMP_Text _scoreText;
        
        [SerializeField]
        [Tooltip("Текст для отображения добавленных очков (popup)")]
        private TMP_Text _scorePopupText;
        
        [Header("Attempts UI")]
        [SerializeField]
        [Tooltip("Текст для отображения количества попыток")]
        private TMP_Text _attemptsText;
        
        [SerializeField]
        [Tooltip("Формат отображения попыток")]
        private string _attemptsFormat = "Attempts: {0}";
        
        [Header("Game Over UI")]
        [SerializeField]
        [Tooltip("Панель поражения")]
        private GameObject _gameOverPanel;
        
        [SerializeField]
        [Tooltip("Текст поражения")]
        private TMP_Text _gameOverText;
        
        [SerializeField]
        [Tooltip("Кнопка рестарта")]
        private UnityEngine.UI.Button _restartButton;
        
        [Header("Game Settings")]
        [SerializeField]
        [Tooltip("Максимальное количество попыток")]
        private int _maxAttempts = 5;
        
        [SerializeField]
        [Tooltip("Префаб для отображения всплывающих очков над целью")]
        private GameObject _floatingScorePrefab;
        
        [Header("Score Display Settings")]
        [SerializeField]
        [Tooltip("Формат отображения счета")]
        private string _scoreFormat = "Score: {0}";
        
        [SerializeField]
        [Tooltip("Формат отображения добавленных очков")]
        private string _scoreAddFormat = "+{0}";
        
        [Header("Animation Settings")]
        [SerializeField]
        [Tooltip("Длительность анимации изменения счета")]
        private float _scoreAnimationDuration = 0.5f;
        
        [SerializeField]
        [Tooltip("Длительность анимации всплывающих очков")]
        private float _popupAnimationDuration = 1.5f;
        
        [SerializeField]
        [Tooltip("Высота подъема всплывающих очков")]
        private float _popupFloatHeight = 50f;
        
        [SerializeField]
        [Tooltip("Масштаб анимации при получении очков")]
        private float _scorePunchScale = 1.2f;
        
        private int _currentDisplayedScore = 0;
        private int _targetScore = 0;
        private int _currentAttempts = 0;
        private bool _isGameOver = false;
        private Coroutine _scoreUpdateCoroutine;
        private Camera _gameCamera;
        private Canvas _canvas;
        
        /// <summary>
        /// Событие, вызываемое при изменении счета
        /// </summary>
        public event Action<int> OnScoreUpdated;
        
        /// <summary>
        /// Событие, вызываемое при изменении количества попыток
        /// </summary>
        public event Action<int> OnAttemptsChanged;
        
        /// <summary>
        /// Событие, вызываемое при окончании попыток (поражение)
        /// </summary>
        public event Action OnGameOver;
        
        /// <summary>
        /// Событие, вызываемое при нажатии кнопки рестарта
        /// </summary>
        public event Action OnRestartRequested;
        
        private void Awake()
        {
            // Находим камеру для конвертации позиций
            _gameCamera = Camera.main;
            _canvas = GetComponentInParent<Canvas>();
            
            // Инициализируем UI
            UpdateScoreDisplay(0);
            UpdateAttemptsDisplay(_maxAttempts);
            
            if (_scorePopupText != null)
            {
                _scorePopupText.gameObject.SetActive(false);
            }
            
            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(false);
            }
            
            // Подписываемся на кнопку рестарта
            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(HandleRestartClick);
            }
        }
        
        /// <summary>
        /// Инициализирует контроллер счета
        /// </summary>
        public void Initialize(Camera gameCamera = null)
        {
            if (gameCamera != null)
            {
                _gameCamera = gameCamera;
            }
            
            ResetScore();
            ResetAttempts();
        }
        
        /// <summary>
        /// Обновляет счет с анимацией
        /// </summary>
        public void UpdateScore(int newScore)
        {
            _targetScore = newScore;
            
            // Останавливаем текущую анимацию, если она есть
            if (_scoreUpdateCoroutine != null)
            {
                StopCoroutine(_scoreUpdateCoroutine);
            }
            
            _scoreUpdateCoroutine = StartCoroutine(AnimateScoreChange());
        }
        
        /// <summary>
        /// Добавляет очки к текущему счету
        /// </summary>
        public void AddScore(int points)
        {
            UpdateScore(_targetScore + points);
            ShowScorePopup(points);
        }
        
        /// <summary>
        /// Показывает всплывающее окно с добавленными очками
        /// </summary>
        public void ShowScorePopup(int points)
        {
            if (_scorePopupText == null)
                return;
            
            _scorePopupText.text = string.Format(_scoreAddFormat, points);
            _scorePopupText.gameObject.SetActive(true);
            
            // Анимация всплывающего текста
            var sequence = DOTween.Sequence();
            
            // Сброс позиции и масштаба
            _scorePopupText.transform.localScale = Vector3.zero;
            var originalPosition = _scorePopupText.transform.localPosition;
            
            sequence.Append(_scorePopupText.transform.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack));
            sequence.Join(_scorePopupText.transform.DOLocalMoveY(originalPosition.y + _popupFloatHeight, _popupAnimationDuration));
            // Для TextMeshPro используем CanvasGroup или меняем альфу цвета
            var startColor = _scorePopupText.color;
            sequence.Join(DOTween.To(() => _scorePopupText.alpha, x => _scorePopupText.alpha = x, 0, _popupAnimationDuration).SetDelay(0.5f));
            sequence.OnComplete(() =>
            {
                _scorePopupText.gameObject.SetActive(false);
                _scorePopupText.transform.localPosition = originalPosition;
                _scorePopupText.alpha = 1;
            });
        }
        
        /// <summary>
        /// Показывает всплывающие очки в мировых координатах
        /// </summary>
        public void ShowFloatingScore(Vector3 worldPosition, int points, Color? color = null)
        {
            if (_floatingScorePrefab == null || _canvas == null)
                return;
            
            // Создаем всплывающий текст
            var floatingScore = Instantiate(_floatingScorePrefab, _canvas.transform);
            
            // Конвертируем мировые координаты в координаты Canvas
            if (_gameCamera != null)
            {
                Vector2 screenPosition = _gameCamera.WorldToScreenPoint(worldPosition);
                
                if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    floatingScore.transform.position = screenPosition;
                }
                else if (_canvas.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _canvas.transform as RectTransform,
                        screenPosition,
                        _canvas.worldCamera,
                        out Vector2 localPoint
                    );
                    floatingScore.transform.localPosition = localPoint;
                }
            }
            
            // Настраиваем текст
            var textComponent = floatingScore.GetComponentInChildren<TMP_Text>();
            if (textComponent != null)
            {
                textComponent.text = string.Format(_scoreAddFormat, points);
                if (color.HasValue)
                {
                    textComponent.color = color.Value;
                }
                
                // Анимация всплывающего текста
                var sequence = DOTween.Sequence();
                
                floatingScore.transform.localScale = Vector3.zero;
                
                sequence.Append(floatingScore.transform.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack));
                sequence.Join(floatingScore.transform.DOMoveY(floatingScore.transform.position.y + _popupFloatHeight, _popupAnimationDuration));
                // Для TextMeshPro используем альфу цвета напрямую
                sequence.Join(DOTween.To(() => textComponent.alpha, x => textComponent.alpha = x, 0, _popupAnimationDuration).SetDelay(0.5f));
                sequence.OnComplete(() => Destroy(floatingScore));
            }
        }
        
        /// <summary>
        /// Обработчик уничтожения цели
        /// </summary>
        public void OnTargetDestroyed(Target target, int scoreValue)
        {
            // Добавляем очки
            AddScore(scoreValue);
            
            // Показываем всплывающие очки на месте цели
            if (target != null)
            {
                ShowFloatingScore(target.transform.position, scoreValue);
            }
            
            // Анимация основного счетчика
            AnimateScoreText();
        }
        
        /// <summary>
        /// Обработчик завершения структуры (бонусные очки)
        /// </summary>
        public void OnStructureCompleted(TargetStructure structure, int bonusScore)
        {
            // Добавляем бонусные очки
            AddScore(bonusScore);
            
            // Показываем всплывающие очки с особым цветом для бонусов
            if (structure != null)
            {
                ShowFloatingScore(structure.transform.position, bonusScore, Color.yellow);
            }
            
            // Особая анимация для бонусных очков
            AnimateBonusScore();
        }
        
        /// <summary>
        /// Сбрасывает счет
        /// </summary>
        public void ResetScore()
        {
            _currentDisplayedScore = 0;
            _targetScore = 0;
            UpdateScoreDisplay(0);
            
            if (_scoreUpdateCoroutine != null)
            {
                StopCoroutine(_scoreUpdateCoroutine);
                _scoreUpdateCoroutine = null;
            }
        }
        
        /// <summary>
        /// Сбрасывает попытки
        /// </summary>
        public void ResetAttempts()
        {
            _currentAttempts = _maxAttempts;
            _isGameOver = false;
            UpdateAttemptsDisplay(_currentAttempts);
            
            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// Уменьшает количество попыток на 1
        /// </summary>
        public void UseAttempt()
        {
            if (_isGameOver)
                return;
                
            _currentAttempts--;
            UpdateAttemptsDisplay(_currentAttempts);
            OnAttemptsChanged?.Invoke(_currentAttempts);
            
            if (_currentAttempts <= 0)
            {
                ShowGameOver();
            }
        }
        
        /// <summary>
        /// Показывает экран поражения
        /// </summary>
        private void ShowGameOver()
        {
            _isGameOver = true;
            
            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(true);
                
                // Отображаем финальный счет
                if (_gameOverText != null)
                {
                    _gameOverText.text = $"Поражение\n\nВаш счет: {_currentDisplayedScore}";
                }
                
                // Анимация появления панели
                if (_gameOverPanel.transform != null)
                {
                    _gameOverPanel.transform.localScale = Vector3.zero;
                    _gameOverPanel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
                }
            }
            
            OnGameOver?.Invoke();
        }
        
        /// <summary>
        /// Обработчик нажатия кнопки рестарта
        /// </summary>
        private void HandleRestartClick()
        {
            OnRestartRequested?.Invoke();
        }
        
        /// <summary>
        /// Обновляет отображение попыток
        /// </summary>
        private void UpdateAttemptsDisplay(int attempts)
        {
            if (_attemptsText != null)
            {
                _attemptsText.text = string.Format(_attemptsFormat, attempts);
            }
        }
        
        /// <summary>
        /// Анимирует изменение счета
        /// </summary>
        private IEnumerator AnimateScoreChange()
        {
            float elapsedTime = 0;
            int startScore = _currentDisplayedScore;
            
            while (elapsedTime < _scoreAnimationDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / _scoreAnimationDuration;
                
                // Используем easing для плавной анимации
                float easedProgress = Mathf.SmoothStep(0, 1, progress);
                
                _currentDisplayedScore = Mathf.RoundToInt(Mathf.Lerp(startScore, _targetScore, easedProgress));
                UpdateScoreDisplay(_currentDisplayedScore);
                
                yield return null;
            }
            
            _currentDisplayedScore = _targetScore;
            UpdateScoreDisplay(_currentDisplayedScore);
            
            OnScoreUpdated?.Invoke(_currentDisplayedScore);
            _scoreUpdateCoroutine = null;
        }
        
        /// <summary>
        /// Обновляет отображение счета
        /// </summary>
        private void UpdateScoreDisplay(int score)
        {
            if (_scoreText != null)
            {
                _scoreText.text = string.Format(_scoreFormat, score);
            }
        }
        
        /// <summary>
        /// Анимирует текст счета при получении очков
        /// </summary>
        private void AnimateScoreText()
        {
            if (_scoreText == null)
                return;
            
            // Punch scale анимация
            _scoreText.transform.DOKill();
            _scoreText.transform.localScale = Vector3.one;
            _scoreText.transform.DOPunchScale(Vector3.one * _scorePunchScale, 0.3f, 5, 0.5f);
        }
        
        /// <summary>
        /// Специальная анимация для бонусных очков
        /// </summary>
        private void AnimateBonusScore()
        {
            if (_scoreText == null)
                return;
            
            // Более выраженная анимация для бонусов
            var sequence = DOTween.Sequence();
            
            sequence.Append(_scoreText.transform.DOScale(_scorePunchScale * 1.3f, 0.2f).SetEase(Ease.OutBack));
            sequence.Append(_scoreText.transform.DOScale(1f, 0.2f).SetEase(Ease.InBack));
            
            // Можно добавить изменение цвета
            sequence.Join(_scoreText.DOColor(Color.yellow, 0.2f));
            sequence.Append(_scoreText.DOColor(Color.white, 0.3f));
        }
        
        /// <summary>
        /// Получает текущий отображаемый счет
        /// </summary>
        public int GetCurrentScore()
        {
            return _currentDisplayedScore;
        }
        
        /// <summary>
        /// Получает целевой счет (к которому идет анимация)
        /// </summary>
        public int GetTargetScore()
        {
            return _targetScore;
        }
        
        /// <summary>
        /// Получает текущее количество попыток
        /// </summary>
        public int GetCurrentAttempts()
        {
            return _currentAttempts;
        }
        
        /// <summary>
        /// Проверяет, закончилась ли игра
        /// </summary>
        public bool IsGameOver => _isGameOver;
        
        /// <summary>
        /// Устанавливает максимальное количество попыток
        /// </summary>
        public void SetMaxAttempts(int maxAttempts)
        {
            _maxAttempts = maxAttempts;
            // Если игра еще не началась или текущие попытки больше новых максимальных
            if (_currentAttempts == 0 || _currentAttempts > maxAttempts)
            {
                _currentAttempts = maxAttempts;
            }
            UpdateAttemptsDisplay(_currentAttempts);
        }
        
        private void OnDestroy()
        {
            // Очищаем анимации
            DOTween.Kill(this);
            
            if (_scoreUpdateCoroutine != null)
            {
                StopCoroutine(_scoreUpdateCoroutine);
            }
            
            // Отписываемся от кнопки рестарта
            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(HandleRestartClick);
            }
        }
    }
}
