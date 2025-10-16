using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.Tools.Pool;
using LightDI.Runtime;
using UnityEngine;

namespace Lightseeker
{
    internal class LightseekerLevelPm : BaseDisposable
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
            
            Debug.Log($"LightseekerLevelPm: UpdateActiveSections - CurrentLevel: {currentLevel}, Total sections: {sections.Count}");
            
            // Отключаем пройденные секции (уровни, которые уже прошли)
            // currentLevel 1 = все секции активны
            // currentLevel 2 = секция 0 отключена, остальные активны
            // currentLevel 3 = секции 0,1 отключены, остальные активны
            // и т.д.
            for (int i = 0; i < sections.Count; i++)
            {
                bool shouldBeActive = i >= currentLevel - 1;
                
                // Отключаем секции с индексом меньше (currentLevel - 1)
                // т.е. пройденные секции
                if (shouldBeActive)
                {
                    sections[i].SetActive(true);
                    _activeSections.Add(sections[i]);
                    Debug.Log($"LightseekerLevelPm: Section {i} ACTIVATED, SpawnPoints count: {sections[i].SpawnPoints.Count}");
                }
                else
                {
                    sections[i].SetActive(false);
                    Debug.Log($"LightseekerLevelPm: Section {i} DEACTIVATED (passed level)");
                }
            }
            
            Debug.Log($"LightseekerLevelPm: Level {currentLevel}, Active sections: {_activeSections.Count}/{sections.Count}");
        }

        private void SpawnStars()
        {
            // Очищаем старые звезды
            ClearStars();
            
            Debug.Log($"LightseekerLevelPm: SpawnStars - Active sections count: {_activeSections.Count}");
            
            // Собираем все доступные точки спавна из активных секций
            var allSpawnPoints = new List<Transform>();
            foreach (var section in _activeSections)
            {
                Debug.Log($"LightseekerLevelPm: Collecting spawn points from section, count: {section.SpawnPoints.Count}");
                allSpawnPoints.AddRange(section.SpawnPoints);
            }
            
            Debug.Log($"LightseekerLevelPm: Total spawn points collected: {allSpawnPoints.Count}");
            
            if (allSpawnPoints.Count == 0)
            {
                Debug.LogWarning("LightseekerLevelPm: No spawn points available!");
                return;
            }
            
            // Перемешиваем точки спавна
            var shuffledPoints = allSpawnPoints.OrderBy(x => UnityEngine.Random.value).ToList();
            
            // Спавним 4 звезды
            int starsToSpawn = Mathf.Min(LightseekerGameModel.StarsPerLevel, shuffledPoints.Count);
            Debug.Log($"LightseekerLevelPm: Spawning {starsToSpawn} stars");
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
