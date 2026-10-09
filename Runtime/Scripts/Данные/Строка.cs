using Codice.Client.BaseCommands;
using System;
using UnityEditor;

/// <summary>
/// Текстовые данные. Альтернатива string
/// </summary>
[Serializable]
public struct Строка : IEquatable<Строка>
{
    string значение;

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
        => первое + второе;

    public static Строка operator +(Строка первое, Число второе)
        => первое + второе;

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
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(Строка))] internal class СтрокаDrawer : ПолеDrawer { }
#endif