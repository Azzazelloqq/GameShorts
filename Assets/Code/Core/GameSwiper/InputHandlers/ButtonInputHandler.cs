using UnityEngine;
using UnityEngine.UI;

namespace Code.Core.GameSwiper.InputHandlers
{
/// <summary>
/// Handles button input for GameSwiper.
/// Manages next/previous navigation buttons.
/// </summary>
public class ButtonInputHandler : GameSwiperInputHandler
{
	[Header("Buttons")]
	[SerializeField]
	private Button _nextButton;

	[SerializeField]
	private Button _previousButton;

	[Header("Visual Settings")]
	[SerializeField]
	private bool _autoHideUnavailableButtons = true;

	[SerializeField]
	private bool _autoDisableUnavailableButtons = true;

	private bool _isEnabled = true;
	private bool _canGoNext = true;
	private bool _canGoPrevious = true;

	public override bool IsEnabled
	{
		get => _isEnabled;
		set
		{
			_isEnabled = value;
			UpdateButtonStates();
		}
	}

	private void Awake()
	{
		_nextButton.onClick.AddListener(OnNextButtonClicked);

		_previousButton.onClick.AddListener(OnPreviousButtonClicked);
	}

	private void OnDestroy()
	{
		_nextButton.onClick.RemoveListener(OnNextButtonClicked);

		_previousButton.onClick.RemoveListener(OnPreviousButtonClicked);
	}

	public override void SetNavigationAvailability(bool canGoNext, bool canGoPrevious)
	{
		_canGoNext = canGoNext;
		_canGoPrevious = canGoPrevious;
		UpdateButtonStates();
	}

	public override void ResetInputState()
	{
		UpdateButtonStates();
	}

	private void OnNextButtonClicked()
	{
		if (!_isEnabled || !_canGoNext)
		{
			return;
		}

		ReportDragProgress(1f); // Full progress towards next
		RequestNextGame();
		ReportDragProgress(0f); // Reset to neutral
	}

	private void OnPreviousButtonClicked()
	{
		if (!_isEnabled || !_canGoPrevious)
		{
			return;
		}

		// Quick transition - report instant progress
		ReportDragProgress(-1f); // Full progress towards previous
		RequestPreviousGame();
		ReportDragProgress(0f); // Reset to neutral
	}

	private void UpdateButtonStates()
	{
		if (_nextButton != null)
		{
			var shouldBeActive = _isEnabled && _canGoNext;

			if (_autoDisableUnavailableButtons)
			{
				_nextButton.interactable = shouldBeActive;
			}

			if (_autoHideUnavailableButtons)
			{
				_nextButton.gameObject.SetActive(shouldBeActive || !_autoDisableUnavailableButtons);
			}
		}

		if (_previousButton != null)
		{
			var shouldBeActive = _isEnabled && _canGoPrevious;

			if (_autoDisableUnavailableButtons)
			{
				_previousButton.interactable = shouldBeActive;
			}

			if (_autoHideUnavailableButtons)
			{
				_previousButton.gameObject.SetActive(shouldBeActive || !_autoDisableUnavailableButtons);
			}
		}
	}

	/// <summary>
	/// Set the next button reference at runtime
	/// </summary>
	public void SetNextButton(Button button)
	{
		_nextButton.onClick.RemoveListener(OnNextButtonClicked);

		_nextButton = button;

		_nextButton.onClick.AddListener(OnNextButtonClicked);

		UpdateButtonStates();
	}

	/// <summary>
	/// Set the previous button reference at runtime
	/// </summary>
	public void SetPreviousButton(Button button)
	{
		_previousButton.onClick.RemoveListener(OnPreviousButtonClicked);

		_previousButton = button;

		_previousButton.onClick.AddListener(OnPreviousButtonClicked);

		UpdateButtonStates();
	}
}
}