using System;
using UnityEngine;

namespace Code.Core.GameSwiper.InputHandlers
{
/// <summary>
/// Base contract for all GameSwiper input handlers.
/// Defines the interface that GameSwiper uses to receive input events.
/// </summary>
public abstract class GameSwiperInputHandler : MonoBehaviour
{
	/// <summary>
	/// Event fired when the next game is requested by the input handler
	/// </summary>
	public event Action OnNextGameRequested;

	/// <summary>
	/// Event fired when the previous game is requested by the input handler
	/// </summary>
	public event Action OnPreviousGameRequested;

	/// <summary>
	/// Event for real-time drag/interaction progress (-1 to 1)
	/// Negative values indicate movement towards previous, positive towards next
	/// 0 means no movement/neutral position
	/// </summary>
	public event Action<float> OnDragProgress;

	/// <summary>
	/// Enable or disable this input handler
	/// </summary>
	public abstract bool IsEnabled { get; set; }

	/// <summary>
	/// Set whether navigation is available in certain directions
	/// </summary>
	/// <param name="canGoNext">Whether next navigation is available</param>
	/// <param name="canGoPrevious">Whether previous navigation is available</param>
	public abstract void SetNavigationAvailability(bool canGoNext, bool canGoPrevious);

	/// <summary>
	/// Reset any ongoing input state
	/// </summary>
	public abstract void ResetInputState();

	/// <summary>
	/// Fire next game requested event
	/// </summary>
	protected void RequestNextGame()
	{
		OnNextGameRequested?.Invoke();
	}

	/// <summary>
	/// Fire previous game requested event
	/// </summary>
	protected void RequestPreviousGame()
	{
		OnPreviousGameRequested?.Invoke();
	}

	/// <summary>
	/// Report drag/interaction progress
	/// </summary>
	protected void ReportDragProgress(float progress)
	{
		OnDragProgress?.Invoke(progress);
	}
}
}