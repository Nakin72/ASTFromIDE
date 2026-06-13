using System;
using System.Collections.Generic;
namespace UniversalContainers
{
    public enum ValueKind { Number, Bool, String, List, Dictionary }

    public readonly struct UniversalContainer
    {
        public ValueKind Kind { get; }
        private readonly object _value;

        public UniversalContainer(double val) { Kind = ValueKind.Number; _value = val; }
        public UniversalContainer(bool val) { Kind = ValueKind.Bool; _value = val; }
        public UniversalContainer(string val) { Kind = ValueKind.String; _value = val; }
        public UniversalContainer(List<UniversalContainer> val) { Kind = ValueKind.List; _value = val; }
        public UniversalContainer(Dictionary<UniversalContainer, UniversalContainer> val) { Kind = ValueKind.Dictionary; _value = val; }

        // Приведение типов
        public double AsNumber() => Kind == ValueKind.Number ? (double)_value : throw new InvalidCastException();
        public string AsString() => Kind == ValueKind.String ? (string)_value : throw new InvalidCastException();

        // Неявное преобразование для удобства кода
        public static implicit operator UniversalContainer(double v) => new(v);
        public static implicit operator UniversalContainer(string v) => new(v);
        public static implicit operator UniversalContainer(bool v) => new(v);
    }
}