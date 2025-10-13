using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Code.Games.AngryHumans
{
/// <summary>
/// Конфигурация уровня для игры AngryHumans
/// </summary>
[CreateAssetMenu(fileName = "LevelConfig", menuName = "AngryHumans/Level Config", order = 1)]
internal class LevelConfig : ScriptableObject
{
	[Header("Level Info")]
	[SerializeField]
	[Tooltip("Название уровня")]
	private string _levelName = "New Level";

	[SerializeField]
	[Tooltip("Описание уровня")]
	[TextArea(2, 4)]
	private string _levelDescription;

	[SerializeField]
	[Tooltip("Номер уровня")]
	private int _levelNumber = 1;

	[Header("Gameplay Settings")]
	[SerializeField]
	[Tooltip("Количество попыток на уровень")]
	private int _maxAttempts = 5;

	[SerializeField]
	[Tooltip("Минимальное количество очков для прохождения (3 звезды)")]
	private int _threeStarsScore = 5000;

	[SerializeField]
	[Tooltip("Минимальное количество очков для 2 звезд")]
	private int _twoStarsScore = 3000;

	[SerializeField]
	[Tooltip("Минимальное количество очков для 1 звезды")]
	private int _oneStarScore = 1000;

	[Header("Level Prefab")]
	[SerializeField]
	[Tooltip("Префаб уровня со всеми структурами и окружением")]
	private AssetReference _levelPrefabReference;

	[Header("Visual Settings")]
	[SerializeField]
	[Tooltip("Цвет фона/неба")]
	private Color _backgroundColor = new(0.5f, 0.7f, 1f, 1f);

	[Header("Rewards")]
	[SerializeField]
	[Tooltip("Базовая награда за прохождение уровня")]
	private int _baseReward = 100;

	[SerializeField]
	[Tooltip("Множитель награды за идеальное прохождение (все цели уничтожены)")]
	private float _perfectMultiplier = 2f;

	// Публичные свойства для доступа к настройкам
	public string LevelName => _levelName;
	public string LevelDescription => _levelDescription;
	public int LevelNumber => _levelNumber;
	public int MaxAttempts => _maxAttempts;
	public int ThreeStarsScore => _threeStarsScore;
	public int TwoStarsScore => _twoStarsScore;
	public int OneStarScore => _oneStarScore;
	public AssetReference LevelPrefabReference => _levelPrefabReference;
	public Color BackgroundColor => _backgroundColor;
	public int BaseReward => _baseReward;
	public float PerfectMultiplier => _perfectMultiplier;

	/// <summary>
	/// Вычисляет количество звезд за набранные очки
	/// </summary>
	public int CalculateStars(int score)
	{
		if (score >= _threeStarsScore)
		{
			return 3;
		}

		if (score >= _twoStarsScore)
		{
			return 2;
		}

		if (score >= _oneStarScore)
		{
			return 1;
		}

		return 0;
	}

	/// <summary>
	/// Проверяет, пройден ли уровень (хотя бы 1 звезда)
	/// </summary>
	public bool IsLevelCompleted(int score)
	{
		return score >= _oneStarScore;
	}

	/// <summary>
	/// Вычисляет финальную награду с учетом результатов
	/// </summary>
	public int CalculateReward(int score, bool perfectCompletion)
	{
		var reward = _baseReward;

		// Добавляем бонус за звезды
		var stars = CalculateStars(score);
		reward += stars * 50;

		// Добавляем множитель за идеальное прохождение
		if (perfectCompletion)
		{
			reward = Mathf.RoundToInt(reward * _perfectMultiplier);
		}

		return reward;
	}

	#if UNITY_EDITOR
	/// <summary>
	/// Валидация данных в редакторе
	/// </summary>
	private void OnValidate()
	{
		if (_twoStarsScore <= _oneStarScore)
		{
			_twoStarsScore = _oneStarScore + 1000;
		}

		if (_threeStarsScore <= _twoStarsScore)
		{
			_threeStarsScore = _twoStarsScore + 1000;
		}

		if (_maxAttempts < 1)
		{
			_maxAttempts = 1;
		}

		if (_baseReward < 0)
		{
			_baseReward = 0;
		}

		if (_perfectMultiplier < 1f)
		{
			_perfectMultiplier = 1f;
		}
	}
	#endif
}
}