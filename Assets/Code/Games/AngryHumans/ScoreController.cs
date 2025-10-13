using System;
using System.Collections;
using UnityEngine;
using TMPro;
using DG.Tweening;

namespace Code.Games.AngryHumans
{
internal class ScoreController : MonoBehaviour
{
	[Header("UI References")]
	[SerializeField]
	[Tooltip("Text for displaying current score")]
	private TMP_Text _scoreText;

	[SerializeField]
	[Tooltip("Text for displaying added points (popup)")]
	private TMP_Text _scorePopupText;

	[Header("Attempts UI")]
	[SerializeField]
	[Tooltip("Text for displaying attempts count")]
	private TMP_Text _attemptsText;

	[SerializeField]
	[Tooltip("Attempts display format")]
	private string _attemptsFormat = "Attempts: {0}";

	[Header("Game Over UI")]
	[SerializeField]
	[Tooltip("Game over panel")]
	private GameObject _gameOverPanel;

	[SerializeField]
	[Tooltip("Game over text")]
	private TMP_Text _gameOverText;

	[SerializeField]
	[Tooltip("Restart button")]
	private UnityEngine.UI.Button _restartButton;

	[Header("Game Settings")]
	[SerializeField]
	[Tooltip("Maximum number of attempts")]
	private int _maxAttempts = 5;

	[SerializeField]
	[Tooltip("Prefab for displaying floating score above targets")]
	private GameObject _floatingScorePrefab;

	[Header("Score Display Settings")]
	[SerializeField]
	[Tooltip("Score display format")]
	private string _scoreFormat = "Score: {0}";

	[SerializeField]
	[Tooltip("Added score display format")]
	private string _scoreAddFormat = "+{0}";

	[Header("Animation Settings")]
	[SerializeField]
	[Tooltip("Score change animation duration")]
	private float _scoreAnimationDuration = 0.5f;

	[SerializeField]
	[Tooltip("Floating score animation duration")]
	private float _popupAnimationDuration = 1.5f;

	[SerializeField]
	[Tooltip("Floating score rise height")]
	private float _popupFloatHeight = 50f;

	[SerializeField]
	[Tooltip("Score punch animation scale")]
	private float _scorePunchScale = 1.2f;

	private int _currentDisplayedScore = 0;
	private int _targetScore = 0;
	private int _currentAttempts = 0;
	private bool _isGameOver = false;
	private Coroutine _scoreUpdateCoroutine;
	private Camera _gameCamera;
	private Canvas _canvas;

	public event Action<int> OnScoreUpdated;
	public event Action<int> OnAttemptsChanged;
	public event Action OnGameOver;
	public event Action OnRestartRequested;

	private void Awake()
	{
		_gameCamera = Camera.main;
		_canvas = GetComponentInParent<Canvas>();

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

		if (_restartButton != null)
		{
			_restartButton.onClick.AddListener(HandleRestartClick);
		}
	}

	public void Initialize(Camera gameCamera = null)
	{
		if (gameCamera != null)
		{
			_gameCamera = gameCamera;
		}

		ResetScore();
		ResetAttempts();
	}

	public void UpdateScore(int newScore)
	{
		_targetScore = newScore;

		if (_scoreUpdateCoroutine != null)
		{
			StopCoroutine(_scoreUpdateCoroutine);
		}

		_scoreUpdateCoroutine = StartCoroutine(AnimateScoreChange());
	}

	public void AddScore(int points)
	{
		UpdateScore(_targetScore + points);
		ShowScorePopup(points);
	}

	public void ShowScorePopup(int points)
	{
		if (_scorePopupText == null)
		{
			return;
		}

		_scorePopupText.text = string.Format(_scoreAddFormat, points);
		_scorePopupText.gameObject.SetActive(true);

		var sequence = DOTween.Sequence();
		_scorePopupText.transform.localScale = Vector3.zero;
		var originalPosition = _scorePopupText.transform.localPosition;

		sequence.Append(_scorePopupText.transform.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack));
		sequence.Join(
			_scorePopupText.transform.DOLocalMoveY(originalPosition.y + _popupFloatHeight, _popupAnimationDuration));
		sequence.Join(DOTween.To(() => _scorePopupText.alpha, x => _scorePopupText.alpha = x, 0, _popupAnimationDuration)
			.SetDelay(0.5f));
		sequence.OnComplete(() =>
		{
			_scorePopupText.gameObject.SetActive(false);
			_scorePopupText.transform.localPosition = originalPosition;
			_scorePopupText.alpha = 1;
		});
	}

	public void ShowFloatingScore(Vector3 worldPosition, int points, Color? color = null)
	{
		if (_floatingScorePrefab == null || _canvas == null)
		{
			return;
		}

		var floatingScore = Instantiate(_floatingScorePrefab, _canvas.transform);

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
					out var localPoint
				);
				floatingScore.transform.localPosition = localPoint;
			}
		}

		var textComponent = floatingScore.GetComponentInChildren<TMP_Text>();
		if (textComponent != null)
		{
			textComponent.text = string.Format(_scoreAddFormat, points);
			if (color.HasValue)
			{
				textComponent.color = color.Value;
			}

			var sequence = DOTween.Sequence();
			floatingScore.transform.localScale = Vector3.zero;

			sequence.Append(floatingScore.transform.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack));
			sequence.Join(floatingScore.transform.DOMoveY(floatingScore.transform.position.y + _popupFloatHeight,
				_popupAnimationDuration));
			sequence.Join(DOTween.To(() => textComponent.alpha, x => textComponent.alpha = x, 0, _popupAnimationDuration)
				.SetDelay(0.5f));
			sequence.OnComplete(() => Destroy(floatingScore));
		}
	}

	public void OnTargetDestroyed(Target target, int scoreValue)
	{
		AddScore(scoreValue);

		if (target != null)
		{
			ShowFloatingScore(target.transform.position, scoreValue);
		}

		AnimateScoreText();
	}

	public void OnStructureCompleted(TargetStructure structure, int bonusScore)
	{
		AddScore(bonusScore);

		if (structure != null)
		{
			ShowFloatingScore(structure.transform.position, bonusScore, Color.yellow);
		}

		AnimateBonusScore();
	}

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

	public void UseAttempt()
	{
		if (_isGameOver)
		{
			return;
		}

		_currentAttempts--;
		UpdateAttemptsDisplay(_currentAttempts);
		OnAttemptsChanged?.Invoke(_currentAttempts);

		if (_currentAttempts <= 0)
		{
			ShowGameOver();
		}
	}

	private void ShowGameOver()
	{
		_isGameOver = true;

		if (_gameOverPanel != null)
		{
			_gameOverPanel.SetActive(true);

			if (_gameOverText != null)
			{
				_gameOverText.text = $"Game Over\n\nYour Score: {_currentDisplayedScore}";
			}

			if (_gameOverPanel.transform != null)
			{
				_gameOverPanel.transform.localScale = Vector3.zero;
				_gameOverPanel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
			}
		}

		OnGameOver?.Invoke();
	}

	private void HandleRestartClick()
	{
		OnRestartRequested?.Invoke();
	}

	private void UpdateAttemptsDisplay(int attempts)
	{
		if (_attemptsText != null)
		{
			_attemptsText.text = string.Format(_attemptsFormat, attempts);
		}
	}

	private IEnumerator AnimateScoreChange()
	{
		float elapsedTime = 0;
		var startScore = _currentDisplayedScore;

		while (elapsedTime < _scoreAnimationDuration)
		{
			elapsedTime += Time.deltaTime;
			var progress = elapsedTime / _scoreAnimationDuration;
			var easedProgress = Mathf.SmoothStep(0, 1, progress);

			_currentDisplayedScore = Mathf.RoundToInt(Mathf.Lerp(startScore, _targetScore, easedProgress));
			UpdateScoreDisplay(_currentDisplayedScore);

			yield return null;
		}

		_currentDisplayedScore = _targetScore;
		UpdateScoreDisplay(_currentDisplayedScore);

		OnScoreUpdated?.Invoke(_currentDisplayedScore);
		_scoreUpdateCoroutine = null;
	}

	private void UpdateScoreDisplay(int score)
	{
		if (_scoreText != null)
		{
			_scoreText.text = string.Format(_scoreFormat, score);
		}
	}

	private void AnimateScoreText()
	{
		if (_scoreText == null)
		{
			return;
		}

		_scoreText.transform.DOKill();
		_scoreText.transform.localScale = Vector3.one;
		_scoreText.transform.DOPunchScale(Vector3.one * _scorePunchScale, 0.3f, 5, 0.5f);
	}

	private void AnimateBonusScore()
	{
		if (_scoreText == null)
		{
			return;
		}

		var sequence = DOTween.Sequence();

		sequence.Append(_scoreText.transform.DOScale(_scorePunchScale * 1.3f, 0.2f).SetEase(Ease.OutBack));
		sequence.Append(_scoreText.transform.DOScale(1f, 0.2f).SetEase(Ease.InBack));
		sequence.Join(_scoreText.DOColor(Color.yellow, 0.2f));
		sequence.Append(_scoreText.DOColor(Color.white, 0.3f));
	}

	public int GetCurrentScore()
	{
		return _currentDisplayedScore;
	}

	public int GetTargetScore()
	{
		return _targetScore;
	}

	public int GetCurrentAttempts()
	{
		return _currentAttempts;
	}

	public bool IsGameOver => _isGameOver;

	public void SetMaxAttempts(int maxAttempts)
	{
		_maxAttempts = maxAttempts;
		if (_currentAttempts == 0 || _currentAttempts > maxAttempts)
		{
			_currentAttempts = maxAttempts;
		}

		UpdateAttemptsDisplay(_currentAttempts);
	}

	private void OnDestroy()
	{
		DOTween.Kill(this);

		if (_scoreUpdateCoroutine != null)
		{
			StopCoroutine(_scoreUpdateCoroutine);
		}

		if (_restartButton != null)
		{
			_restartButton.onClick.RemoveListener(HandleRestartClick);
		}
	}
}
}