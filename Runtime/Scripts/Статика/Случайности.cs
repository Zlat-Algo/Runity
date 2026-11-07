using UnityEngine;

/*[System.Serializable]
internal struct Диапазон
{
    public float минимальное;
    public float максимальное;
    public bool _диапазон;
    public bool диапазон
    {
        get => _диапазон;
        set => _диапазон = value;
    }

    public float ПолучитьЗначение()
    {
        if (диапазон)
        {
            return Случайности.СлучайноеИзДиапазона(минимальное, максимальное);
        }
        else
        {
            return минимальное;
        }
    }
}*/

/// <summary>
/// Случайные значения
/// </summary>
public static class Случайности
{
    /// <summary>
    /// Даёт случайное значение от минимального до максимального
    /// </summary>
    public static Дробное СлучайноеИзДиапазона(Дробное минимальное, Дробное максимальное)
    {
        return Random.Range(минимальное, максимальное);
    }
    /// <summary>
    /// Даёт случайное значение от минимального до максимального
    /// </summary>
    public static Число СлучайноеИзДиапазона(Число минимальное, Число максимальное)
    {
        return Random.Range(минимальное, максимальное + 1);
    }
}
