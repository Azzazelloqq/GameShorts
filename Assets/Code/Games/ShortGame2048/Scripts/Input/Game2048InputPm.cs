using System;
using System.Threading;
using Disposable;
using UnityEngine;
using R3;
using CompositeDisposable = Disposable.CompositeDisposable;

namespace Code.Games
{
    internal class Game2048InputPm : DisposableBase
    {
        internal struct Ctx
        {
            public Game2048InputAreaView inputAreaView;
            public CancellationToken cancellationToken;
        }

        private readonly Ctx _ctx;
        
        public readonly Subject<Vector2> OnPointerDown = new();
        public readonly Subject<Vector2> OnPointerMove = new();
        public readonly Subject<Vector2> OnPointerUp = new();
        
        private bool _isInputEnabled = true;

        public Game2048InputPm(Ctx ctx)
        {
            _ctx = ctx;
            
            SetupInputBindings();
            AddDisposable(OnPointerDown);
            AddDisposable(OnPointerMove);
            AddDisposable(OnPointerUp);
        }

        private void SetupInputBindings()
        {
            var inputCtx = new Game2048InputAreaView.Ctx
            {
                onPointerDown = pos => { if (_isInputEnabled) OnPointerDown.OnNext(pos); },
                onPointerMove = pos => { if (_isInputEnabled) OnPointerMove.OnNext(pos); },
                onPointerUp = pos => { if (_isInputEnabled) OnPointerUp.OnNext(pos); }
            };
            
            _ctx.inputAreaView.SetCtx(inputCtx);
        }
        
        public void SetInputEnabled(bool enabled)
        {
            _isInputEnabled = enabled;
            Debug.Log($"Game2048InputPm: Input {(enabled ? "enabled" : "disabled")}");
        }
    }
}
