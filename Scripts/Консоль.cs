using UnityEngine;

public static class Консоль
{
    public static void Напечатать(object сообщение) => Debug.Log(сообщение);
    public static void Напечатать(object сообщение, Object объект) => Debug.Log(сообщение, объект);
    public static void НапечататьПредупреждение(object сообщение) => Debug.LogWarning(сообщение);
    public static void НапечататьПредупреждение(object сообщение, Object объект) => Debug.LogWarning(сообщение, объект);
    public static void НапечататьОшибку(object сообщение) => Debug.LogError(сообщение);
    public static void НапечататьОшибку(object сообщение, Object объект) => Debug.LogError(сообщение, объект);
}
