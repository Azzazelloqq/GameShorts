
using System;
using System.Runtime.InteropServices;
using Code.Core.Tools;
using Code.Games.FruitSlasher.Scripts.View;
using Disposable;
using LightDI.Runtime;
using TickHandler;
using UnityEngine;

namespace Code.Games.FruitSlasher.Scripts.Logic
{
    internal class FruitPm : DisposableBase
    {
        internal struct Ctx
        {
            public float _lifeTime;
            public FruitView fruitView;
            public ReactiveTrigger<Guid> remove;
            public Guid id;
            public float startForce;
            public BladePm blade;
            public ReactiveTrigger<Guid> sliced;
        }

        public Guid Id => _ctx.id;
        public FruitView FruitView => _fruitView;
        
        private float _lifeTime;
        private readonly Ctx _ctx;
        private FruitView _fruitView;
        private readonly ITickHandler _tickHandler;
        private bool _isSliced = false;

        public bool IsSliced => _isSliced;

        private const float SLICED_FORCE = 5f;

        public FruitPm(Ctx ctx, [Inject] ITickHandler  tickHandler)
        {
            _ctx = ctx;
            _fruitView = _ctx.fruitView;
            _fruitView.Reset();
            _lifeTime = _ctx._lifeTime;
            _tickHandler = tickHandler;
            _fruitView.Rigidbody.AddForce(_ctx.startForce * _fruitView.transform.up, ForceMode.Impulse);
            _tickHandler.FrameUpdate += UpdateLifeTime;
            _fruitView.Slicing += Slicing;
        }

        private void Slicing()
        {
            _ctx.sliced.Notify(_ctx.id);
            _isSliced = true;
            _fruitView.Whole.gameObject.SetActive(false);
            _fruitView.FruitCollider.enabled = false;
            _fruitView.Sliced.gameObject.SetActive(true);
            _fruitView.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            
            var angle = Mathf.Atan2(_ctx.blade.Direction.y,  _ctx.blade.Direction.x) * Mathf.Rad2Deg;
            _fruitView.Sliced.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            foreach (var rigidbody in _fruitView.SlicedRigidbody)
            {
                rigidbody.linearVelocity = _fruitView.Rigidbody.linearVelocity;
                rigidbody.AddForceAtPosition(_ctx.blade.Direction * SLICED_FORCE, _ctx.blade.Position, ForceMode.Impulse);
            }
            _fruitView.Particles.gameObject.SetActive(true);
            _fruitView.Particles.Play(true);
        }

        protected override void OnDispose()
        {
            _tickHandler.FrameUpdate -= UpdateLifeTime;
            _fruitView.Slicing -= Slicing;
        }

        private void UpdateLifeTime(float deltaTime)
        {
            _lifeTime -= deltaTime;
            if (_lifeTime <= 0f)
            {
                _tickHandler.FrameUpdate -= UpdateLifeTime;
                _fruitView.Rigidbody.linearVelocity = Vector3.zero;
                _ctx.remove.Notify(Id);
            }
        }
    }
}