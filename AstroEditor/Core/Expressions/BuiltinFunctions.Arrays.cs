// AstroEditor.Core/Expressions/BuiltinFunctions.Arrays.cs
// Встроенные функции для работы с массивами

namespace AstroEditor.Core.Expressions;

public static partial class BuiltinFunctions
{
    public static Dictionary<string, Func<object?[], object?>> GetArrayFunctions()
    {
        return new Dictionary<string, Func<object?[], object?>>
        {
            /// <summary>SIZE(arr) — возвращает размер массива</summary>
            { "SIZE", args =>
                {
                    var arr = NormalizeArray(args[0]);
                    return arr?.Count ?? 0;
                }
            },
            
            /// <summary>ADD(arr, value) — добавляет элемент в конец массива, возвращает новый размер</summary>
            { "ADD", args =>
                {
                    var arr = NormalizeArray(args[0]);
                    if (arr == null) throw new Exception("ADD: first argument must be an array");
                    arr.Add(args[1]);
                    return arr.Count;
                }
            },
            
            /// <summary>REMOVE(arr, index) — удаляет элемент по индексу, возвращает true если успешно</summary>
            { "REMOVE", args =>
                {
                    var arr = NormalizeArray(args[0]);
                    if (arr == null) throw new Exception("REMOVE: first argument must be an array");
                    var index = Convert.ToInt32(args[1]);
                    if (index < 0 || index >= arr.Count) return false;
                    arr.RemoveAt(index);
                    return true;
                }
            },
            
            /// <summary>FIND(arr, value) — возвращает индекс элемента или -1 если не найден</summary>
            { "FIND", args =>
                {
                    var arr = NormalizeArray(args[0]);
                    if (arr == null) return -1;
                    var value = args[1];
                    for (int i = 0; i < arr.Count; i++)
                    {
                        if (Equals(arr[i], value)) return i;
                    }
                    return -1;
                }
            },
            
            /// <summary>SLICE(arr, start, length) — возвращает срез массива (новый массив)</summary>
            { "SLICE", args =>
                {
                    var arr = NormalizeArray(args[0]);
                    if (arr == null) return new List<object?>();
                    var start = Convert.ToInt32(args[1]);
                    var length = args.Length > 2 ? Convert.ToInt32(args[2]) : arr.Count - start;
                    if (start < 0) start = 0;
                    if (length < 0 || start >= arr.Count) return new List<object?>();
                    var result = new List<object?>();
                    for (int i = start; i < Math.Min(start + length, arr.Count); i++)
                        result.Add(arr[i]);
                    return result;
                }
            },
            
            /// <summary>EMPTY() — создаёт пустой массив</summary>
            { "EMPTY", args => new List<object?>() },
            
            /// <summary>RANGE(start, end, step) — создаёт массив чисел от start до end с шагом step</summary>
            { "RANGE", args =>
                {
                    var start = Convert.ToDouble(args[0]);
                    var end = Convert.ToDouble(args[1]);
                    var step = args.Length > 2 ? Convert.ToDouble(args[2]) : 1.0;
                    if (step == 0) throw new Exception("RANGE: step cannot be zero");
                    var result = new List<object?>();
                    bool isIncreasing = step > 0;
                    for (var v = start; isIncreasing ? v <= end : v >= end; v += step)
                        result.Add(v);
                    return result;
                }
            },
            
            /// <summary>INDEXOF(arr, value) — синоним FIND для совместимости</summary>
            { "INDEXOF", args =>
                {
                    var arr = NormalizeArray(args[0]);
                    if (arr == null) return -1;
                    var value = args[1];
                    return arr.IndexOf(value);
                }
            },
            
            /// <summary>CONTAINS(arr, value) — возвращает true если массив содержит элемент</summary>
            { "CONTAINS", args =>
                {
                    var arr = NormalizeArray(args[0]);
                    if (arr == null) return false;
                    return arr.Contains(args[1]);
                }
            },
            
            /// <summary>REVERSE(arr) — возвращает новый массив с элементами в обратном порядке</summary>
            { "REVERSE", args =>
                {
                    var arr = NormalizeArray(args[0]);
                    if (arr == null) return new List<object?>();
                    var result = new List<object?>(arr);
                    result.Reverse();
                    return result;
                }
            },
            
            /// <summary>SUM(arr) — возвращает сумму элементов массива</summary>
            { "SUM", args =>
                {
                    var arr = NormalizeArray(args[0]);
                    if (arr == null) return 0.0;
                    double sum = 0;
                    foreach (var item in arr)
                        sum += Convert.ToDouble(item ?? 0);
                    return sum;
                }
            },
            
            /// <summary>MIN(arr) — возвращает минимальный элемент</summary>
            { "MIN", args =>
                {
                    var arr = NormalizeArray(args[0]);
                    if (arr == null || arr.Count == 0) throw new Exception("MIN: array is empty");
                    double min = Convert.ToDouble(arr[0] ?? 0);
                    foreach (var item in arr.Skip(1))
                    {
                        var val = Convert.ToDouble(item ?? 0);
                        if (val < min) min = val;
                    }
                    return min;
                }
            },
            
            /// <summary>MAX(arr) — возвращает максимальный элемент</summary>
            { "MAX", args =>
                {
                    var arr = NormalizeArray(args[0]);
                    if (arr == null || arr.Count == 0) throw new Exception("MAX: array is empty");
                    double max = Convert.ToDouble(arr[0] ?? 0);
                    foreach (var item in arr.Skip(1))
                    {
                        var val = Convert.ToDouble(item ?? 0);
                        if (val > max) max = val;
                    }
                    return max;
                }
            },
            
            /// <summary>AVERAGE(arr) — возвращает среднее значение элементов</summary>
            { "AVERAGE", args =>
                {
                    var arr = NormalizeArray(args[0]);
                    if (arr == null || arr.Count == 0) return 0.0;
                    double sum = 0;
                    foreach (var item in arr)
                        sum += Convert.ToDouble(item ?? 0);
                    return sum / arr.Count;
                }
            },
        };
    }
}
