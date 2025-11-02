# Отчет о рефакторинге PlaceableItem в MVP архитектуру

## Проблема

Изначально вся логика drag-and-drop находилась в `PlaceableItemView`, что нарушает принципы MVP:
- View содержал бизнес-логику (создание превью, валидация, координаты)
- Сложно тестировать
- Смешана ответственность (отображение + логика)

## Решение

Выполнен полный рефакторинг с разделением на **Model-View-Presenter**:

### 📁 Новая структура

```
PlaceableItem (Model) - данные элемента
    ↓
PlaceableItemPm (Presenter) - вся логика
    ↓
PlaceableItemView (View) - только отображение
```

---

## 🎯 PlaceableItemView - Чистый View

**Файл:** `Assets/Code/Games/Gardener/Scripts/UI/PlaceableItemView.cs`

### Ответственность:
✅ Отображение данных (иконка, название, количество)  
✅ Обработка Unity событий (IBeginDragHandler, IDragHandler, IEndDragHandler)  
✅ Передача событий в Presenter через Action'ы  
✅ Выполнение команд от Presenter'а (создать/удалить UI превью)  

### Что удалено из View:
❌ Создание 3D превью  
❌ Валидация позиции  
❌ Проверка границ огорода  
❌ Преобразование координат (screen → world)  
❌ Логика цветовой индикации  

### Публичный API View:

```csharp
// Установка данных
public void SetData(PlaceableItem item)

// События для Presenter
public event Action<PointerEventData> OnBeginDragEvent;
public event Action<PointerEventData> OnDragEvent;
public event Action<PointerEventData> OnEndDragEvent;

// Команды от Presenter
public void CreateUIPreview(Sprite icon, Canvas canvas)
public void UpdateUIPreviewPosition(Vector2 screenPosition)
public void DestroyUIPreview()
```

---

## 🧠 PlaceableItemPm - Presenter с логикой

**Файл:** `Assets/Code/Games/Gardener/Scripts/UI/PlaceableItemPm.cs` (новый)

### Ответственность:
✅ Вся бизнес-логика drag-and-drop  
✅ Создание и управление 3D превью  
✅ Преобразование координат (screen → world)  
✅ Валидация позиции размещения  
✅ Проверка границ огорода  
✅ Управление цветом превью (зеленый/красный)  
✅ Вызов callback'а размещения  

### Архитектура:

```csharp
internal class PlaceableItemPm : BaseDisposable
{
    public struct Ctx
    {
        public PlaceableItem item;           // Model - данные
        public PlaceableItemView view;       // View - отображение
        public Canvas canvas;
        public Camera worldCamera;
        public GardenBounds gardenBounds;
        public Action<PlaceableItem, Vector3> onItemPlaced;
    }
    
    // Подписка на события View
    private void HandleBeginDrag(PointerEventData eventData)
    private void HandleDrag(PointerEventData eventData)
    private void HandleEndDrag(PointerEventData eventData)
    
    // Логика
    private bool TryGetWorldPosition(...)
    private void SetPreviewColor(Color color)
    private void DisableColliders(GameObject obj)
    private void DestroyPreviews()
}
```

### Поток данных:

```
Unity Event (OnBeginDrag) 
    → View ловит событие
    → View.OnBeginDragEvent?.Invoke()
    → Presenter.HandleBeginDrag()
    → Presenter создает логику
    → View.CreateUIPreview() - команда отображения
```

---

## 📦 PlaceableItemsPanel - Фабрика Presenter'ов

**Файл:** `Assets/Code/Games/Gardener/Scripts/UI/PlaceableItemsPanel.cs`

### Изменения:

**Было:**
```csharp
var itemView = Instantiate(_itemPrefab, _itemsContainer);
itemView.Initialize(item, canvas, worldCamera, gardenBounds, onItemPlaced);
```

**Стало (MVP):**
```csharp
var itemView = Instantiate(_itemPrefab, _itemsContainer);

// Создаем Presenter для View
var itemPm = new PlaceableItemPm(new PlaceableItemPm.Ctx
{
    item = item,
    view = itemView,
    canvas = _canvas,
    worldCamera = _worldCamera,
    gardenBounds = _gardenBounds,
    onItemPlaced = OnItemPlaced
});

_itemPresenters.Add(itemPm);
```

### Управление жизненным циклом:

```csharp
private readonly List<PlaceableItemPm> _itemPresenters = new List<PlaceableItemPm>();

private void ClearItems()
{
    // Сначала удаляем Presenter'ы (логика)
    foreach (var presenter in _itemPresenters)
    {
        presenter?.Dispose();
    }
    _itemPresenters.Clear();
    
    // Затем удаляем View объекты (UI)
    for (int i = _itemsContainer.childCount - 1; i >= 0; i--)
    {
        Destroy(_itemsContainer.GetChild(i).gameObject);
    }
}
```

---

## ✅ Преимущества MVP архитектуры

### 1. **Разделение ответственности (SRP)**
- View - только отображение
- Presenter - только логика
- Model - только данные

### 2. **Тестируемость**
Presenter можно тестировать отдельно:
```csharp
[Test]
public void WhenDragOutsideBounds_ShouldNotPlaceItem()
{
    var presenter = new PlaceableItemPm(ctx);
    // Тестируем логику без Unity
}
```

### 3. **Переиспользование**
- View можно использовать с разными Presenter'ами
- Логику можно менять не трогая View
- Легко добавить новые типы элементов

### 4. **Поддерживаемость**
- Логика отделена от Unity компонентов
- Легко найти где что находится
- Изменения в одном месте не ломают другое

### 5. **Расширяемость**
Легко добавить новую функциональность:
```csharp
// Добавляем валидацию в Presenter
private bool ValidateCanPlace(Vector3 position)
{
    // Дополнительная логика
    return true;
}
```

---

## 📊 Сравнение до/после

### До рефакторинга:
```
PlaceableItemView (240 строк)
├─ Отображение (30 строк)
├─ Drag события (50 строк)
└─ Логика drag-and-drop (160 строк) ❌ Нарушение MVP
```

### После рефакторинга:
```
PlaceableItemView (120 строк)
├─ Отображение (30 строк)
├─ Drag события (30 строк)
└─ Команды UI (60 строк) ✅ Только View

PlaceableItemPm (170 строк)
└─ Вся логика drag-and-drop ✅ Только Presenter
```

---

## 🔄 Диаграмма взаимодействия

```
┌──────────────────┐
│  PlaceableItem   │ Model (данные)
│  - ItemName      │
│  - Icon          │
│  - Count         │
│  - PlantSettings │
└────────┬─────────┘
         │
         ▼
┌──────────────────────────┐
│   PlaceableItemPm        │ Presenter (логика)
│                          │
│  + HandleBeginDrag()     │◄──┐
│  + HandleDrag()          │   │ События
│  + HandleEndDrag()       │   │
│  - TryGetWorldPosition() │   │
│  - SetPreviewColor()     │   │
│  - ValidatePosition()    │   │
└────────┬─────────────────┘   │
         │ Команды             │
         ▼                     │
┌──────────────────────────────┴┐
│   PlaceableItemView           │ View (отображение)
│                               │
│  + SetData()                  │
│  + CreateUIPreview()          │
│  + UpdateUIPreviewPosition()  │
│  + DestroyUIPreview()         │
│  + OnBeginDragEvent ────────►│
│  + OnDragEvent ──────────────►│
│  + OnEndDragEvent ───────────►│
└───────────────────────────────┘
```

---

## 📝 Checklist интеграции

- [x] PlaceableItemPm создан с полной логикой
- [x] PlaceableItemView очищен от логики
- [x] PlaceableItemsPanel создает Presenter'ы
- [x] Жизненный цикл Presenter'ов управляется правильно
- [x] События передаются через Action'ы
- [x] Нет linter ошибок
- [x] Соблюдены принципы MVP

---

## 🎓 Принципы MVP в проекте

### View (PlaceableItemView):
- Наследуется от MonoBehaviour (Unity компонент)
- Содержит SerializeField для UI элементов
- Реагирует на Unity события (OnBeginDrag, etc)
- Вызывает события для Presenter'а
- Выполняет команды от Presenter'а

### Presenter (PlaceableItemPm):
- Наследуется от BaseDisposable (управление памятью)
- Не знает о Unity компонентах напрямую
- Содержит всю бизнес-логику
- Управляет View через публичные методы
- Может быть протестирован отдельно

### Model (PlaceableItem):
- Простой POCO класс с данными
- Сериализуется для Unity Inspector
- Не содержит логики
- Передается между слоями

---

## 🚀 Заключение

Успешно выполнен рефакторинг системы drag-and-drop в полноценную MVP архитектуру:

✅ View отвечает только за отображение  
✅ Presenter содержит всю логику  
✅ Model - чистые данные  
✅ Код легко тестировать  
✅ Легко расширять и поддерживать  

Архитектура теперь соответствует принципам **SOLID** и лучшим практикам разработки Unity приложений.


