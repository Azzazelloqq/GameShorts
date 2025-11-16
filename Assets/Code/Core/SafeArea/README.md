# Safe Area Module

Модуль для автоматической адаптации UI элементов под safe area мобильных устройств (вырезы, камеры, скругленные углы).

## Компоненты

### SafeAreaFitter
Основной компонент для адаптации RectTransform под safe area.

**Возможности:**
- Автоматическая адаптация под safe area устройства
- Настраиваемые стороны применения (left, right, top, bottom)
- Дополнительные отступы
- Режим отладки
- Работает в Editor с симуляцией notch

**Использование:**
```csharp
// Добавить на GameObject с RectTransform
gameObject.AddComponent<SafeAreaFitter>();
```

**Настройки в Inspector:**
- `Apply Left/Right/Top/Bottom` - выбор сторон для применения safe area
- `Additional Padding Top/Bottom` - дополнительные отступы
- `Show Debug Info` - отображение отладочной информации

### SafeAreaCanvas
Менеджер safe area на уровне Canvas. Управляет всеми SafeAreaFitter компонентами.

**Возможности:**
- Автоматическое обновление всех SafeAreaFitter
- Создание панелей с разными настройками safe area
- Централизованное управление

**Использование:**
```csharp
// Добавить на Canvas
canvas.AddComponent<SafeAreaCanvas>();

// Программное создание панели
var safeCanvas = GetComponent<SafeAreaCanvas>();
var panel = safeCanvas.AddSafeAreaPanel("MyPanel", true, true, true, true);
```

### SafeAreaHelper
Статический класс с утилитами для работы с safe area.

**Методы:**
```csharp
// Получить safe area
Rect safeArea = SafeAreaHelper.GetSafeArea();

// Получить нормализованную safe area (0-1)
Rect normalized = SafeAreaHelper.GetNormalizedSafeArea();

// Получить отступы от краев экрана
Vector4 insets = SafeAreaHelper.GetSafeAreaInsets();

// Проверить наличие notch
bool hasNotch = SafeAreaHelper.HasNotch();

// Определить тип устройства
var deviceType = SafeAreaHelper.GetEstimatedDeviceType();

// Проверить, находится ли точка в safe area
bool inSafeArea = SafeAreaHelper.IsPointInSafeArea(screenPoint);
```

## Примеры использования

### Базовое использование

1. **Простая адаптация всего UI:**
```csharp
// Создать Canvas
GameObject canvasGO = new GameObject("Canvas");
Canvas canvas = canvasGO.AddComponent<Canvas>();
canvas.renderMode = RenderMode.ScreenSpaceOverlay;

// Добавить SafeAreaCanvas
SafeAreaCanvas safeCanvas = canvasGO.AddComponent<SafeAreaCanvas>();
// Автоматически создается панель с SafeAreaFitter
```

2. **Разные safe area для разных панелей:**
```csharp
SafeAreaCanvas safeCanvas = GetComponent<SafeAreaCanvas>();

// Панель только с верхним safe area (для header)
var headerPanel = safeCanvas.AddSafeAreaPanel("Header", false, false, true, false);

// Панель только с нижним safe area (для navigation bar)
var bottomPanel = safeCanvas.AddSafeAreaPanel("BottomNav", false, false, false, true);

// Полная safe area панель
var mainPanel = safeCanvas.AddSafeAreaPanel("Main", true, true, true, true);
```

3. **Ручная настройка через Inspector:**
   - Добавьте `SafeAreaFitter` на любой UI объект
   - Настройте в Inspector какие стороны применять
   - Добавьте дополнительные отступы при необходимости

### Продвинутое использование

**Адаптивная верстка в зависимости от устройства:**
```csharp
void SetupAdaptiveUI()
{
    var deviceType = SafeAreaHelper.GetEstimatedDeviceType();
    
    switch (deviceType)
    {
        case SafeAreaHelper.DeviceType.PhoneWithNotch:
            // Увеличенные отступы для телефонов с notch
            SetupPhoneWithNotchUI();
            break;
            
        case SafeAreaHelper.DeviceType.Tablet:
            // Планшетная верстка
            SetupTabletUI();
            break;
            
        default:
            // Стандартная верстка
            SetupDefaultUI();
            break;
    }
}
```

**Динамическое позиционирование элементов:**
```csharp
void PositionFloatingButton()
{
    var insets = SafeAreaHelper.GetSafeAreaInsets();
    
    // Позиционировать кнопку с учетом safe area
    floatingButton.anchoredPosition = new Vector2(
        -20 - insets.z,  // Отступ от правого края + safe area
        20 + insets.y    // Отступ от нижнего края + safe area
    );
}
```

## Тестирование в Editor

В Unity Editor компонент симулирует notch в стиле iPhone X для тестирования:
- Portrait: notch сверху, home indicator снизу
- Landscape: notch по бокам

Включите `Show Debug Info` в SafeAreaFitter для отображения границ safe area.

## Поддерживаемые платформы

- iOS (iPhone, iPad)
- Android (устройства с вырезами и скругленными углами)
- Unity Editor (с симуляцией)

## Производительность

- Кэширование safe area для минимизации вызовов Screen.safeArea
- Обновление только при изменении параметров экрана
- Настраиваемый интервал обновления в SafeAreaCanvas

## Troubleshooting

**UI элементы не адаптируются:**
- Убедитесь, что SafeAreaFitter добавлен на объект с RectTransform
- Проверьте настройки Apply Left/Right/Top/Bottom
- Вызовите ForceRefresh() для принудительного обновления

**Неправильные отступы:**
- Проверьте anchor и pivot настройки RectTransform
- Убедитесь, что родительский элемент имеет правильные размеры
- Используйте Show Debug Info для визуализации границ

**Не работает в Editor:**
- Симуляция notch работает автоматически
- Переключите Game View в режим симуляции мобильного устройства
- Измените соотношение сторон для тестирования разных ориентаций

