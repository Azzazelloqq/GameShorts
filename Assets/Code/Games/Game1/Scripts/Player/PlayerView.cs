using Logic.Entities;
using UnityEngine;

namespace Logic.Player
{
	public class PlayerView : BaseView
	{
		public Transform ShootPoint => _shootPoint;
		[SerializeField]
		private Transform _shootPoint;
	}
}