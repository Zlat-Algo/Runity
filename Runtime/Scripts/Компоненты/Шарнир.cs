#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Соединяет объекты так, что они вращаются вокруг одной оси: дверь, маятник, колесо
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(HingeJoint))]
[AddComponentMenu("  Runity/ Физика 3D/ Соединения/Шарнир")]
public class Шарнир : Соединение3D
{
    HingeJoint шарнир => (HingeJoint)оригинальныйКомпонент;

    /// <summary>
    /// Вокруг какой оси вращается объект. Для двери это (0, 1, 0)
    /// </summary>
    public Направление3D ось { get => шарнир.axis; set => шарнир.axis = value; }

    /// <summary>
    /// Если "да", то шарнир сам крутится, как колесо машины
    /// </summary>
    public Флажок мотор { get => шарнир.useMotor; set => шарнир.useMotor = value; }

    /// <summary>
    /// Скорость вращения мотора в градусах в секунду
    /// </summary>
    public Дробное скоростьМотора
    {
        get => шарнир.motor.targetVelocity;
        set { JointMotor м = шарнир.motor; м.targetVelocity = value; шарнир.motor = м; }
    }

    /// <summary>
    /// Насколько сильный мотор: чем больше, тем тяжелее груз он может крутить
    /// </summary>
    public Дробное силаМотора
    {
        get => шарнир.motor.force;
        set { JointMotor м = шарнир.motor; м.force = value; шарнир.motor = м; }
    }

    /// <summary>
    /// Если "да", то поворот ограничен минимальным и максимальным углом. Подходит для дверей
    /// </summary>
    public Флажок ограничитьУгол { get => шарнир.useLimits; set => шарнир.useLimits = value; }

    /// <summary>
    /// На сколько градусов можно повернуть в одну сторону
    /// </summary>
    public Дробное минимальныйУгол
    {
        get => шарнир.limits.min;
        set { JointLimits п = шарнир.limits; п.min = value; шарнир.limits = п; }
    }

    /// <summary>
    /// На сколько градусов можно повернуть в другую сторону
    /// </summary>
    public Дробное максимальныйУгол
    {
        get => шарнир.limits.max;
        set { JointLimits п = шарнир.limits; п.max = value; шарнир.limits = п; }
    }

    /// <summary>
    /// Текущий угол поворота шарнира
    /// </summary>
    public Дробное текущийУгол => шарнир.angle;
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Шарнир))]
internal class ШарнирEditor : Соединение3DEditor<Шарнир>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект вращается вокруг оси в точке крепления");

        Пробел();

        ОбщиеНастройки();

        Синхрополе("Ось", x => x.ось,
            (title, value) => EditorGUILayout.Vector3Field(title, value));

        Пробел();

        Синхрополе("Мотор", x => x.мотор,
            (title, value) => EditorGUILayout.Toggle(title, value));

        if (компонент.мотор)
        {
            Синхрополе("Скорость мотора", x => x.скоростьМотора,
                (title, value) => EditorGUILayout.FloatField(title, value));

            Синхрополе("Сила мотора", x => x.силаМотора,
                (title, value) => EditorGUILayout.FloatField(title, value));
        }

        Синхрополе("Ограничить угол", x => x.ограничитьУгол,
            (title, value) => EditorGUILayout.Toggle(title, value));

        if (компонент.ограничитьУгол)
        {
            Синхрополе("Минимальный угол", x => x.минимальныйУгол,
                (title, value) => EditorGUILayout.FloatField(title, value));

            Синхрополе("Максимальный угол", x => x.максимальныйУгол,
                (title, value) => EditorGUILayout.FloatField(title, value));
        }

        Пробел();

        ОбщиеСобытия();
    }
}
#endif
