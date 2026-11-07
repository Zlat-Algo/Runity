#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Поверхность, которая сама везёт стоящие на ней объекты, как лента конвейера или эскалатор
/// </summary>
[RequireComponent(typeof(SurfaceEffector2D))]
[AddComponentMenu("  Runity/ Физика 2D/ Зоны/Конвейер")]
public class Конвейер : Эффектор2D
{
    protected override bool нуженНетвёрдыйКоллайдер => false;
    SurfaceEffector2D лента => (SurfaceEffector2D)оригинальныйКомпонент;

    /// <summary>
    /// Скорость ленты. Отрицательное число везёт в другую сторону
    /// </summary>
    public Дробное скорость { get => лента.speed; set => лента.speed = value; }

    /// <summary>
    /// Случайная добавка к скорости, чтобы объекты ехали неровно
    /// </summary>
    public Дробное разброс { get => лента.speedVariation; set => лента.speedVariation = value; }

    /// <summary>
    /// Насколько сильно лента разгоняет объекты: от 0 до 1
    /// </summary>
    public Дробное сила { get => лента.forceScale; set => лента.forceScale = value; }

    /// <summary>
    /// Меняет направление ленты на противоположное
    /// </summary>
    public void Развернуть() => скорость = -скорость;
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Конвейер))]
internal class КонвейерEditor : Эффектор2DEditor<Конвейер>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект везёт всё, что на нём стоит");

        Пробел();

        ПроверитьКоллайдер();

        Синхрополе("Скорость", x => x.скорость,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Разброс", x => x.разброс,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Сила", x => x.сила,
            (title, value) => EditorGUILayout.Slider(title, value, 0, 1));

        ОбщиеНастройки();
    }
}
#endif
