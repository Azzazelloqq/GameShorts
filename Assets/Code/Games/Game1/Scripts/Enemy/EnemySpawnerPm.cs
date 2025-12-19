using System;
using System.Threading;
using Asteroids.Code.Games.Game1.Scripts.Enemy;
using Asteroids.Code.Games.Game1.Scripts.Entities;
using Asteroids.Code.Games.Game1.Scripts.Entities.Core;
using Asteroids.Code.Games.Game1.Scripts.View;
using Code.Core.ShortGamesCore.Game1.Scripts.Entities;
using Code.Core.Tools.Pool;
using Disposable;
using LightDI.Runtime;
using Logic.Enemy.Asteroid;
using Logic.Enemy.UFO;
using Logic.Entities.Core;
using Logic.Settings;
using R3;
using ResourceLoader;
using Random = UnityEngine.Random;

namespace Logic.Enemy
{
internal class EnemySpawnerPm : DisposableBase
{
	internal struct Ctx
	{
		public CancellationToken cancellationToken;
		public MainSceneContextView sceneContextView;
		public EnemyCoutControllerPm enemyCoutController;
		public IEntitiesController entitiesController;
	}

	private readonly Ctx _ctx;
	private AsteroidSettings _asteroidSettings;
	private RewardSettings _rewardSettings;
	private UFOSettings _ufoSettings;
	private readonly IPoolManager _poolManager;
	private readonly IResourceLoader _resourceLoader;
	private PlayerModel _playerModel;
	private float _lastAsteroidMultiplier = 1.0f;
	private float _lastUFOMultiplier = 1.0f;

	public EnemySpawnerPm(
		Ctx ctx,
		[Inject] IPoolManager poolManager,
		[Inject] IResourceLoader resourceLoader)
	{
		_ctx = ctx;
		_poolManager = poolManager;
		_resourceLoader = resourceLoader;
		_asteroidSettings = _ctx.sceneContextView.AsteroidSettings;
		_rewardSettings = _ctx.sceneContextView.RewardSettings;
		_ufoSettings = _ctx.sceneContextView.UFOSettings;
		_playerModel = _ctx.entitiesController.GetPlayerModel();
		_ctx.enemyCoutController.SpawnEnemy += SpawnEnemy;

		// Подписываемся на изменения множителей скорости для обновления существующих врагов
		if (_playerModel?.DifficultyScaler != null)
		{
			AddDisposable(
				_playerModel.DifficultyScaler.AsteroidSpeedMultiplier.Subscribe(OnAsteroidSpeedMultiplierChanged));
			AddDisposable(_playerModel.DifficultyScaler.UFOSpeedMultiplier.Subscribe(OnUFOSpeedMultiplierChanged));
		}
	}

	protected override void OnDispose()
	{
		_ctx.enemyCoutController.SpawnEnemy -= SpawnEnemy;
		base.OnDispose();
	}

	private void SpawnEnemy(EnemySpawnInfo spawninfo)
	{
		switch (spawninfo.entityType)
		{
			case EntityType.Asteroid:
				SpawnBigAsteroid(spawninfo);
				break;
			case EntityType.AsteroidPart:
				var count = Random.Range(2, 5);
				for (var i = 0; i < count; i++)
				{
					SpawnSmallAsteroid(spawninfo);
				}

				break;
			case EntityType.UFO:
				SpawnUFO(spawninfo);
				break;
			default: throw new ArgumentOutOfRangeException();
		}
	}

	private void SpawnUFO(EnemySpawnInfo spawninfo)
	{
		// Проверяем, что EntitiesController еще существует
		if (_ctx.entitiesController == null)
		{
			return;
		}

		// Получаем текущий множитель скорости UFO
		var ufoSpeedMultiplier = _playerModel?.DifficultyScaler?.UFOSpeedMultiplier?.Value ?? 1.0f;

		var model = new UFOModel
		{
			Id = _ctx.entitiesController.GenerateId(),
			Position = { Value = spawninfo.SpawnPosition },
			EntityType = EntityType.UFO,
			CurrentAngle = { Value = spawninfo.Angle },
			MaxSpeed = { Value = _ufoSettings.MaxSpeed * ufoSpeedMultiplier },
			AccelerationSpeed = { Value = _ufoSettings.Acceleration * ufoSpeedMultiplier },
			Reward = _rewardSettings.UFOReward
		};
		var ufoCtx = new UFOPm.Ctx
		{
			sceneContextView = _ctx.sceneContextView,
			ufoModel = model,
			playerModel = _ctx.entitiesController.GetPlayerModel(),
			entitiesController = _ctx.entitiesController,
			cancellationToken = _ctx.cancellationToken
		};

		var ufo = UFOPmFactory.CreateUFOPm(ufoCtx);

		// Проверяем, что EntitiesController еще существует
		if (_ctx.entitiesController != null)
		{
			_ctx.entitiesController.AddEntity(model.Id, new EntityInfo
			{
				Logic = ufo,
				Model = model
			});
		}
	}

	private void SpawnBigAsteroid(EnemySpawnInfo spawninfo)
	{
		// Проверяем, что EntitiesController еще существует
		if (_ctx.entitiesController == null)
		{
			return;
		}

		// Получаем текущий множитель скорости астероидов
		var asteroidSpeedMultiplier = _playerModel?.DifficultyScaler?.AsteroidSpeedMultiplier?.Value ?? 1.0f;

		var rotateSide = Random.Range(0, 2) == 0 ? -1 : 1;
		var model = new AsteroidModel
		{
			Id = _ctx.entitiesController.GenerateId(),
			Position = { Value = spawninfo.SpawnPosition },
			EntityType = EntityType.Asteroid,
			CurrentAngle = { Value = spawninfo.Angle },
			CanCollapse = { Value = true },
			MaxSpeed =
			{
				Value = Random.Range(_asteroidSettings.MinSpeed, _asteroidSettings.MaxSpeed) * asteroidSpeedMultiplier
			},
			MaxRotateSpeed =
				{ Value = rotateSide * Random.Range(_asteroidSettings.MinRotateSpeed, _asteroidSettings.MaxRotateSpeed) },
			Reward = _rewardSettings.AsteroidBigReward
		};
		var asteroidCtx = new AsteroidPm.Ctx
		{
			sceneContextView = _ctx.sceneContextView,
			asteroidModel = model,
			entitiesController = _ctx.entitiesController,
			requestSpawn = spawnPos =>
			{
				SpawnEnemy(new EnemySpawnInfo
				{
					SpawnPosition = spawnPos,
					entityType = EntityType.AsteroidPart
				});
			},
			cancellationToken = _ctx.cancellationToken
		};

		var asteroid = AsteroidPmFactory.CreateAsteroidPm(asteroidCtx);

		// Проверяем, что EntitiesController еще существует
		if (_ctx.entitiesController != null)
		{
			_ctx.entitiesController.AddEntity(model.Id, new EntityInfo
			{
				Logic = asteroid,
				Model = model
			});
		}
	}

	private void SpawnSmallAsteroid(EnemySpawnInfo spawninfo)
	{
		// Проверяем, что EntitiesController еще существует
		if (_ctx.entitiesController == null)
		{
			return;
		}

		// Получаем текущий множитель скорости астероидов
		var asteroidSpeedMultiplier = _playerModel?.DifficultyScaler?.AsteroidSpeedMultiplier?.Value ?? 1.0f;

		var rotateSide = Random.Range(0, 2) == 0 ? -1 : 1;
		var model = new AsteroidModel
		{
			Id = _ctx.entitiesController.GenerateId(),
			Position = { Value = spawninfo.SpawnPosition },
			EntityType = EntityType.AsteroidPart,
			CurrentAngle = { Value = Random.Range(0, 360) },
			CanCollapse = { Value = false },
			MaxSpeed =
			{
				Value = Random.Range(_asteroidSettings.MinSpeedSmall, _asteroidSettings.MaxSpeedSmall) *
						asteroidSpeedMultiplier
			},
			MaxRotateSpeed =
			{
				Value = rotateSide * Random.Range(_asteroidSettings.MinRotateSpeedSmall,
					_asteroidSettings.MaxRotateSpeedSmall)
			},
			Reward = _rewardSettings.AsteroidSmallReward
		};
		var asteroidCtx = new AsteroidPm.Ctx
		{
			sceneContextView = _ctx.sceneContextView,
			asteroidModel = model,
			entitiesController = _ctx.entitiesController,
			requestSpawn = spawnPos =>
			{
				SpawnEnemy(new EnemySpawnInfo
				{
					SpawnPosition = spawnPos,
					entityType = EntityType.AsteroidPart
				});
			},
			cancellationToken = _ctx.cancellationToken
		};

		var asteroid = AsteroidPmFactory.CreateAsteroidPm(asteroidCtx);

		// Проверяем, что EntitiesController еще существует
		if (_ctx.entitiesController != null)
		{
			_ctx.entitiesController.AddEntity(model.Id, new EntityInfo
			{
				Logic = asteroid,
				Model = model
			});
		}
	}

	private void OnAsteroidSpeedMultiplierChanged(float newMultiplier)
	{
		if (_ctx.entitiesController == null)
		{
			return;
		}

		// Вычисляем коэффициент изменения скорости
		var multiplierRatio = newMultiplier / _lastAsteroidMultiplier;
		_lastAsteroidMultiplier = newMultiplier;

		// Обновляем скорости всех существующих астероидов
		foreach (var entity in _ctx.entitiesController.AllEntities.Values)
		{
			if (entity.Model.EntityType == EntityType.Asteroid || entity.Model.EntityType == EntityType.AsteroidPart)
			{
				entity.Model.MaxSpeed.Value *= multiplierRatio;
			}
		}
	}

	private void OnUFOSpeedMultiplierChanged(float newMultiplier)
	{
		if (_ctx.entitiesController == null)
		{
			return;
		}

		// Вычисляем коэффициент изменения скорости
		var multiplierRatio = newMultiplier / _lastUFOMultiplier;
		_lastUFOMultiplier = newMultiplier;

		// Обновляем скорости всех существующих UFO
		foreach (var entity in _ctx.entitiesController.AllEntities.Values)
		{
			if (entity.Model.EntityType == EntityType.UFO)
			{
				entity.Model.MaxSpeed.Value *= multiplierRatio;
				entity.Model.AccelerationSpeed.Value *= multiplierRatio;
			}
		}
	}
}
}