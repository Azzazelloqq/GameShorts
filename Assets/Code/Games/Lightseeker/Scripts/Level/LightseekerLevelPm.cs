using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;

namespace Lightseeker
{
    internal class LightseekerLevelPm : BaseDisposable
    {
        public struct Ctx
        {
            public CancellationToken cancellationToken;
            public LightseekerSceneContextView sceneContextView;
        }

        private readonly Ctx _ctx;

        public LightseekerLevelPm(Ctx ctx)
        {
            _ctx = ctx;
            
            InitializeLevel();
        }

        private void InitializeLevel()
        {
            // Здесь будет инициализация уровня:
            // - Создание препятствий
            // - Расстановка объектов
            // - Настройка границ
        }
    }
}

