# Экспорт программ в формате FANUC TP .ls

## Обзор

Проект ASTRO теперь поддерживает экспорт программ в человекочитаемом формате, похожем на **FANUC TP .ls** (паскалеподобный синтаксис). Этот формат удобен для:

- Чтения и редактирования программ в текстовом редакторе
- Версионного контроля (Git)
- Интеграции с внешними системами
- Печати документации

## Структура файла .ls

```
/PROG
  NAME: ИмяПрограммы
  COMMENT: "Описание"
  VERSION: 1.0
  AUTHOR: Автор
  RETURN_TYPE: INT
  MAX_CYCLES: 1000
/ATTR

!--- ARGUMENTS ---
  Аргумент1 : ТИП = Значение;
  Аргумент2 : ТИП OUT;

!--- LOCAL VARIABLES ---
  Переменная1 : ТИП = Значение;
  Переменная2 : ТИП = [1, 2, 3];

!--- CODE ---
/BODY
Присваивание := Выражение;
IF Условие THEN
  ...
ELSE
  ...
END_IF;
FOR Переменная := Нач TO Конечн DO
  ...
END_FOR;
FOR_EACH Элемент IN Коллекция DO
  ...
END_FOR_EACH;
SWITCH Выражение DO
  CASE Значение:
  ...
  DEFAULT:
  ...
END_SWITCH;
GOTO Метка;
Метка:
CALL Программа(аргументы);
RETURN Значение;
/END
```

## Синтаксис инструкций

### Присваивание
```pascal
Переменная := Выражение;  ! Комментарий
```

### Условия
```pascal
IF Условие THEN
  инструкции
ELSE
  инструкции
END_IF;

IF Условие THEN GOTO Метка;
```

### Циклы
```pascal
WHILE Условие DO
  инструкции
END_WHILE;

FOR Переменная := Нач TO Конечн BY Шаг DO
  инструкции
END_FOR;

FOR_EACH Элемент IN Коллекция DO
  инструкции
END_FOR_EACH;
```

### Switch
```pascal
SWITCH Выражение DO
  CASE Значение1:
  инструкции
  CASE Значение2:
  инструкции
  DEFAULT:
  инструкции
END_SWITCH;
```

### Переходы
```pascal
Метка:
GOTO Метка;
```

### Вызов программ
```pascal
CALL Программа(арг1, арг2);
CALL Программа;
```

### Возврат
```pascal
RETURN Значение;
RETURN;
```

### Прерывания
```pascal
BREAK;
CONTINUE;
```

### Ожидание
```pascal
WAIT 100ms;
WAIT FOR Условие;
```

### Аварии
```pascal
RAISE_ALARM Код;
RAISE_ALARM Код, Аргументы;
CLEAR_ALARM Код;
ACK_ALARM Код;
```

### Обработка исключений
```pascal
TRY
  инструкции
CATCH Переменная FROM Код DO
  инструкции
FINALLY
  инструкции
END_TRY;
```

## Комментарии

Комментарии начинаются с `!` и идут до конца строки:
```pascal
Sum := Sum + 1;  ! Инкремент суммы
```

## Типы данных

- Примитивы: `INT`, `REAL`, `DOUBLE`, `BOOL`, `STRING`, `CHAR`
- Пользовательские: `COLOR`, `POINT`, `SPEED` и др.
- Массивы: `[1, 2, 3]`
- Структуры: `{X: 100, Y: 200, Z: 50}`

## Литералы

- Числа: `42`, `3.14`, `-100`
- Строки: `'Hello World'`
- Булевы: `TRUE`, `FALSE`
- NULL: `NULL`

## Автоматический экспорт

При сохранении проекта через `ProjectManager.SaveAll()` программы автоматически экспортируются в трёх форматах:

1. **`.ast`** — JSON (основной формат хранения)
2. **`.txt`** — Табличный человекочитаемый формат
3. **`.ls`** — FANUC TP стиль (паскалеподобный)

## Пример

См. файлы в папке `AstroData/Programs/`:
- `MainProgram.ls`
- `Multiply.ls`
- `ArrayTest.ls`

## Интеграция

Для экспорта программы в коде:

```csharp
using AstroEditor.Core.Serialization;

FanucStyleExporter.SaveToFile(program, "path/to/file.ls", typeRegistry);
```

Или для получения строки:

```csharp
string lsContent = FanucStyleExporter.Generate(program, typeRegistry);
```
