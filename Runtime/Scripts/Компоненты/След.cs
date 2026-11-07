#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Оставляет за движущимся объектом тающий хвост: подходит для пуль, мечей и комет
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TrailRenderer))]
[AddComponentMenu("  Runity/ Эффекты/След")]
public class След : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<TrailRenderer>();

    void OnValidate()
    {
        Awake();
        МатериалЛиний.ПроверитьМатериал(оригинал);
    }

    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public TrailRenderer оригинал => (TrailRenderer)оригинальныйКомпонент;

    /// <summary>
    /// Сколько секунд держится след
    /// </summary>
    public Дробное длительность
    {
        get => оригинал.time;
        set => оригинал.time = value;
    }

    /// <summary>
    /// Толщина следа у объекта
    /// </summary>
    public Дробное толщина
    {
        get => оригинал.widthMultiplier;
        set => оригинал.widthMultiplier = value;
    }

    /// <summary>
    /// Если "да", то след сужается к хвосту
    /// </summary>
    public Флажок сужается
    {
        get => оригинал.endWidth < оригинал.startWidth;
        set
        {
            оригинал.startWidth = 1;
            оригинал.endWidth = value ? 0 : 1;
        }
    }

    /// <summary>
    /// Цвет следа у объекта
    /// </summary>
    public Цвет цветНачала
    {
        get => оригинал.startColor;
        set => оригинал.startColor = value;
    }

    /// <summary>
    /// Цвет на конце следа. Если сделать его прозрачным, след будет плавно таять
    /// </summary>
    public Цвет цветКонца
    {
        get => оригинал.endColor;
        set => оригинал.endColor = value;
    }

    /// <summary>
    /// Пока "да", объект оставляет след
    /// </summary>
    public Флажок рисует
    {
        get => оригинал.emitting;
        set => оригинал.emitting = value;
    }

    /// <summary>
    /// Сразу стирает весь след. Удобно после телепортации
    /// </summary>
    public void Стереть() => оригинал.Clear();
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(След))]
internal class СледEditor : RunityEditor<След>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект оставляет за собой след, когда движется");

        Пробел();

        Синхрополе("Длительность", x => x.длительность,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Толщина", x => x.толщина,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Сужается", x => x.сужается,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Цвет начала", x => x.цветНачала,
            (title, value) => EditorGUILayout.ColorField(title, value));

        Синхрополе("Цвет конца", x => x.цветКонца,
            (title, value) => EditorGUILayout.ColorField(title, value));

        Синхрополе("Рисует", x => x.рисует,
            (title, value) => EditorGUILayout.Toggle(title, value));
    }
}
#endif
