# InputManager System

Система управления джойстиками для GameShorts проекта.

## Структура

- **IInputManager** - интерфейс для управления системой ввода
- **InputManager** - основная реализация системы управления вводом  
- **JoystickMode** - enum с режимами работы джойстика
- **InputManagerExample** - пример использования системы

## Режимы джойстика

- **Circular** - круговой джойстик, движение во все стороны
- **Horizontal** - горизонтальный джойстик, только влево и вправо
- **Disabled** - джойстик отключен

## Использование

### 1. Инициализация InputManager

```csharp
IInputManager inputManager = new InputManager();
inputManager.Initialize(variableJoystickReference);
```

### 2. Настройка джойстика для игры

```csharp
// В IShortGame добавлено поле RequiredJoystickMode
public JoystickMode RequiredJoystickMode => JoystickMode.Circular;

// При смене игры
inputManager.SetJoystickMode(currentGame.RequiredJoystickMode);
```

### 3. Получение ввода

```csharp
Vector2 input = inputManager.GetJoystickInput();
bool isActive = inputManager.IsJoystickActive;
```

## Интеграция с IShortGame

В интерфейс `IShortGame` добавлено новое поле:

```csharp
public JoystickMode RequiredJoystickMode { get; }
```

Каждая игра должна указывать, какой режим джойстика ей требуется.

## Пример реализации в игре

```csharp
public class MyGame : IShortGame
{
    public int Id => 1;
    public JoystickMode RequiredJoystickMode => JoystickMode.Horizontal;
    
    // ... остальные методы
}
```

## Масштабирование

Система спроектирована для легкого добавления новых режимов джойстика:
1. Добавить новый режим в enum `JoystickMode`
2. Обновить логику в `InputManager.SetJoystickMode()`
3. При необходимости обновить логику в `GetJoystickInput()`
