#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Как вращается объект
/// </summary>
public enum РежимВращения { Постоянно, НаУгол }

/// <summary>
/// Вращает объект в 2D или 3D: постоянно с заданной скоростью или плавно на нужный угол с повторами
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Работа с объектами/ Движение/Вращение")]
public class Вращение : ПлавноеИзменение
{
    [SerializeField] РежимВращения _режим;
    /// <summary>
    /// Постоянно - крутится без остановки. На угол - плавно поворачивается на указанный угол и может повторять это
    /// </summary>
    public РежимВращения режим { get => _режим; set => _режим = value; }

    [SerializeField] Флажок _режим2D = да;
    /// <summary>
    /// "Да" для 2D-игр: объект крутится вокруг оси Z, как стрелка часов
    /// </summary>
    public Флажок режим2D { get => _режим2D; set => _режим2D = value; }

    [SerializeField] float _скорость = 45;
    /// <summary>
    /// Сколько градусов в секунду проходит объект в 2D. Отрицательная скорость крутит по часовой стрелке
    /// </summary>
    public float скорость { get => _скорость; set => _скорость = value; }

    [SerializeField] Направление3D _скорость3D = new Vector3(0, 90, 0);
    /// <summary>
    /// Сколько градусов в секунду объект проходит вокруг каждой оси в 3D
    /// </summary>
    public Направление3D скорость3D { get => _скорость3D; set => _скорость3D = value; }

    [SerializeField] float _угол = 90;
    /// <summary>
    /// На сколько градусов повернуться в 2D в режиме «На угол»
    /// </summary>
    public float угол { get => _угол; set => _угол = value; }

    [SerializeField] Направление3D _углы3D = new Vector3(0, 90, 0);
    /// <summary>
    /// На сколько градусов повернуться вокруг каждой оси в 3D в режиме «На угол»
    /// </summary>
    public Направление3D углы3D { get => _углы3D; set => _углы3D = value; }

    [SerializeField] Флажок _вокругСвоихОсей = да;
    /// <summary>
    /// Если "да", то объект крутится вокруг своих осей, наклоняясь вместе с ними. Если "нет", то вокруг осей мира
    /// </summary>
    public Флажок вокругСвоихОсей { get => _вокругСвоихОсей; set => _вокругСвоихОсей = value; }

    Quaternion начало;

    Vector3 углы => режим2D ? new Vector3(0, 0, угол) : (Vector3)углы3D;

    protected override bool бесконечное => режим == РежимВращения.Постоянно;

    protected override void ШагБесконечного(float секунд)
    {
        Vector3 шаг = режим2D ? new Vector3(0, 0, скорость) : (Vector3)скорость3D;
        transform.Rotate(шаг * секунд, вокругСвоихОсей ? Space.Self : Space.World);
    }

    protected override void ЗапомнитьНачало() => начало = transform.localRotation;

    protected override void Применить(float доля)
    {
        Quaternion поворот = Quaternion.Euler(углы * доля);
        transform.localRotation = вокругСвоихОсей ? начало * поворот : поворот * начало;
    }

    protected override void ПеренестиНачалоВКонец()
    {
        Quaternion поворот = Quaternion.Euler(углы);
        начало = вокругСвоихОсей ? начало * поворот : поворот * начало;
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Вращение))]
internal class ВращениеEditor : ПлавноеИзменениеEditor<Вращение>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект вращается: постоянно или плавно на нужный угол");

        Пробел();

        Поле("Режим", x => x.режим,
            (title, value) => (РежимВращения)EditorGUILayout.EnumPopup(title, value));

        Поле("Режим 2D", x => x.режим2D,
            (title, value) => EditorGUILayout.Toggle(title, value));

        if (компонент.режим == РежимВращения.Постоянно)
        {
            if (компонент.режим2D)
                Поле("Скорость", x => x.скорость, (title, value) => EditorGUILayout.FloatField(title, value));
            else
                Поле("Скорость", x => x.скорость3D, (title, value) => EditorGUILayout.Vector3Field(title, value));
        }
        else
        {
            if (компонент.режим2D)
                Поле("Угол", x => x.угол, (title, value) => EditorGUILayout.FloatField(title, value));
            else
                Поле("Углы", x => x.углы3D, (title, value) => EditorGUILayout.Vector3Field(title, value));
        }

        if (!компонент.режим2D)
        {
            Поле("Вокруг своих осей", x => x.вокругСвоихОсей,
                (title, value) => EditorGUILayout.Toggle(title, value));
        }

        Пробел();

        if (компонент.режим == РежимВращения.Постоянно) ОбщиеНастройкиЗапуска();
        else ОбщиеНастройки();

        Пробел();

        КнопкиЗапуска();
        ОбщиеСобытия();
    }
}
#endif
