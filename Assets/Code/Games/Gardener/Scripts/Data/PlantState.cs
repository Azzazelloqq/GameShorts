namespace GameShorts.Gardener.Data
{
    /// <summary>
    /// Состояния растения
    /// </summary>
    internal enum PlantState
    {
        Seed,       // Семя
        Sprout,     // Саженец
        Bush,       // Куст
        Flowering,  // Цветение
        Fruit,      // Плоды
        Rotten,     // Сгнившее
        Empty       // Пустая грядка
    }
}