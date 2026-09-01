using UnityEngine;

[System.Serializable]
public struct Диапазон
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
}

public static class Случайности
{
    public static float СлучайноеИзДиапазона(float минимальное, float максимальное)
    {
        return Random.Range(минимальное, максимальное);
    }
    public static int СлучайноеИзДиапазона(int минимальное, int максимальное)
    {
        return Random.Range(минимальное, максимальное + 1);
    }
}
