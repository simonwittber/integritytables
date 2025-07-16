using System;
using System.Collections.Generic;

namespace IntegrityTables;

public class ObservableProperty<T>(T initialValue = default!)
{
    private T _value = initialValue;
    public event Action<T, T>? OnChanged;

    public T Value
    {
        get => _value;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(_value, value))
            {
                var oldValue = _value;
                _value = value;
                OnChanged?.Invoke(oldValue, _value);
            }
        }
    }

    public static implicit operator T(ObservableProperty<T> prop) => prop._value;
}