# GameSwiper Integration - Архитектура MVC

## Обзор

GameSwiper - это система для управления переключением между мини-играми, построенная по паттерну MVC. Система обеспечивает четкое разделение ответственности между компонентами.

## Архитектура MVC

### Model - GameSwiper
**Бизнес-логика** переключения игр:
- Реализует интерфейс `ISwiperGame`
- Использует `IShortGameLifeCycleService` для управления играми
- Обрабатывает async операции загрузки
- Логирует все операции

### View - GameSwiperView
**Чистая View** без бизнес-логики:
- Содержит только UI элементы и обработку ввода
- Генерирует реактивные события: `OnNextGameRequested`, `OnPreviousGameRequested`
- Поддерживает различные способы ввода (кнопки, свайпы, клавиатура)
- Управляет визуальным состоянием (загрузка, активность кнопок)

### Controller - GameSwiperController
**Связующее звено** между Model и View:
- Подписывается на события View
- Вызывает методы Model
- Управляет состоянием View (индикатор загрузки)
- Обрабатывает ошибки и отмены операций

## Компоненты системы

### 1. ISwiperGame (Interface)
```csharp
public interface ISwiperGame
{
    ValueTask<IShortGame> NextGameAsync(CancellationToken cancellationToken = default);
    ValueTask<IShortGame> PreviousGameAsync(CancellationToken cancellationToken = default);
}
```

### 2. GameSwiper (Model)
- Основная бизнес-логика
- Интеграция с `IShortGameLifeCycleService`
- Обработка асинхронных операций

### 3. GameSwiperView (View)
**Реактивные события:**
- `OnNextGameRequested` - запрос следующей игры
- `OnPreviousGameRequested` - запрос предыдущей игры
- `OnGameChanged` - уведомление о смене игры

**Методы управления состоянием:**
- `SetLoadingState(bool)` - показ/скрытие индикатора загрузки
- `SetSwipeGesturesEnabled(bool)` - включение/выключение свайпов
- `SetKeyboardInputEnabled(bool)` - включение/выключение клавиатуры

### 4. GameSwiperController (Controller)
- Управляет связью между Model и View
- Обрабатывает события View
- Вызывает операции Model
- Управляет состоянием View

### 5. GameSwiperFactory
Фабрика для создания GameSwiper с зависимостями:
```csharp
public static GameSwiper CreateGameSwiper(
    IShortGameLifeCycleService lifeCycleService, 
    IInGameLogger logger)
```

## Поток данных

```
User Input (View) 
    → Event (OnNextGameRequested)
    → Controller (HandleNextGameRequested)
    → Model (NextGameAsync)
    → Controller (HandleResponse)
    → View (SetLoadingState, OnGameChanged)
```

## Интеграция в GameEntryPoint

1. **Создание Model** - `GameSwiperFactory.CreateGameSwiper()`
2. **Создание Controller** - `new GameSwiperController(gameSwiper, logger)`
3. **Загрузка View** - загружается префаб `GameSwiperView`
4. **Связывание** - `controller.AttachView(view)`

## Настройка

### GameSwiperView настройки:
- `_swipeThreshold` - чувствительность свайпа (50px)
- `_enableSwipeGestures` - включить свайп-жесты
- `_enableKeyboardInput` - включить клавиатурный ввод
- `_loadingIndicator` - индикатор загрузки

### GameEntryPoint настройки:
- `_uiParent` - родительский объект для UI
- `_gameSwiperViewPrefabPath` - путь к префабу

## Способы взаимодействия

### 1. UI Кнопки
- "Next Game" / "Previous Game"
- Автоматически отключаются во время загрузки

### 2. Свайп-жесты
- Свайп вверх → предыдущая игра
- Свайп вниз → следующая игра
- Настраиваемый порог срабатывания

### 3. Клавиатурный ввод (Editor)
- ↑ / ↓ - навигация между играми
- Только в редакторе Unity

### 4. Программное управление
```csharp
var gameSwiper = container.GetInstance<ISwiperGame>();
await gameSwiper.NextGameAsync();
```

## Тестирование

**GameSwiperTester** для проверки системы:
- Клавиши N/P - тестирование навигации
- Клавиша C - проверка контроллера
- Автоматическое подключение при инициализации

## Преимущества новой архитектуры

1. **Разделение ответственности** - четкие границы между компонентами
2. **Тестируемость** - каждый компонент можно тестировать изолированно
3. **Расширяемость** - легко добавить новые способы ввода или логику
4. **Переиспользование** - View можно использовать с разными контроллерами
5. **Чистота кода** - View содержит только UI логику, Model - только бизнес-логику

## Файловая структура

```
Assets/Code/Core/GameSwiper/
├── ISwiperGame.cs              # Интерфейс
├── GameSwiper.cs               # Model (бизнес-логика)
├── GameSwiperView.cs           # View (чистый UI)
├── GameSwiperController.cs     # Controller (связующее звено)
├── GameSwiperFactory.cs        # Фабрика создания
└── README.md                   # Документация
```

## Lifecycle Management

1. **Инициализация**: GameEntryPoint создает все компоненты
2. **Связывание**: Controller подключается к View
3. **Работа**: События View → Controller → Model → Controller → View
4. **Очистка**: Controller отписывается от View, освобождает ресурсы
