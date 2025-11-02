using Code.Core.BaseDMDisposable.Scripts;
using GameShorts.Gardener.Data;
using UnityEngine;

namespace GameShorts.Gardener.View
{
    internal class PlotView : BaseMonoBehaviour
    {
        [SerializeField] private Transform _plantContainer;
        [SerializeField] private GameObject _emptyPlotModel;
        [SerializeField] private GameObject _rottenPlantModel;
        
        private GameObject _currentPlantModel;

        public void UpdateState(PlantState state, PlantSettings plantSettings)
        {
            // Очищаем текущую модель растения
            if (_currentPlantModel != null)
            {
                Destroy(_currentPlantModel);
                _currentPlantModel = null;
            }

            // Показываем соответствующую модель
            switch (state)
            {
                case PlantState.Empty:
                    _emptyPlotModel.SetActive(true);
                    _rottenPlantModel.SetActive(false);
                    break;
                
                case PlantState.Rotten:
                    _emptyPlotModel.SetActive(false);
                    _rottenPlantModel.SetActive(true);
                    break;
                
                default:
                    if (plantSettings != null)
                    {
                        _emptyPlotModel.SetActive(false);
                        _rottenPlantModel.SetActive(false);
                        
                        // Создаем модель растения в соответствии с состоянием
                        GameObject modelPrefab = GetModelForState(state, plantSettings);
                        if (modelPrefab != null)
                        {
                            _currentPlantModel = Instantiate(modelPrefab, _plantContainer);
                        }
                    }
                    break;
            }
        }

        private GameObject GetModelForState(PlantState state, PlantSettings plantSettings)
        {
            return state switch
            {
                PlantState.Seed => plantSettings.SeedModel,
                PlantState.Sprout => plantSettings.SproutModel,
                PlantState.Bush => plantSettings.BushModel,
                PlantState.Flowering => plantSettings.FloweringModel,
                PlantState.Fruit => plantSettings.FruitModel,
                _ => null
            };
        }
    }
}