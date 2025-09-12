using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Game1.Scripts.View;
using Code.Core.Tools.Pool;
using Logic.Entities;
using Logic.Scene;
using ResourceLoader;

namespace Logic.Enemy
{
    public class EnemyManagerPm : BaseDisposable
    {
        public struct Ctx
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
            var enemyController = new EnemyCoutControllerPm(enemyCoutControllerCtx);
            AddDispose(enemyController);

            EnemySpawnerPm.Ctx enemySpawnerCtx = new EnemySpawnerPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                enemyCoutController = enemyController,
                entitiesController = _ctx.entitiesController,
                cancellationToken = _ctx.cancellationToken
            };
            AddDispose(EnemySpawnerPmFactory.CreateEnemySpawnerPm(enemySpawnerCtx));
        }
    }
}