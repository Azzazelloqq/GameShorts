using System.Collections.Generic;
using Asteroids.Code.Games.Game1.Scripts.Core;
using Logic.Entities.Core;
using Logic.Player.LaserWeapon;
using R3;

namespace Asteroids.Code.Games.Game1.Scripts.Entities.Core
{
	internal class PlayerModel : BaseModel
	{
		public ReactiveProperty<float> LaserLength;
		public ReactiveProperty<float> LaserThickness;
		public ReactiveProperty<float> LaserCooldown;
		public ReactiveProperty<float> CountLaserShots;
		public ReactiveProperty<float> LaserShotDuration;
		
		public ReactiveProperty<float> BulletMaxSpeed;
		public ReactiveProperty<float> BulletRate;

	public ReactiveProperty<int> Score;
	
	private List<LaserBattery> _charges;
	public List<LaserBattery> Charges => _charges;

	public DifficultyScaler DifficultyScaler { get; private set; }

		public PlayerModel()
		{
			BulletMaxSpeed = new ReactiveProperty<float>();
			BulletRate = new ReactiveProperty<float>();
			LaserLength = new ReactiveProperty<float>();
			LaserThickness = new ReactiveProperty<float>();
			LaserCooldown = new ReactiveProperty<float>();
			CountLaserShots = new ReactiveProperty<float>();
			LaserShotDuration = new ReactiveProperty<float>();
			Score = new ReactiveProperty<int>();
			_charges = new List<LaserBattery>();
			
			// Инициализируем систему масштабирования сложности
			DifficultyScaler = new DifficultyScaler(Score);
		}

		public void InitLaserBattary(int countCharges, float rechargeCooldown)
		{
			for (var i = 0; i < countCharges; i++)
			{
				_charges.Add(new LaserBattery(rechargeCooldown));
			}
		}

		public new void Destroy(int? killerId = null)
		{
			DifficultyScaler?.Dispose();
			base.Destroy(killerId);
		}
	}
}