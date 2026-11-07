using UnityEngine;

/// <summary>
/// Всё, что связано со временем в игре
/// </summary>
public static class Время
{
    /// <summary>
    /// Скорость времени: 1 - обычная, 0 - пауза, 2 - в два раза быстрее
    /// </summary>
    public static float скоростьИгры
    {
        get => Time.timeScale;
        set => Time.timeScale = value;
    }
    /// <summary>
    /// Сколько секунд прошло с прошлого кадра
    /// </summary>
    public static float времяСПрошлогоКадра => Time.deltaTime;
    /// <summary>
    /// Сколько секунд прошло с прошлого кадра, если не учитывать скорость игры
    /// </summary>
    public static float времяСПрошлогоКадраБезУчётаСкорости => Time.unscaledDeltaTime;
    /// <summary>
    /// Сколько кадров в секунду показывает игра
    /// </summary>
    public static float FPS => 1 / Time.deltaTime;
    /// <summary>
    /// Сколько кадров в секунду показывает игра
    /// </summary>
    public static float КВС => FPS;
    /// <summary>
    /// Сколько кадров в секунду показывает игра
    /// </summary>
    public static float ФПС => FPS;
    /// <summary>
    /// Сколько секунд проходит между физическими кадрами
    /// </summary>
    public static float времяМеждуFixedUpdate => Time.fixedDeltaTime;
    /// <summary>
    /// Сколько секунд проходит между физическими кадрами, если не учитывать скорость игры
    /// </summary>
    public static float времяМеждуFixedUpdateБезУчётаСкорости => Time.fixedUnscaledDeltaTime;
    /// <summary>
    /// Сколько секунд прошло с начала игры
    /// </summary>
    public static float времяИгры => Time.time;
    /// <summary>
    /// Сколько секунд прошло с начала игры, если не учитывать скорость игры
    /// </summary>
    public static float времяИгрыБезУчётаСкорости => Time.fixedUnscaledTime;
}
