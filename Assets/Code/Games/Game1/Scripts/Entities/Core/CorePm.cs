using System;
using System.Threading;
using Asteroids.Code.Games.Game1.Scripts.View;
using Code.Core.InputManager;
using Code.Core.Tools.Pool;
using Disposable;
using LightDI.Runtime;

namespace Code.Core.ShortGamesCore.Game1.Scripts.Core
{
internal class CorePm : DisposableBase
{
	internal struct Ctx
	{
		public CancellationToken cancellationToken;
		public MainSceneContextView sceneContextView;
		public Action restartGame;
	}

	private readonly Ctx _ctx;
	private IDisposable _scene;
	private readonly IDiContainer _diContainer;
	private readonly InputManager.InputManager _inputManager;
	private readonly IPoolManager _poolManager;

	public CorePm(Ctx ctx, [Inject] IPoolManager poolManager)
	{
		_ctx = ctx;
		_poolManager = poolManager;
		_diContainer = DiContainerFactory.CreateContainer();
		AddDisposable(_diContainer);
		_inputManager = new InputManager.InputManager();
		_diContainer.RegisterAsSingleton<IInputManager>(_inputManager);
		_inputManager?.SetJoystickOptions(AxisOptions.None);
		var sceneCtx = new ScenePm.Ctx
		{
			sceneContextView = _ctx.sceneContextView,
			cancellationToken = _ctx.cancellationToken,
			restartGame = _ctx.restartGame
		};
		_scene = new ScenePm(sceneCtx);
	}

	protected override void OnDispose()
	{
		_inputManager?.SetJoystickOptions(AxisOptions.None);
		_scene?.Dispose();
		_poolManager?.Clear();
		base.OnDispose();
	}
}
}