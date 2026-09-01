using UnityEngine;

public static class Время
{
    public static float скоростьИгры
    {
        get => Time.timeScale;
        set => Time.timeScale = value;
    }
    public static float времяСПрошлогоКадра => Time.deltaTime;
    public static float времяСПрошлогоКадраБезУчётаСкорости => Time.unscaledDeltaTime;
    public static float FPS => 1 / Time.deltaTime;
    public static float КВС => FPS;
    public static float ФПС => FPS;
    public static float времяМеждуFixedUpdate => Time.fixedDeltaTime;
    public static float времяМеждуFixedUpdateБезУчётаСкорости => Time.fixedUnscaledDeltaTime;
    public static float времяИгры => Time.time;
    public static float времяИгрыБезУчётаСкорости => Time.fixedUnscaledTime;
}
