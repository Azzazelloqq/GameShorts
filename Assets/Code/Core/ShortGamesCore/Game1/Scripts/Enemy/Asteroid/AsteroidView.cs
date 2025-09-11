using Code.Core.ShortGamesCore.Game1.Scripts.Entities.Core;
using Logic.Entities;
using Logic.Entities.Core;
using UnityEngine;

namespace Logic.Enemy.Asteroid
{
	internal class AsteroidView : BaseView
	{
		[SerializeField]
		private Transform _holder;

		[SerializeField]
		private CircleCollider2D _collider;
		
		[SerializeField]
		private Transform _bigAsteroid;
		[SerializeField]
		private Transform _smallAsteroid;
		
		public Transform Holder => _holder;
		
		

		protected override void AfterSetCtx()
		{
			var asteroidModel = _ctx.model as AsteroidModel;
			if (asteroidModel == null)
				return;
			bool canCollapse = asteroidModel.CanCollapse.Value;
			_bigAsteroid.gameObject.SetActive(canCollapse);
			_smallAsteroid.gameObject.SetActive(!canCollapse);
			_collider.radius = canCollapse ? 2 : 1.25f;
		}
	}
}