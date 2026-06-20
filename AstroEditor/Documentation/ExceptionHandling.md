# 🚨 Обработка исключений в AstroEditor

## 📖 Обзор

AstroEditor v4 поддерживает полноценную обработку исключений через конструкции **TRY/CATCH/FINALLY**, аналогичные языкам C#, Java, Python.

---

## 🎯 Синтаксис

### Базовая структура

```astro
TRY
  # Код, который может вызвать ошибку
  X = 10 / Y
CATCH ErrorVar
  # Обработка ошибки
  ErrorVar = "Division by zero"
FINALLY
  # Выполняется всегда
  Cleanup()
ENDTRY
```

### Варианты использования

#### 1. TRY/CATCH

```astro
TRY
  Result = Value1 / Value2
CATCH Err
  Err = "Error occurred"
  Result = 0
ENDTRY
```

#### 2. TRY/FINALLY

```astro
TRY
  File = OpenFile("data.txt")
  # Работа с файлом
FINALLY
  CloseFile(File)  # Выполнится всегда
ENDTRY
```

#### 3. TRY/CATCH/FINALLY

```astro
TRY
  ProcessData()
CATCH Err
  LogError(Err)
FINALLY
  Cleanup()
ENDTRY
```

#### 4. Вложенные TRY

```astro
TRY
  TRY
    RiskyOperation1()
  CATCH Err1
    HandleError1(Err1)
  ENDTRY
  
  RiskyOperation2()
CATCH Err2
  HandleError2(Err2)
ENDTRY
```

---

## 🔧 Инструкции

### THROW

Выбрасывает исключение с указанным кодом и сообщением.

**Форма:** `core.throw`

**Поля:**
- `errorCode` (int) — Код ошибки (обязательно)
- `message` (string) — Сообщение об ошибке (опционально)
- `severity` (enum) — Тяжесть: Info, Warning, Error, Fatal

**Пример:**
```astro
# Выбросить ошибку с кодом 100
THROW ErrorCode=100, Message="Division by zero"

# Выбросить fatal ошибку
THROW ErrorCode=500, Message="Critical failure", Severity="Fatal"
```

---

### RETHROW

Повторно выбрасывает текущее исключение (используется в CATCH).

**Форма:** `core.rethrow`

**Пример:**
```astro
TRY
  ProcessData()
CATCH Err
  LogError(Err)
  RETHROW  # Пробросить ошибку дальше
ENDTRY
```

---

## 📊 Примеры использования

### Пример 1: Защита от деления на ноль

```astro
Dividend = 100
Divisor = GetUserInput()
Result = 0

TRY
  IF Divisor == 0 THEN
    THROW ErrorCode=1, Message="Division by zero"
  ENDIF
  Result = Dividend / Divisor
CATCH ErrMsg
  Result = 0
  LogError("Calculation failed: " + ErrMsg)
FINALLY
  DisplayResult(Result)
ENDTRY
```

---

### Пример 2: Работа с файлами

```astro
FileName = "data.csv"
FileHandle = 0

TRY
  FileHandle = OpenFile(FileName)
  
  TRY
    WHILE NOT EndOfFile(FileHandle) DO
      Line = ReadLine(FileHandle)
      ProcessLine(Line)
    ENDWHILE
  CATCH Err
    LogError("Read error: " + Err)
  FINALLY
    CloseFile(FileHandle)  # Всегда закрываем файл
  ENDTRY
  
CATCH Err
  LogError("Failed to open file: " + Err)
ENDTRY
```

---

### Пример 3: Валидация данных

```astro
UserData = GetInput()
IsValid = FALSE

TRY
  # Проверка 1: не пустое
  IF LEN(UserData) == 0 THEN
    THROW ErrorCode=10, Message="Input is empty"
  ENDIF
  
  # Проверка 2: формат
  IF NOT IsValidFormat(UserData) THEN
    THROW ErrorCode=11, Message="Invalid format"
  ENDIF
  
  # Проверка 3: диапазон
  Value = ToNumber(UserData)
  IF Value < 0 OR Value > 100 THEN
    THROW ErrorCode=12, Message="Value out of range"
  ENDIF
  
  IsValid = TRUE
  
CATCH ErrMsg
  IsValid = FALSE
  ShowError(ErrMsg)
  
FINALLY
  SaveValidationResult(IsValid)
ENDTRY
```

---

### Пример 4: Множественные операции

```astro
Errors = 0
SuccessCount = 0

FOR I = 1 TO 10 STEP 1
  TRY
    ProcessItem(I)
    SuccessCount = SuccessCount + 1
  CATCH Err
    Errors = Errors + 1
    LogError($"Item {I} failed: {Err}")
  ENDTRY
ENDFOR

PRINT $"Processed: {SuccessCount}, Errors: {Errors}"
```

---

### Пример 5: RETHROW для логирования

```astro
PROCEDURE CriticalOperation()
  TRY
    RiskyOperation()
  CATCH Err
    # Логирование
    WriteToLog("CriticalOperation failed: " + Err)
    
    # Проброс дальше для обработки на верхнем уровне
    RETHROW
  ENDTRY
ENDPROCEDURE

# Основной код
TRY
  CriticalOperation()
CATCH Err
  # Обработка на верхнем уровне
  ShowUserMessage("Operation failed: " + Err)
ENDTRY
```

---

## 🎯 Лучшие практики

### ✅ Делайте

1. **Используйте конкретные коды ошибок**
   ```astro
   THROW ErrorCode=1001, Message="File not found"
   THROW ErrorCode=1002, Message="Access denied"
   ```

2. **Всегда освобождайте ресурсы в FINALLY**
   ```astro
   TRY
     File = OpenFile()
     Process(File)
   FINALLY
     CloseFile(File)
   ENDTRY
   ```

3. **Логируйте ошибки в CATCH**
   ```astro
   CATCH Err
     LogError(Err)
     NotifyUser("Operation failed")
   ENDTRY
   ```

4. **Используйте вложенные TRY для сложных операций**
   ```astro
   TRY
     TRY
       ConnectToDatabase()
     CATCH Err
       HandleConnectionError(Err)
     ENDTRY
     
     TRY
       ExecuteQuery()
     CATCH Err
       HandleQueryError(Err)
     ENDTRY
   CATCH Err
     HandleGeneralError(Err)
   ENDTRY
   ```

---

### ❌ Не делайте

1. **Пустые CATCH блоки**
   ```astro
   # ПЛОХО
   TRY
     RiskyOperation()
   CATCH Err
     # Ничего не делаем - ошибка игнорируется!
   ENDTRY
   ```

2. **Слишком общие ошибки**
   ```astro
   # ПЛОХО
   CATCH Err
     # Неясно, что именно произошло
   ENDTRY
   
   # ХОРОШО
   CATCH Err
     LogError("Database operation failed: " + Err)
   ENDTRY
   ```

3. **Игнорирование FINALLY**
   ```astro
   # ПЛОХО - ресурс может не освободиться
   TRY
     File = OpenFile()
     Process(File)
   CATCH Err
     HandleError(Err)
   ENDTRY
   # File может остаться открытым!
   ```

---

## 📈 Коды ошибок

### Системные коды (1-999)

| Код | Описание |
|-----|----------|
| 1-99 | Математические ошибки (деление на 0, переполнение) |
| 100-199 | Ошибки ввода/вывода |
| 200-299 | Ошибки доступа |
| 300-399 | Ошибки типов данных |
| 400-499 | Ошибки выполнения программ |
| 500-999 | Зарезервировано |

### Пользовательские коды (1000+)

| Код | Описание |
|-----|----------|
| 1000-1999 | Ошибки прикладного уровня |
| 2000-2999 | Бизнес-логика |
| 3000-3999 | Интеграции |
| 4000+ | Специфичные ошибки |

---

## 🔍 Отладка

### Вывод информации об ошибке

```astro
TRY
  RiskyOperation()
CATCH Err
  PRINT "Error occurred!"
  PRINT "Message: " + Err
  PRINT "Line: " + GetCurrentLine()
FINALLY
  PRINT "Cleanup completed"
ENDTRY
```

### Логирование

```astro
TRY
  ProcessData()
CATCH Err
  LogError(
    Level="ERROR",
    Code=GetCurrentErrorCode(),
    Message=Err,
    Line=GetCurrentLine(),
    Program=GetCurrentProgram()
  )
ENDTRY
```

---

## 🎓 Сравнение с другими языками

| Конструкция | AstroEditor | C# | Python | Java |
|-------------|-------------|----|--------|------|
| Try блок | TRY | try | try | try |
| Catch блок | CATCH Err | catch (Exception e) | except Exception as e | catch (Exception e) |
| Finally блок | FINALLY | finally | finally | finally |
| Выброс | THROW ErrorCode=1 | throw new Exception() | raise Exception() | throw new Exception() |
| Повторный выброс | RETHROW | throw; | raise | throw |
| End блок | ENDTRY | } (конец блока) | (отступ) | } (конец блока) |

---

## 🚀 Интеграция с аварийной системой

TRY/CATCH работает вместе с системой аварий (Alarms):

```astro
TRY
  IF SensorValue > 100 THEN
    # Поднять аварию
    RAISE_ALARM Code=1001, Message="Sensor overload"
    
    # Выбросить исключение
    THROW ErrorCode=1001, Message="Sensor value too high"
  ENDIF
CATCH Err
  # Обработать и записать в лог
  LogError(Err)
  
  # Квитировать аварию если была
  ACK_ALARM Code=1001
ENDTRY
```

---

## 📊 Производительность

| Операция | Время |
|----------|-------|
| Вход в TRY | ~0.1 мкс |
| Выброс THROW | ~1-5 мкс |
| Обработка CATCH | ~0.5 мкс |
| Выполнение FINALLY | ~0.1 мкс |

**Рекомендация:** Не используйте TRY/CATCH для обычного потока управления — это медленнее чем IF/ELSE.

---

## 🔮 Планы развития

- [ ] Фильтрация по типу исключения
- [ ] Пользовательские типы исключений
- [ ] Stack trace для отладки
- [ ] Асинхронная обработка исключений
- [ ] Глобальные обработчики исключений

---

## 📚 Дополнительные ресурсы

- [Модульная структура](./ModularStructure.md)
- [Система плагинов](./Plugins.md)
- [Система аварий](./Alarms.md)
