using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Code.Core.GameSwiper
{
/// <summary>
/// Интерфейс для игр, поддерживающих свайп-навигацию между играми
/// </summary>
public interface ISwiperGame : IDisposable
{
	/// <summary>
	/// Событие, вызываемое при изменении состояния перехода
	/// </summary>
	event Action<GameSwiperService.TransitionState> OnTransitionStateChanged;

	/// <summary>
	/// Событие, вызываемое при смене игры
	/// </summary>
	event Action<Type, Type> OnGameChanged;

	/// <summary>
	/// Проверяет, можно ли перейти к следующей игре
	/// </summary>
	bool CanSwipeNext { get; }

	/// <summary>
	/// Проверяет, можно ли перейти к предыдущей игре
	/// </summary>
	bool CanSwipePrevious { get; }

	/// <summary>
	/// Проверяет, выполняется ли переход в данный момент
	/// </summary>
	bool IsTransitioning { get; }

	/// <summary>
	/// Получает текущие RenderTextures для отображения
	/// </summary>
	(RenderTexture current, RenderTexture next, RenderTexture previous) GetRenderTextures();

	/// <summary>
	/// Переключиться на следующую игру
	/// </summary>
	/// <param name="cancellationToken">Токен отмены операции</param>
	/// <returns>true если переключение успешно, false если нет</returns>
	Task<bool> SwipeToNextGameAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Переключиться на предыдущую игру
	/// </summary>
	/// <param name="cancellationToken">Токен отмены операции</param>
	/// <returns>true если переключение успешно, false если нет</returns>
	Task<bool> SwipeToPreviousGameAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Приостанавливает все игры
	/// </summary>
	void PauseAll();

	/// <summary>
	/// Возобновляет текущую игру
	/// </summary>
	void ResumeCurrent();
}
}