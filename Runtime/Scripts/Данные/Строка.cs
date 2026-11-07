using System;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Текстовые данные. Альтернатива string
/// </summary>
[Serializable]
public struct Строка : IEquatable<Строка>
{
    [SerializeField] string значение;

    /// <summary>
    /// Количество символов в строке
    /// </summary>
    public Число длина => значение.Length;

    public Строка(string число)
    {
        значение = число;
    }

    public static implicit operator string(Строка строка)
        => строка.значение;

    public static implicit operator Строка(string стринг)
        => new Строка(стринг);

    public static Строка operator +(Строка первое, Дробное второе)
        => первое.значение + второе.ToString();

    public static Строка operator +(Строка первое, Число второе)
        => первое.значение + второе.ToString();

    public static Строка operator *(Строка первое, Число второе)
    {
        Строка результат = "";
        for (int i = 0; i < второе; i++)
        {
            результат += первое;
        }
        return результат;
    }

    public static Флажок operator ==(Строка первое, Строка второе)
        => первое.значение == второе.значение;

    public static Флажок operator !=(Строка первое, Строка второе)
        => первое.значение != второе.значение;

    public bool Equals(Строка другое)
        => значение == другое.значение;

    public override bool Equals(object obj)
        => obj is Строка другое && Equals(другое);

    public override int GetHashCode()
        => значение.GetHashCode();

    public override string ToString()
        => значение;

    public Флажок кириллица => Regex.IsMatch(значение, @"\p{IsCyrillic}");

    public Флажок чтоТоЕсть => !string.IsNullOrEmpty(значение);

    public Флажок Совпадает(params Строка[] строки)
    {
        foreach(Строка строка in строки)
        {
            if (значение == строка) return true;
        }
        return false;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(Строка))] internal class СтрокаDrawer : ПолеDrawer { }
#endif