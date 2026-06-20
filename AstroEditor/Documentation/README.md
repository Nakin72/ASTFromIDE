# 📚 AstroEditor v4 — Индекс документации

## 🎯 Быстрая навигация

### Для разработчиков

| Документ | Описание | Для кого |
|----------|----------|----------|
| [CoreDocumentation.md](./CoreDocumentation.md) | **Полная документация по ядру** | Разработчики, архитекторы |
| [ModularStructure.md](./ModularStructure.md) | Модульная структура проекта | Разработчики ядра |
| [Plugins.md](./Plugins.md) | Система плагинов | Разработчики расширений |
| [RuntimeCompilation.md](./RuntimeCompilation.md) | Runtime компиляция и скрипты | Разработчики плагинов |
| [ExceptionHandling.md](./ExceptionHandling.md) | Обработка исключений | Разработчики программ |

### Для пользователей

| Документ | Описание | Для кого |
|----------|----------|----------|
| [CoreDocumentation.md](./CoreDocumentation.md#5-программы-и-инструкции) | Язык программирования | Программисты ASTRO |
| [CoreDocumentation.md](./CoreDocumentation.md#10-аварии-alarms) | Система аварий | Операторы, наладчики |
| [CoreDocumentation.md](./CoreDocumentation.md#11-прерывания-interrupts) | Прерывания и таймеры | Инженеры АСУ ТП |

---

## 📖 Полное оглавление

### 1. Общая информация

- [Архитектура системы](./CoreDocumentation.md#1-общая-архитектура)
- [Слои системы](./CoreDocumentation.md#11-слои-системы)
- [Поток выполнения](./CoreDocumentation.md#12-поток-выполнения)

### 2. Типы данных

- [Примитивные типы](./CoreDocumentation.md#22-примитивные-типы-primitivedatatype)
- [Перечисления (Enum)](./CoreDocumentation.md#23-перечисления-enumdatatype)
- [Структуры (Struct)](./CoreDocumentation.md#24-структуры-structdatatype)
- [Псевдонимы (Alias)](./CoreDocumentation.md#25-псевдонимы-aliasdatatype)
- [Реестр типов](./CoreDocumentation.md#26-реестр-типов-datatyperegistry)

### 3. Переменные и таблицы

- [Переменная](./CoreDocumentation.md#31-переменная-variable)
- [Таблица переменных](./CoreDocumentation.md#32-таблица-переменных-variabletableset)
- [Глобальные и локальные таблицы](./CoreDocumentation.md#33-глобальные-и-локальные-таблицы)

### 4. Привязки (Binding)

- [Направления привязки](./CoreDocumentation.md#41-направления-привязки)
- [Создание привязки](./CoreDocumentation.md#42-создание-привязки)
- [BindingRouter](./CoreDocumentation.md#43-bindingrouter)

### 5. Программы и инструкции

- [AstroProgram](./CoreDocumentation.md#51-программа-astroprogram)
- [Аргументы программы](./CoreDocumentation.md#52-аргументы-программы)
- [Instruction](./CoreDocumentation.md#53-инструкция-instruction)
- [Метки (Labels)](./CoreDocumentation.md#54-метки-labels)

### 6. Формы инструкций

- [FormDefinition](./CoreDocumentation.md#61-форма-formdefinition)
- [FormFieldDefinition](./CoreDocumentation.md#62-поля-формы-formfielddefinition)
- [FormRegistry](./CoreDocumentation.md#63-реестр-форм-formregistry)

### 7. Выражения и функции

- [ExpressionParser](./CoreDocumentation.md#71-парсер-выражений-expressionparser)
- [ExpressionEvaluator](./CoreDocumentation.md#72-вычислитель-expressionevaluator)
- [Встроенные функции](./CoreDocumentation.md#73-встроенные-функции)
- [Регистрация функций](./CoreDocumentation.md#74-регистрация-функций)

### 8. Интерпретатор

- [AstroInterpreter](./CoreDocumentation.md#81-astrointerpreter)
- [InterpreterState](./CoreDocumentation.md#82-состояние-интерпретатора-interpreterstate)
- [InterpreterContext](./CoreDocumentation.md#83-контекст-интерпретатора-interpretercontext)
- [Обработка инструкций](./CoreDocumentation.md#84-обработка-инструкций)

### 9. Обработка исключений

- [Инструкции TRY/CATCH](./CoreDocumentation.md#91-инструкции)
- [Пример использования](./CoreDocumentation.md#92-пример-использования)
- [ExceptionContext](./CoreDocumentation.md#93-контекст-исключения-exceptioncontext)

### 10. Аварии (Alarms)

- [AlarmSeverity](./CoreDocumentation.md#101-тяжесть-аварий)
- [AlarmDefinition](./CoreDocumentation.md#102-определение-аварии-alarmdefinition)
- [AlarmInstance](./CoreDocumentation.md#103-экземпляр-аварии-alarminstance)
- [AlarmManager](./CoreDocumentation.md#104-alarmmanager)

### 11. Прерывания (Interrupts)

- [Типы триггеров](./CoreDocumentation.md#111-типы-триггеров)
- [Режимы выполнения](./CoreDocumentation.md#112-режимы-выполнения)
- [InterruptDefinition](./CoreDocumentation.md#113-interruptdefinition)
- [InterruptManager](./CoreDocumentation.md#114-interruptmanager)

### 12. Таймеры

- [Режимы таймера](./CoreDocumentation.md#121-режимы-таймера)
- [TimerDefinition](./CoreDocumentation.md#122-timerdefinition)
- [TimerInstance](./CoreDocumentation.md#123-timerinstance)
- [TimerManager](./CoreDocumentation.md#124-timermanager)

### 13. Планировщик задач

- [Типы задач](./CoreDocumentation.md#131-типы-задач)
- [Приоритеты](./CoreDocumentation.md#132-приоритеты)
- [TaskConfig](./CoreDocumentation.md#133-taskconfig)
- [TaskScheduler](./CoreDocumentation.md#134-taskscheduler)

### 14. Система плагинов

- [IPlugin](./CoreDocumentation.md#141-интерфейс-плагина)
- [PluginContext](./CoreDocumentation.md#142-plugincontext)
- [PluginManager](./CoreDocumentation.md#143-pluginmanager)
- [Runtime компиляция](./CoreDocumentation.md#144-runtime-компиляция)
- [C# скрипты](./CoreDocumentation.md#145-c-скрипты)

### 15. Сериализация и хранение

- [Структура проекта](./CoreDocumentation.md#151-структура-проекта)
- [AstroSerializer](./CoreDocumentation.md#152-astroserializer)
- [Формат файлов](./CoreDocumentation.md#153-формат-файлов)

---

## 🔧 Справочники

### Список инструкций

[Полный список инструкций](./CoreDocumentation.md#a-список-всех-инструкций)

### Быстрый старт

[Пример инициализации и запуска](./CoreDocumentation.md#c-быстрый-старт)

---

## 📊 Диаграммы

### Архитектура

```
UI Layer
    ↓
Data Layer (ProjectManager, Types, Forms, Variables)
    ↓
Binding Layer (BindingManager, BindingRouter)
    ↓
Execution Layer (Interpreter, Scheduler, Interrupts, Timers, Alarms)
```

### Поток данных

```
ProjectManager → BindingManager → AstroInterpreter → TaskScheduler
                                            ↓
                              InterruptManager ← TimerManager
                                            ↓
                                      AlarmManager
```

---

## 🎓 Обучение

### Для начинающих

1. Начните с [быстрого старта](./CoreDocumentation.md#c-быстрый-старт)
2. Изучите [типы данных](./CoreDocumentation.md#2-типы-данных)
3. Создайте первую [программу](./CoreDocumentation.md#5-программы-и-инструкции)
4. Добавьте [привязки](./CoreDocumentation.md#4-привязки-binding)
5. Запустите через [интерпретатор](./CoreDocumentation.md#8-интерпретатор)

### Для продвинутых

1. [Система плагинов](./Plugins.md)
2. [Runtime компиляция](./RuntimeCompilation.md)
3. [Обработка исключений](./ExceptionHandling.md)
4. [Модульная структура](./ModularStructure.md)

---

## 📞 Поддержка

### Вопросы по ядру

- [Документация по ядру](./CoreDocumentation.md)
- [Модульная структура](./ModularStructure.md)

### Вопросы по плагинам

- [Система плагинов](./Plugins.md)
- [Runtime компиляция](./RuntimeCompilation.md)

### Вопросы по программированию

- [Обработка исключений](./ExceptionHandling.md)
- [Список инструкций](./CoreDocumentation.md#a-список-всех-инструкций)

---

**Версия:** 4.0  
**Последнее обновление:** 2024  
**Поддерживаемые версии:** AstroEditor 4.x
