#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Соединяет объекты так, что они могут вращаться вокруг общей точки: дверь, маятник, колесо
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(HingeJoint2D))]
[AddComponentMenu("  Runity/ Физика 2D/ Соединения/Шарнир 2D")]
public class Шарнир2D : Соединение2D
{
    HingeJoint2D шарнир => (HingeJoint2D)оригинальныйКомпонент;

    /// <summary>
    /// Если "да", то шарнир сам крутится, как колесо машины
    /// </summary>
    public Флажок мотор { get => шарнир.useMotor; set => шарнир.useMotor = value; }

    /// <summary>
    /// Скорость вращения мотора в градусах в секунду
    /// </summary>
    public Дробное скоростьМотора
    {
        get => шарнир.motor.motorSpeed;
        set { JointMotor2D м = шарнир.motor; м.motorSpeed = value; шарнир.motor = м; }
    }

    /// <summary>
    /// Насколько сильный мотор: чем больше, тем тяжелее груз он может крутить
    /// </summary>
    public Дробное силаМотора
    {
        get => шарнир.motor.maxMotorTorque;
        set { JointMotor2D м = шарнир.motor; м.maxMotorTorque = value; шарнир.motor = м; }
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
        set { JointAngleLimits2D п = шарнир.limits; п.min = value; шарнир.limits = п; }
    }

    /// <summary>
    /// На сколько градусов можно повернуть в другую сторону
    /// </summary>
    public Дробное максимальныйУгол
    {
        get => шарнир.limits.max;
        set { JointAngleLimits2D п = шарнир.limits; п.max = value; шарнир.limits = п; }
    }

    /// <summary>
    /// Текущий угол поворота шарнира
    /// </summary>
    public Дробное текущийУгол => шарнир.jointAngle;
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Шарнир2D))]
internal class Шарнир2DEditor : Соединение2DEditor<Шарнир2D>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект вращается вокруг точки крепления. Если ни с чем не связать, он будет висеть на гвозде в мире");

        Пробел();

        ОбщиеНастройки();

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
