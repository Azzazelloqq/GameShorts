using Logic.Entities;
using UnityEngine;

namespace Logic.Player.LaserWeapon
{
    internal class LaserView : BaseView
    {
        [SerializeField]
        private LineRenderer _laser;

        public LineRenderer Laser => _laser;
    }
}