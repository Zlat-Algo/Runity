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

public static class Случайности
{
    public static Дробное СлучайноеИзДиапазона(Дробное минимальное, Дробное максимальное)
    {
        return Random.Range(минимальное, максимальное);
    }
    public static Число СлучайноеИзДиапазона(Число минимальное, Число максимальное)
    {
        return Random.Range(минимальное, максимальное + 1);
    }
}
