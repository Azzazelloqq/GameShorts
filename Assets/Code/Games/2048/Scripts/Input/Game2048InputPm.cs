using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Games._2048.Scripts.View;
using UnityEngine;
using R3;

namespace Code.Games._2048.Scripts.Input
{
    internal class Game2048InputPm : BaseDisposable
    {
        internal struct Ctx
        {
            public Game2048InputAreaView inputAreaView;
            public CancellationToken cancellationToken;
        }

        private readonly Ctx _ctx;
        private readonly CompositeDisposable _compositeDisposable = new();
        
        public readonly Subject<Vector2> OnPointerDown = new();
        public readonly Subject<Vector2> OnPointerMove = new();
        public readonly Subject<Vector2> OnPointerUp = new();

        public Game2048InputPm(Ctx ctx)
        {
            _ctx = ctx;
            
            SetupInputBindings();
            AddDispose(_compositeDisposable);
            AddDispose(OnPointerDown);
            AddDispose(OnPointerMove);
            AddDispose(OnPointerUp);
        }

        private void SetupInputBindings()
        {
            var inputCtx = new Game2048InputAreaView.Ctx
            {
                onPointerDown = OnPointerDown.OnNext,
                onPointerMove = OnPointerMove.OnNext,
                onPointerUp = OnPointerUp.OnNext
            };
            
            _ctx.inputAreaView.SetCtx(inputCtx);
        }
    }
}
