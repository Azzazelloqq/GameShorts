# Simple Game Entry Point

## Быстрый старт

### 1. Настройка в Unity

1. Создайте GameObject в сцене
2. Добавьте компонент `SimpleGameEntryPoint`
3. Настройте в инспекторе:
   - **Games Parent** - родительский объект для игр (опционально)
   - **Preload Depth** - количество игр для предзагрузки (по умолчанию 2)

### 2. Использование

#### Вариант 1: С GameInitializer
1. Добавьте компонент `GameInitializer` на GameObject
2. Укажите ссылку на `SimpleGameEntryPoint`
3. Добавьте типы игр в код:

```csharp
private readonly List<Type> _gameTypes = new()
{
    typeof(MyGame1),
    typeof(MyGame2),
    typeof(MyGame3)
};
```

#### Вариант 2: Прямое использование
```csharp
public class MyController : MonoBehaviour
{
    [SerializeField] private SimpleGameEntryPoint _entryPoint;
    
    async void Start()
    {
        // Инициализация с играми
        var games = new List<Type>
        {
            typeof(Game1),
            typeof(Game2),
            typeof(Game3)
        };
        
        await _entryPoint.InitializeQueueAsync(games);
        
        // Загрузка первой игры
        var game = await _entryPoint.LoadNextGameAsync();
    }
    
    public async void NextGame()
    {
        await _entryPoint.LoadNextGameAsync();
    }
    
    public async void PreviousGame()
    {
        await _entryPoint.LoadPreviousGameAsync();
    }
}
```

### 3. Предоставление ресурсов

Создайте наследника `SimpleGameEntryPoint` и переопределите метод `GetResourceMapping`:

```csharp
public class MyGameEntryPoint : SimpleGameEntryPoint
{
    protected override Dictionary<Type, string> GetResourceMapping()
    {
        return new Dictionary<Type, string>
        {
            { typeof(PuzzleGame), "Games/PuzzleGame" },
            { typeof(RacingGame), "Games/RacingGame" },
            { typeof(PlatformerGame), "Games/PlatformerGame" }
        };
    }
}
```

## API

### SimpleGameEntryPoint

- `QueueLoader` - доступ к загрузчику очереди
- `CurrentGame` - текущая загруженная игра
- `InitializeQueueAsync(IReadOnlyList<Type> gameTypes)` - инициализация очереди
- `LoadNextGameAsync()` - загрузить следующую игру
- `LoadPreviousGameAsync()` - загрузить предыдущую игру
- `LoadGameByIndexAsync(int index)` - загрузить игру по индексу
- `StopCurrentGame()` - остановить текущую игру

### QueueShortGamesLoader

Доступен через `entryPoint.QueueLoader`:

- `CurrentGameIndex` - индекс текущей игры
- `TotalGamesCount` - общее количество игр
- `GameQueue` - список типов игр в очереди
- `PreloadDepth` - глубина предзагрузки
- `AddGameToQueue(Type)` - добавить игру в очередь
- `RemoveGameFromQueue(Type)` - удалить игру из очереди
- `ClearQueue()` - очистить очередь

## Пример с UI

```csharp
public class GameUI : MonoBehaviour
{
    [SerializeField] private SimpleGameEntryPoint _entryPoint;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Button _previousButton;
    [SerializeField] private Text _gameNameText;
    
    void Start()
    {
        _nextButton.onClick.AddListener(async () =>
        {
            var game = await _entryPoint.LoadNextGameAsync();
            UpdateUI(game);
        });
        
        _previousButton.onClick.AddListener(async () =>
        {
            var game = await _entryPoint.LoadPreviousGameAsync();
            UpdateUI(game);
        });
    }
    
    void UpdateUI(IShortGame game)
    {
        if (game != null)
        {
            _gameNameText.text = game.GetType().Name;
            
            var loader = _entryPoint.QueueLoader;
            _previousButton.interactable = loader.CurrentGameIndex > 0;
            _nextButton.interactable = loader.CurrentGameIndex < loader.TotalGamesCount - 1;
        }
    }
}
```
