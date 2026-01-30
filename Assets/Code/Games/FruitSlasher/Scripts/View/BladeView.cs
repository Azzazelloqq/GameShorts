using System;
using Disposable;
using UnityEngine;

namespace Code.Games.FruitSlasher.Scripts.View
{
    public class BladeView: MonoBehaviourDisposable
    {
        [SerializeField] private Collider _collider;
        [SerializeField] private TrailRenderer _trailRenderer;

        public Collider Collider => _collider;

        public TrailRenderer TrailRenderer => _trailRenderer;

    }
}