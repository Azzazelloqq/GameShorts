using Disposable;
using R3;

namespace Asteroids.Code.Games.Game1.Scripts.Core
{
    internal class DifficultyScaler : DisposableBase
    {
        private const int SCORE_THRESHOLD = 500; 
        private const float PLAYER_SPEED_MULTIPLIER = 1.3f; // Увеличение скорости игрока
        private const float ASTEROID_SPEED_MULTIPLIER = 1.1f; // Увеличение скорости астероидов
        private const float UFO_SPEED_MULTIPLIER = 1.2f; // Увеличение скорости UFO

        public ReactiveProperty<float> PlayerSpeedMultiplier { get; private set; }
        public ReactiveProperty<float> AsteroidSpeedMultiplier { get; private set; }
        public ReactiveProperty<float> UFOSpeedMultiplier { get; private set; }
        public ReactiveProperty<int> CurrentDifficultyLevel { get; private set; }

        private readonly ReactiveProperty<int> _score;
        private int _lastScoreThreshold;

        public DifficultyScaler(ReactiveProperty<int> score)
        {
            _score = score;
            _lastScoreThreshold = 0;

            PlayerSpeedMultiplier = new ReactiveProperty<float>(1.0f);
            AsteroidSpeedMultiplier = new ReactiveProperty<float>(1.0f);
            UFOSpeedMultiplier = new ReactiveProperty<float>(1.0f);
            CurrentDifficultyLevel = new ReactiveProperty<int>(0);

            // Подписываемся на изменения счета
            AddDisposable(_score.Subscribe(OnScoreChanged));
        }

        private void OnScoreChanged(int newScore)
        {
            int currentThreshold = (newScore / SCORE_THRESHOLD) * SCORE_THRESHOLD;
            
            if (currentThreshold > _lastScoreThreshold)
            {
                _lastScoreThreshold = currentThreshold;
                int difficultyLevel = currentThreshold / SCORE_THRESHOLD;
                
                UpdateDifficultyMultipliers(difficultyLevel);
                CurrentDifficultyLevel.Value = difficultyLevel;
            }
        }

        private void UpdateDifficultyMultipliers(int level)
        {
            // Применяем мультипликаторы для каждого уровня сложности
            PlayerSpeedMultiplier.Value = CalculateMultiplier(PLAYER_SPEED_MULTIPLIER, level);
            AsteroidSpeedMultiplier.Value = CalculateMultiplier(ASTEROID_SPEED_MULTIPLIER, level);
            UFOSpeedMultiplier.Value = CalculateMultiplier(UFO_SPEED_MULTIPLIER, level);
        }

        private float CalculateMultiplier(float baseMultiplier, int level)
        {
            if (level <= 0) return 1.0f;
            
            // Применяем мультипликатор level раз: 1.0 * multiplier^level
            float result = 1.0f;
            for (int i = 0; i < level; i++)
            {
                result *= baseMultiplier;
            }
            return result;
        }

        protected override void OnDispose()
        {
            PlayerSpeedMultiplier?.Dispose();
            AsteroidSpeedMultiplier?.Dispose();
            UFOSpeedMultiplier?.Dispose();
            CurrentDifficultyLevel?.Dispose();
            base.OnDispose();
        }
    }
}
