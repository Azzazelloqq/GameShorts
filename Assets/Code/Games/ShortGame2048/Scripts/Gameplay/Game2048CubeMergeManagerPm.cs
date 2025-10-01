using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using UnityEngine;
using R3;

namespace Code.Games
{
    internal class Game2048CubeMergeManagerPm : BaseDisposable
    {
        internal struct Ctx
        {
            public Game2048CubeSpawnerPm cubeSpawner;
            public GameObject cubePrefab;
            public Transform spawnPoint;
            public float mergeUpwardForce;
            public float mergeForwardForce;
            public CancellationToken cancellationToken;
            public Action<Guid> disposeCube;
            public IReadOnlyDictionary<Guid, CubePm> cubes;
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
            var activeCubes = _ctx.cubes;
            
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

            _ctx.disposeCube(cube1.Id);
            _ctx.disposeCube(cube2.Id);

            // Создаем новый куб в точке мержа
            var newCube = CreateMergedCube(mergePosition, newNumber);

            // Применяем физический толчок
            ApplyMergeImpulse(newCube.View, mergePosition, newNumber);
        }

        private CubePm CreateMergedCube(Vector3 position, int number)
        {
            // Создаем новый куб через спавнер
            var newCube = _ctx.cubeSpawner.CreateCubeAtPosition(position, number);
            return newCube;
        }

        private void ApplyMergeImpulse(Game2048CubeView newCube, Vector3 mergePosition, int newNumber)
        {
            var rigidbody = newCube.GetComponent<Rigidbody>();
            if (rigidbody == null) return;

            newCube.ResetVelocity();

            Vector3 upwardForce = Vector3.up * _ctx.mergeUpwardForce;
            Vector3 forwardForce = Vector3.forward * _ctx.mergeForwardForce;

            // Ищем ближайший куб с таким же числом для небольшого бокового смещения
            Vector3 directionToSimilarCube = FindDirectionToNearestSimilarCube(mergePosition, newNumber);
            
            // Добавляем небольшой наклон в сторону ближайшего похожего куба (уменьшил силу)
            Vector3 lateralForce = directionToSimilarCube * 1f;

            // Применяем комбинированную силу: вперед + вверх + немного в бок
            Vector3 totalForce = forwardForce + upwardForce + lateralForce;
            rigidbody.AddForce(totalForce, ForceMode.Impulse);

            // Добавляем небольшое вращение для эффектности (уменьшил интенсивность)
            Vector3 randomTorque = new Vector3(
                UnityEngine.Random.Range(-0.5f, 0.5f),
                UnityEngine.Random.Range(-0.5f, 0.5f),
                UnityEngine.Random.Range(-0.5f, 0.5f)
            ) * 1.5f;
            rigidbody.AddTorque(randomTorque, ForceMode.Impulse);
        }

        private Vector3 FindDirectionToNearestSimilarCube(Vector3 fromPosition, int targetNumber)
        {
            // Получаем список активных кубов из спавнера
            var activeCubes = _ctx.cubes;
            
            CubePm nearestSimilarCube = null;
            float nearestDistance = float.MaxValue;

            foreach (var cube in activeCubes)
            {
                if (cube.Value.Number == targetNumber)
                {
                    float distance = Vector3.Distance(fromPosition, cube.Value.View.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestSimilarCube = cube.Value;
                    }
                }
            }

            if (nearestSimilarCube != null)
            {
                Vector3 direction = (nearestSimilarCube.View.transform.position - fromPosition).normalized;
                // Убираем вертикальную составляющую, оставляем только горизонтальное направление
                direction.y = 0;
                return direction.normalized;
            }

            // Если не найден похожий куб, возвращаем случайное боковое направление (без Z)
            // Чтобы не мешать основному движению вперед
            return new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, 0).normalized;
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
