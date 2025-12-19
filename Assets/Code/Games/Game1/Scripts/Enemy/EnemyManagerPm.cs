using System.Threading;
using Asteroids.Code.Games.Game1.Scripts.Entities;
using Asteroids.Code.Games.Game1.Scripts.View;
using Disposable;

namespace Logic.Enemy
{
    internal class EnemyManagerPm : DisposableBase
    {
        internal struct Ctx
        {
            public MainSceneContextView sceneContextView;
            public IEntitiesController entitiesController;
            public CancellationToken cancellationToken;
        }

        private readonly Ctx _ctx;
        
        public EnemyManagerPm(Ctx ctx)
        {
            _ctx = ctx;

            EnemyCoutControllerPm.Ctx enemyCoutControllerCtx = new EnemyCoutControllerPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                entitiesController = _ctx.entitiesController
            };
            var enemyController = EnemyCoutControllerPmFactory.CreateEnemyCoutControllerPm(enemyCoutControllerCtx);
            AddDisposable(enemyController);

            EnemySpawnerPm.Ctx enemySpawnerCtx = new EnemySpawnerPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                enemyCoutController = enemyController,
                entitiesController = _ctx.entitiesController,
                cancellationToken = _ctx.cancellationToken
            };
            AddDisposable(EnemySpawnerPmFactory.CreateEnemySpawnerPm(enemySpawnerCtx));
        }
    }
}