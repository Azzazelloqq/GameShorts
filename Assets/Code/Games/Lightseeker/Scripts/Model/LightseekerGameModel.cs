using R3;

namespace Lightseeker
{
    internal class LightseekerGameModel
    {
        public ReactiveProperty<int> CurrentLevel { get; } = new ReactiveProperty<int>(1);
        public ReactiveProperty<int> CollectedStars { get; } = new ReactiveProperty<int>(0);
        
        public const int MaxLevel = 4;
        public const int StarsPerLevel = 4;
    }
}

