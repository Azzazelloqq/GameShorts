
using System;
using System.Threading;
using R3;
using UnityEngine;

namespace Code.Games.FruitSlasher.Scripts.Input
{
    public class InputAreaPm : IDisposable
    {
        public readonly Subject<Vector2> OnPointerDown = new();
        public readonly Subject<Vector2> OnPointerMove = new();
        public readonly Subject<Vector2> OnPointerUp = new();
        
        private bool _isInputEnabled = true;
        private readonly InputAreaView _view;

        public InputAreaPm(InputAreaView inputAreaView)
        {
            _view =  inputAreaView;
            
            SetupInputBindings();
        }

        private void SetupInputBindings()
        {
            var inputCtx = new InputAreaView.Ctx
            {
                onPointerDown = pos => { if (_isInputEnabled) OnPointerDown.OnNext(pos); },
                onPointerMove = pos => { if (_isInputEnabled) OnPointerMove.OnNext(pos); },
                onPointerUp = pos => { if (_isInputEnabled) OnPointerUp.OnNext(pos); }
            };
            
            _view.SetCtx(inputCtx);
        }
        
        public void SetInputEnabled(bool enabled)
        {
            _isInputEnabled = enabled;
            Debug.Log($"InputPm: Input {(enabled ? "enabled" : "disabled")}");
        }

        public void Dispose()
        {
            OnPointerDown.Dispose();
            OnPointerMove.Dispose();
            OnPointerUp.Dispose();
        }
    }
}
