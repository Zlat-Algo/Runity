#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Какой формы 2D-свет
/// </summary>
public enum ТипСвета2D { Точечный, Глобальный, ПоФормеСпрайта, Произвольный }

/// <summary>
/// Источник света в 2D. Работает, если в проекте настроен 2D Renderer
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Light2D))]
[AddComponentMenu("  Runity/ Визуал/ 2D/Свет 2D")]
public class Свет2D : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<Light2D>();
    void OnValidate() => Awake();
    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public Light2D оригинал => (Light2D)оригинальныйКомпонент;

    /// <summary>
    /// Точечный светит кругом или конусом, Глобальный освещает всю сцену, По форме спрайта повторяет картинку
    /// </summary>
    public ТипСвета2D тип
    {
        get => оригинал.lightType switch
        {
            Light2D.LightType.Global => ТипСвета2D.Глобальный,
            Light2D.LightType.Sprite => ТипСвета2D.ПоФормеСпрайта,
            Light2D.LightType.Freeform => ТипСвета2D.Произвольный,
            _ => ТипСвета2D.Точечный
        };
        set => оригинал.lightType = value switch
        {
            ТипСвета2D.Глобальный => Light2D.LightType.Global,
            ТипСвета2D.ПоФормеСпрайта => Light2D.LightType.Sprite,
            ТипСвета2D.Произвольный => Light2D.LightType.Freeform,
            _ => Light2D.LightType.Point
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
    /// Как далеко достаёт Точечный свет
    /// </summary>
    public Дробное радиус
    {
        get => оригинал.pointLightOuterRadius;
        set => оригинал.pointLightOuterRadius = value;
    }

    /// <summary>
    /// Внутри этого радиуса Точечный свет светит в полную силу
    /// </summary>
    public Дробное внутреннийРадиус
    {
        get => оригинал.pointLightInnerRadius;
        set => оригинал.pointLightInnerRadius = value;
    }

    /// <summary>
    /// Ширина луча Точечного света в градусах. 360 означает круг
    /// </summary>
    public Дробное угол
    {
        get => оригинал.pointLightOuterAngle;
        set
        {
            оригинал.pointLightOuterAngle = value;
            оригинал.pointLightInnerAngle = Mathf.Min(оригинал.pointLightInnerAngle, value);
        }
    }

    /// <summary>
    /// Насколько плавно свет гаснет к краю: от 0 до 1
    /// </summary>
    public Дробное мягкость
    {
        get => оригинал.falloffIntensity;
        set => оригинал.falloffIntensity = value;
    }

    /// <summary>
    /// Отбрасывают ли тени объекты с компонентом Shadow Caster 2D
    /// </summary>
    public Флажок тени
    {
        get => оригинал.shadowsEnabled;
        set => оригинал.shadowsEnabled = value;
    }

    [SerializeField] Флажок _мерцание = нет;
    /// <summary>
    /// Если "да", то яркость случайно дрожит, как у костра
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
[CustomEditor(typeof(Свет2D))]
internal class Свет2DEditor : RunityEditor<Свет2D>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект освещает 2D-сцену. Свет виден, только если в настройках графики выбран 2D Renderer. Когда на сцене есть хотя бы один 2D-свет, всё остальное становится темнее, поэтому обычно добавляют ещё и Глобальный свет");

        Пробел();

        EnumField("Тип", x => x.тип, (component, value) => component.тип = value);

        Синхрополе("Цвет", x => x.цвет,
            (title, value) => EditorGUILayout.ColorField(title, value));

        Синхрополе("Яркость", x => x.яркость,
            (title, value) => EditorGUILayout.FloatField(title, value));

        if (компонент.тип == ТипСвета2D.Точечный)
        {
            Синхрополе("Радиус", x => x.радиус,
                (title, value) => EditorGUILayout.FloatField(title, value));

            Синхрополе("Внутренний радиус", x => x.внутреннийРадиус,
                (title, value) => EditorGUILayout.FloatField(title, value));

            Синхрополе("Угол", x => x.угол,
                (title, value) => EditorGUILayout.Slider(title, value, 1, 360));
        }

        if (компонент.тип != ТипСвета2D.Глобальный)
        {
            Синхрополе("Мягкость", x => x.мягкость,
                (title, value) => EditorGUILayout.Slider(title, value, 0, 1));

            Синхрополе("Тени", x => x.тени,
                (title, value) => EditorGUILayout.Toggle(title, value));
        }

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
