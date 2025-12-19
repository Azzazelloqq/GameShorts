using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Disposable;
using Code.Core.Tools.Pool;
using LightDI.Runtime;
using UnityEngine;

namespace Lightseeker
{
    internal class LightseekerLevelPm : DisposableBase
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public LightseekerSceneContextView sceneContextView;
            public LightseekerGameModel gameModel;
            public Action<int> onStarCollected;
        }

        private readonly Ctx _ctx;
        private readonly List<StarView> _activeStars = new List<StarView>();
        private readonly List<LevelSection> _activeSections = new List<LevelSection>();
        private readonly IPoolManager _poolManager;

        public LightseekerLevelPm(Ctx ctx, [Inject] IPoolManager poolManager)
        {
            _ctx = ctx;
            _poolManager = poolManager;
            
            InitializeLevel();
        }

        private void InitializeLevel()
        {
            // Активируем секции для текущего уровня
            UpdateActiveSections();
            
            // Спавним первые 4 звезды
            SpawnStars();
        }

        private void UpdateActiveSections()
        {
            _activeSections.Clear();
            
            int currentLevel = _ctx.gameModel.CurrentLevel.Value;
            var sections = _ctx.sceneContextView.LevelSections;
            for (int i = 0; i < sections.Count; i++)
            {
                sections[i].SetActive(currentLevel <= i + 1);
                if (i <= currentLevel - 1)
                {
                    _activeSections.Add(sections[i]);
                }
            }
            
        }

        private void SpawnStars()
        {
            ClearStars();
            
            // Собираем все доступные точки спавна из активных секций
            var allSpawnPoints = new List<Transform>();
            foreach (var section in _activeSections)
            {
                Debug.Log($"LightseekerLevelPm: Collecting spawn points from section, count: {section.SpawnPoints.Count}");
                allSpawnPoints.AddRange(section.SpawnPoints);
            }
            
            if (allSpawnPoints.Count == 0)
            {
                Debug.LogWarning("LightseekerLevelPm: No spawn points available!");
                return;
            }
            
            // Перемешиваем точки спавна
            var shuffledPoints = allSpawnPoints.OrderBy(x => UnityEngine.Random.value).ToList();
            
            // Спавним 4 звезды
            int starsToSpawn = Mathf.Min(LightseekerGameModel.StarsPerLevel, shuffledPoints.Count);
            for (int i = 0; i < starsToSpawn; i++)
            {
                Debug.Log($"LightseekerLevelPm: Spawning star {i+1} at position: {shuffledPoints[i].position}");
                SpawnStar(shuffledPoints[i].position);
            }
        }

        private void SpawnStar(Vector3 position)
        {
            if (_ctx.sceneContextView.StarPrefab == null)
            {
                Debug.LogError("LightseekerLevelPm: Star prefab is not assigned!");
                return;
            }
            
            var starObj = _poolManager.Get(_ctx.sceneContextView.StarPrefab.gameObject, 
                position, _ctx.sceneContextView.StarPlaceholder, Quaternion.identity);
           
            var star = starObj.GetComponent<StarView>();
            star.OnCollected += OnStarCollected;
            _activeStars.Add(star);
        }

        private void OnStarCollected(StarView star)
        {
            _activeStars.Remove(star);
            
            int collectedStars = _ctx.gameModel.CollectedStars.Value + 1;
            _ctx.gameModel.CollectedStars.Value = collectedStars;
            
            _ctx.onStarCollected?.Invoke(collectedStars);
            
            // Если собрали все 4 звезды, открываем новый уровень
            if (collectedStars >= LightseekerGameModel.StarsPerLevel)
            {
                OpenNextLevel();
            }
        }

        private void OpenNextLevel()
        {
            int nextLevel = _ctx.gameModel.CurrentLevel.Value + 1;
            
            if (nextLevel > LightseekerGameModel.MaxLevel)
            {
                Debug.Log("LightseekerLevelPm: All levels completed!");
                return;
            }
            
            _ctx.gameModel.CurrentLevel.Value = nextLevel;
            _ctx.gameModel.CollectedStars.Value = 0;
            
            // Обновляем активные секции
            UpdateActiveSections();
            // Спавним новые звезды
            SpawnStars();
        }

        private void ClearStars()
        {
            foreach (var star in _activeStars)
            {
                if (star != null)
                {
                    star.OnCollected -= OnStarCollected;
                    _poolManager.Return(_ctx.sceneContextView.StarPrefab.gameObject, star.gameObject);
                    UnityEngine.Object.Destroy(star.gameObject);
                }
            }
            _activeStars.Clear();
        }

        protected override void OnDispose()
        {
            ClearStars();
            base.OnDispose();
        }
    }
}
