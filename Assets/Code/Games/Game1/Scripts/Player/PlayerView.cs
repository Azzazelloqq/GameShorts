using Logic.Entities;
using UnityEngine;

namespace Logic.Player
{
	internal class PlayerView : BaseView
	{
		public Transform ShootPoint => _shootPoint;
		[SerializeField]
		private Transform _shootPoint;
	}
}