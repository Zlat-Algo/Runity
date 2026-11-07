using UnityEngine;

/// <summary>
/// Печатает сообщения в окно Console
/// </summary>
public static class Консоль
{
    /// <summary>
    /// Печатает обычное сообщение
    /// </summary>
    public static void Напечатать(object сообщение) => Debug.Log(сообщение);
    /// <summary>
    /// Печатает обычное сообщение
    /// </summary>
    public static void Напечатать(object сообщение, Object объект) => Debug.Log(сообщение, объект);
    /// <summary>
    /// Печатает жёлтое предупреждение
    /// </summary>
    public static void НапечататьПредупреждение(object сообщение) => Debug.LogWarning(сообщение);
    /// <summary>
    /// Печатает жёлтое предупреждение
    /// </summary>
    public static void НапечататьПредупреждение(object сообщение, Object объект) => Debug.LogWarning(сообщение, объект);
    /// <summary>
    /// Печатает красную ошибку
    /// </summary>
    public static void НапечататьОшибку(object сообщение) => Debug.LogError(сообщение);
    /// <summary>
    /// Печатает красную ошибку
    /// </summary>
    public static void НапечататьОшибку(object сообщение, Object объект) => Debug.LogError(сообщение, объект);
}
