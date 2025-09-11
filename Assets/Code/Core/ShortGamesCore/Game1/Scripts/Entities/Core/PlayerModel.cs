using Logic.Entities.Core;
using Logic.Player.LaserWeapon;
using R3;

namespace Code.Core.ShortGamesCore.Game1.Scripts.Entities.Core
{
	public class PlayerModel : BaseModel
	{
		public ReactiveProperty<float> LaserLength;
		public ReactiveProperty<float> LaserThickness;
		public ReactiveProperty<float> LaserCooldown;
		public ReactiveProperty<float> CountLaserShots;
		public ReactiveProperty<float> LaserShotDuration;
		
		public ReactiveProperty<float> BulletMaxSpeed;
		public ReactiveProperty<float> BulletRate;

		public ReactiveProperty<int> Score;
		
		private LaserBattery[] _charges;
		public LaserBattery[] Charges => _charges;

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
		}

		public void InitLaserBattary(int countCharges, float rechargeCooldown)
		{
			_charges = new LaserBattery[countCharges];
			for (int i = 0; i < countCharges; i++)
			{
				_charges[i] = new LaserBattery(rechargeCooldown);
			}
		}
	}
}