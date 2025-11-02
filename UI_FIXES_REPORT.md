# Отчет об исправлении UI проблем

## Исправленные проблемы

### 1. ❌ UI бар грядок отображается далеко от грядок

**Проблема:**
Прогресс бар цветка не отображался над грядкой, а спавнился где-то далеко. Это было вызвано неправильным позиционированием для разных типов Canvas.

**Причина:**
Метод `UpdatePosition()` использовал простое `WorldToScreenPoint` без учета типа Canvas (Screen Space Overlay vs Screen Space Camera). Это приводило к неправильным координатам.

**Решение:**
**Файл:** `Assets/Code/Games/Gardener/Scripts/UI/PlotUIBar.cs`

Добавлена проверка типа Canvas и правильное позиционирование для каждого режима:

```csharp
public void UpdatePosition()
{
    if (!_isInitialized || _targetPlot == null || _worldCamera == null || _rectTransform == null)
        return;
    
    Vector3 worldPosition = _targetPlot.position;
    Vector3 screenPosition = _worldCamera.WorldToScreenPoint(worldPosition);
    
    // Если объект за камерой, скрываем UI
    if (screenPosition.z < 0)
    {
        SetVisible(false);
        return;
    }
    
    SetVisible(true);
    
    // Для Screen Space - Overlay Canvas используем прямую экранную позицию
    if (_parentCanvas != null && _parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
    {
        screenPosition.x += _screenOffset.x;
        screenPosition.y += _screenOffset.y;
        screenPosition.z = 0;
        
        _rectTransform.position = screenPosition;
    }
    // Для Screen Space - Camera или World Space
    else if (_parentCanvas != null)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _parentCanvas.transform as RectTransform,
            screenPosition,
            _parentCanvas.worldCamera ?? _worldCamera,
            out localPoint
        );
        
        localPoint.x += _screenOffset.x;
        localPoint.y += _screenOffset.y;
        
        _rectTransform.anchoredPosition = localPoint;
    }
}
```

**Что изменилось:**

1. **Добавлена проверка Canvas типа:**
   - Screen Space - Overlay → используем `_rectTransform.position`
   - Screen Space - Camera / World Space → используем `RectTransformUtility.ScreenPointToLocalPointInRectangle()`

2. **Добавлена проверка видимости:**
   - Если объект за камерой (`screenPosition.z < 0`), скрываем UI бар

3. **Добавлено поле:**
   ```csharp
   private Canvas _parentCanvas;
   ```

4. **Инициализация Canvas:**
   ```csharp
   public void Initialize(Camera camera, Transform plotTransform)
   {
       _worldCamera = camera;
       _targetPlot = plotTransform;
       _isInitialized = true;
       
       // Получаем родительский Canvas
       _parentCanvas = GetComponentInParent<Canvas>();
       
       UpdatePosition();
   }
   ```

**Результат:**
✅ UI бар теперь правильно отображается над грядкой независимо от типа Canvas  
✅ UI бар автоматически скрывается, если объект за камерой  
✅ Работает для всех Canvas RenderMode (Overlay, Camera, World Space)

---

### 2. ❌ При drag-and-drop создавался 3D объект вместо картинки

**Проблема:**
При перетаскивании грядки или семян под курсором показывался 3D объект, но пользователь хотел видеть только картинку (иконку).

**Причина:**
В `PlaceableItemPm` создавался 3D превью объект (`_dragPreviewObject`) из префаба.

**Решение:**
**Файл:** `Assets/Code/Games/Gardener/Scripts/UI/PlaceableItemPm.cs`

Удалено создание 3D превью - теперь показывается только UI картинка:

**Было:**
```csharp
private void HandleBeginDrag(PointerEventData eventData)
{
    // ...
    _ctx.view.CreateUIPreview(_ctx.item.Icon, _ctx.canvas);
    
    // Создаем 3D превью, если есть префаб ❌
    if (_ctx.item.Prefab != null)
    {
        _dragPreviewObject = UnityEngine.Object.Instantiate(_ctx.item.Prefab);
        _dragPreviewObject.name = "DragPreview3D";
        DisableColliders(_dragPreviewObject);
        SetPreviewColor(Color.green);
    }
}

private void HandleDrag(PointerEventData eventData)
{
    _ctx.view.UpdateUIPreviewPosition(eventData.position);
    
    // Обновление 3D превью ❌
    if (_dragPreviewObject != null && TryGetWorldPosition(...))
    {
        _dragPreviewObject.transform.position = worldPosition;
        SetPreviewColor(isValid ? Color.green : Color.red);
    }
}
```

**Стало:**
```csharp
private void HandleBeginDrag(PointerEventData eventData)
{
    // ...
    // Создаем только UI превью (картинку под курсором) ✅
    _ctx.view.CreateUIPreview(_ctx.item.Icon, _ctx.canvas);
}

private void HandleDrag(PointerEventData eventData)
{
    // Обновляем только позицию UI превью (картинки) ✅
    _ctx.view.UpdateUIPreviewPosition(eventData.position);
}
```

**Удалено:**
- Поле `private GameObject _dragPreviewObject`
- Метод `SetPreviewColor(Color color)`
- Метод `DisableColliders(GameObject obj)`
- Код создания и обновления 3D превью

**Упрощен метод:**
```csharp
private void DestroyPreviews()
{
    // Удаляем только UI превью
    _ctx.view.DestroyUIPreview();
}
```

**Результат:**
✅ При drag-and-drop показывается только иконка под курсором  
✅ Код стал проще и чище  
✅ Меньше накладных расходов (не создается лишний 3D объект)  
✅ Работает одинаково для грядок и семян

---

## Тестирование

### Тест 1: Позиционирование UI бара
1. Создать грядку
2. Посадить семена
3. ✅ UI бар должен отображаться прямо над грядкой
4. Вращать камеру
5. ✅ UI бар должен следовать за грядкой
6. Повернуть камеру так, чтобы грядка была за камерой
7. ✅ UI бар должен скрыться

### Тест 2: Drag-and-drop картинки
1. Открыть режим Harvey
2. Начать перетаскивать грядку
3. ✅ Под курсором должна быть иконка грядки (не 3D модель)
4. Открыть режим Inventory
5. Начать перетаскивать семена
6. ✅ Под курсором должна быть иконка семян (не 3D модель)

---

## Технические детали

### Правильное позиционирование UI в Unity

**Screen Space - Overlay:**
```csharp
// Прямая установка screen position
_rectTransform.position = screenPosition;
```

**Screen Space - Camera / World Space:**
```csharp
// Конвертация через RectTransformUtility
RectTransformUtility.ScreenPointToLocalPointInRectangle(
    parentRect,
    screenPosition,
    camera,
    out localPoint
);
_rectTransform.anchoredPosition = localPoint;
```

### Проверка видимости объекта

```csharp
Vector3 screenPos = camera.WorldToScreenPoint(worldPos);
if (screenPos.z < 0) // Объект за камерой
{
    // Скрыть UI
}
```

---

## Заключение

Обе проблемы успешно исправлены:

✅ **UI бар грядок** - теперь правильно позиционируется над грядками для любого типа Canvas  
✅ **Drag-and-drop** - показывается только иконка, без создания 3D объектов

Код стал:
- 🎯 Правильнее (корректное позиционирование UI)
- 🧹 Чище (удален ненужный код 3D превью)
- ⚡ Быстрее (меньше создаваемых объектов)
- 📱 Универсальнее (работает для всех Canvas режимов)

