using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Games._2048.Scripts.View;
using UnityEngine;
using R3;

namespace Code.Games._2048.Scripts.Gameplay
{
    internal class Game2048CubeMergeManagerPm : BaseDisposable
    {
        internal struct Ctx
        {
            public Game2048CubeSpawnerPm cubeSpawner;
            public GameObject cubePrefab;
            public Transform spawnPoint;
            public float mergeUpwardForce;
            public CancellationToken cancellationToken;
        }

        private readonly Ctx _ctx;
        private readonly CompositeDisposable _compositeDisposable = new();

        public Game2048CubeMergeManagerPm(Ctx ctx)
        {
            _ctx = ctx;
            
            AddDispose(_compositeDisposable);
        }

        public void OnCubeCollision(Guid cubeId1, Guid cubeId2)
        {
            // Получаем список активных кубов из спавнера
            var activeCubes = _ctx.cubeSpawner.Cubes;
            
            // Находим кубы по их ID
            activeCubes.TryGetValue(cubeId1, out var cube1);
            activeCubes.TryGetValue(cubeId2, out var cube2);
            
            // Проверяем, что оба куба найдены и активны
            if (cube1 == null || cube2 == null)
                return;

            // Выполняем мерж
            PerformMerge(cube1, cube2);
        }

        private void PerformMerge(CubePm cube1, CubePm cube2)
        {
            // Вычисляем точку пересечения (среднее между позициями кубов)
            Vector3 mergePosition = (cube1.View.transform.position + cube2.View.transform.position) / 2f;
            
            // Новое число - сумма чисел мержащихся кубов
            int newNumber = cube1.Number + cube2.Number;

            _ctx.cubeSpawner.DisposeCube(cube1.Id);
            _ctx.cubeSpawner.DisposeCube(cube2.Id);

            // Создаем новый куб в точке мержа
            var newCube = CreateMergedCube(mergePosition, newNumber);

            // Применяем физический толчок
            ApplyMergeImpulse(newCube, mergePosition, newNumber);
        }

        private Game2048CubeView CreateMergedCube(Vector3 position, int number)
        {
            // Создаем новый куб через спавнер
            var newCube = _ctx.cubeSpawner.CreateCubeAtPosition(position, number);
            return newCube;
        }

        private void ApplyMergeImpulse(Game2048CubeView newCube, Vector3 mergePosition, int newNumber)
        {
            var rigidbody = newCube.GetComponent<Rigidbody>();
            if (rigidbody == null) return;

            // Сначала сбрасываем скорость куба
            newCube.ResetVelocity();

            // Базовая сила толчка вверх из настроек
            Vector3 upwardForce = Vector3.up * _ctx.mergeUpwardForce;

            // Ищем ближайший куб с таким же числом для направления наклона
            Vector3 directionToSimilarCube = FindDirectionToNearestSimilarCube(mergePosition, newNumber);
            
            // Добавляем небольшой наклон в сторону ближайшего похожего куба
            Vector3 lateralForce = directionToSimilarCube * 2f;

            // Применяем комбинированную силу
            Vector3 totalForce = upwardForce + lateralForce;
            rigidbody.AddForce(totalForce, ForceMode.Impulse);

            // Добавляем небольшое вращение для эффектности
            Vector3 randomTorque = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-1f, 1f)
            ) * 2f;
            rigidbody.AddTorque(randomTorque, ForceMode.Impulse);
        }

        private Vector3 FindDirectionToNearestSimilarCube(Vector3 fromPosition, int targetNumber)
        {
            // Получаем список активных кубов из спавнера
            var activeCubes = _ctx.cubeSpawner.GetAllActiveCubeViews();
            
            Game2048CubeView nearestSimilarCube = null;
            float nearestDistance = float.MaxValue;

            foreach (var cube in activeCubes)
            {
                if (cube.GetNumber() == targetNumber)
                {
                    float distance = Vector3.Distance(fromPosition, cube.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestSimilarCube = cube;
                    }
                }
            }

            if (nearestSimilarCube != null)
            {
                Vector3 direction = (nearestSimilarCube.transform.position - fromPosition).normalized;
                // Убираем вертикальную составляющую, оставляем только горизонтальное направление
                direction.y = 0;
                return direction.normalized;
            }

            // Если не найден похожий куб, возвращаем случайное горизонтальное направление
            return new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(0, 1f)).normalized;
        }

        protected override void OnDispose()
        {
            base.OnDispose();
        }
    }


    internal struct MergeEventData
    {
        public Vector3 mergePosition;
        public int oldNumber;
        public int newNumber;
        public Game2048CubeView newCubeView;
    }
}
