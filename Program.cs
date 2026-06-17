using AstroEditor.Core.v3.Types;
using AstroEditor.Core.v3.Variables;

var tableManager = new VariableTableManager();

// 1. Создаем кастомный тип (например, "ItemInfo")
CoreDataType ItemInfoType = TypeRegistry.RegisterUserStructType("ItemInfo");

// 2. Создаем сложную структуру персонажа ("PlayerProfile")
CoreDataType PlayerProfileType = TypeRegistry.RegisterUserStructType("PlayerProfile");

// 3. Собираем данные: добавляем "ItemInfo" как поле внутрь "PlayerProfile"
var playerStructData = new CoreStruct();

// Поле "InventorySlot" использует удаляемый в будущем тип "ItemInfo"
playerStructData.AddField("InventorySlot", new CoreDataContainer(ItemInfoType));
playerStructData.AddField("Money", new CoreDataContainer(CoreDataType.Integer, 500));

// Кладим профиль игрока в глобальную переменную
CoreVariable heroVar = tableManager.DeclareVariable(
    "HeroData",
    PlayerProfileType,
    playerStructData,
    VariableScope.Global,
    AccessLevel.User
);

// --- ИМИТАЦИЯ УДАЛЕНИЯ ТИПА В UI ---
Console.WriteLine($"Пользователь пытается удалить тип данных '{ItemInfoType.Name}'...");

// Перед удалением ищем, где этот тип использовался, чтобы пометить эти места красным в IDE
List<CoreVariable> brokenVars = tableManager.FindVariablesUsingType(ItemInfoType);

Console.WriteLine($"\n[Оповещение IDE]: Найдены зависимые переменные, которые нужно подсветить красным:");
foreach (var v in brokenVars)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"-> Переменная: '{v.Name}' (Тип: {v.Container.DataType.Name}) содержит удаляемый тип внутри себя!");
    Console.ResetColor();
}
// ВЫВОД: Подсветит переменную 'HeroData', так как внутри неё рекурсивно найдено поле "InventorySlot" с типом "ItemInfo".

// Теперь мы можем со спокойной совестью удалить тип из реестра. 
// Данные в памяти не упадут с ошибкой, но UI формы поймет, что поле "InventorySlot" теперь "Invalid".
TypeRegistry.UnregisterType(ItemInfoType.Name);
Console.WriteLine($"\nТип '{ItemInfoType.Name}' успешно удален. Редактор переведен в режим отображения ошибок связи.");

