using System;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.InputManager;
using LightDI.Runtime;

namespace Root.Inputs
{
	public class PlayerController : BaseDisposable
	{
		public struct Ctx
		{
		}

		private readonly Ctx _ctx;
		private readonly IInputManager _inputManager;
		public event Action Fire1;
		public event Action Fire2;

		public PlayerController(Ctx ctx, [Inject] IInputManager inputManager)
		{
			_ctx = ctx;
			_inputManager = inputManager;
		}
		private void Fireleft()
		{
			Fire1?.Invoke();
		}
		private void FireRight()
		{
			Fire2?.Invoke();
		}

		protected override void OnDispose()
		{
			base.OnDispose();
		}

	}
}