#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Какой формы свет
/// </summary>
public enum ТипСвета { Точечный, Направленный, Прожектор }

/// <summary>
/// Источник света в 3D: лампочка, солнце или фонарик. Может мерцать, как факел
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Light))]
[AddComponentMenu("  Runity/ Визуал/ 3D/Свет")]
public class Свет : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<Light>();
    void OnValidate() => Awake();
    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public Light оригинал => (Light)оригинальныйКомпонент;

    /// <summary>
    /// Точечный светит во все стороны, Направленный светит как солнце на всю сцену, Прожектор светит конусом
    /// </summary>
    public ТипСвета тип
    {
        get => оригинал.type switch
        {
            LightType.Directional => ТипСвета.Направленный,
            LightType.Spot => ТипСвета.Прожектор,
            _ => ТипСвета.Точечный
        };
        set => оригинал.type = value switch
        {
            ТипСвета.Направленный => LightType.Directional,
            ТипСвета.Прожектор => LightType.Spot,
            _ => LightType.Point
        };
    }

    /// <summary>
    /// Цвет света
    /// </summary>
    public Цвет цвет
    {
        get => оригинал.color;
        set => оригинал.color = value;
    }

    /// <summary>
    /// Насколько ярко светит
    /// </summary>
    public Дробное яркость
    {
        get => оригинал.intensity;
        set => оригинал.intensity = value;
    }

    /// <summary>
    /// Как далеко достаёт свет. Не влияет на Направленный свет
    /// </summary>
    public Дробное дальность
    {
        get => оригинал.range;
        set => оригинал.range = value;
    }

    /// <summary>
    /// Ширина конуса Прожектора в градусах
    /// </summary>
    public Дробное уголПрожектора
    {
        get => оригинал.spotAngle;
        set => оригинал.spotAngle = value;
    }

    /// <summary>
    /// Отбрасывают ли объекты тени от этого света
    /// </summary>
    public Флажок тени
    {
        get => оригинал.shadows != LightShadows.None;
        set => оригинал.shadows = value ? LightShadows.Soft : LightShadows.None;
    }

    [SerializeField] Флажок _мерцание = нет;
    /// <summary>
    /// Если "да", то яркость случайно дрожит, как у костра или старой лампы
    /// </summary>
    public Флажок мерцание { get => _мерцание; set => _мерцание = value; }

    [SerializeField] Дробное _силаМерцания = 0.3;
    /// <summary>
    /// Насколько сильно меняется яркость при мерцании: от 0 до 1
    /// </summary>
    public Дробное силаМерцания { get => _силаМерцания; set => _силаМерцания = value; }

    [SerializeField] Дробное _скоростьМерцания = 8;
    /// <summary>
    /// Как быстро дрожит яркость
    /// </summary>
    public Дробное скоростьМерцания { get => _скоростьМерцания; set => _скоростьМерцания = value; }

    /// <summary>
    /// Даёт "да", если свет включён
    /// </summary>
    public Флажок включён => оригинал.enabled;

    float исходнаяЯркость;
    float случайноеНачало;

    void Start()
    {
        исходнаяЯркость = яркость;
        случайноеНачало = Random.value * 100;
    }

    void Update()
    {
        if (!мерцание) return;

        float шум = Mathf.PerlinNoise(случайноеНачало, Время.времяИгры * скоростьМерцания);
        оригинал.intensity = исходнаяЯркость * (1 - силаМерцания * шум);
    }

    /// <summary>
    /// Включает свет
    /// </summary>
    public void Включить() => оригинал.enabled = да;

    /// <summary>
    /// Выключает свет
    /// </summary>
    public void Выключить() => оригинал.enabled = нет;

    /// <summary>
    /// Выключает свет, если он горит, и включает, если не горит
    /// </summary>
    public void Переключить() => оригинал.enabled = !оригинал.enabled;
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Свет))]
internal class СветEditor : RunityEditor<Свет>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект освещает 3D-сцену");

        Пробел();

        EnumField("Тип", x => x.тип, (component, value) => component.тип = value);

        Синхрополе("Цвет", x => x.цвет,
            (title, value) => EditorGUILayout.ColorField(title, value));

        Синхрополе("Яркость", x => x.яркость,
            (title, value) => EditorGUILayout.FloatField(title, value));

        if (компонент.тип != ТипСвета.Направленный)
        {
            Синхрополе("Дальность", x => x.дальность,
                (title, value) => EditorGUILayout.FloatField(title, value));
        }

        if (компонент.тип == ТипСвета.Прожектор)
        {
            Синхрополе("Угол прожектора", x => x.уголПрожектора,
                (title, value) => EditorGUILayout.Slider(title, value, 1, 179));
        }

        Синхрополе("Тени", x => x.тени,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        Поле("Мерцание", x => x.мерцание,
            (title, value) => EditorGUILayout.Toggle(title, value));

        if (компонент.мерцание)
        {
            Поле("Сила мерцания", x => x.силаМерцания,
                (title, value) => EditorGUILayout.Slider(title, value, 0, 1));

            Поле("Скорость мерцания", x => x.скоростьМерцания,
                (title, value) => EditorGUILayout.FloatField(title, value));
        }
    }
}
#endif
