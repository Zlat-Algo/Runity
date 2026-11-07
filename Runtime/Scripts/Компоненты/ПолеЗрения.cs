using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Замечает объекты, которые попали в конус перед этим объектом, и запускает события
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Механики/Поле зрения")]
public class ПолеЗрения : RunityComponent
{
    protected override void OnEnable() { }
    protected override void OnDisable() => видимые.Clear();

    [SerializeField] Флажок _режим2D = да;
    /// <summary>
    /// "Да" для 2D-игр. В 3D объект смотрит вдоль своей синей оси
    /// </summary>
    public Флажок режим2D { get => _режим2D; set => _режим2D = value; }

    [SerializeField] Дробное _дальность = 5;
    /// <summary>
    /// Как далеко видит объект
    /// </summary>
    public Дробное дальность { get => _дальность; set => _дальность = value; }

    [SerializeField] Дробное _угол = 90;
    /// <summary>
    /// Ширина конуса в градусах. 360 означает, что объект видит вокруг себя
    /// </summary>
    public Дробное угол { get => _угол; set => _угол = value; }

    [SerializeField] НаправлениеВзгляда2D _взгляд2D;
    /// <summary>
    /// Куда смотрит объект в 2D, если его не поворачивали
    /// </summary>
    public НаправлениеВзгляда2D взгляд2D { get => _взгляд2D; set => _взгляд2D = value; }

    [SerializeField] Флажок _учитыватьОтзеркаливание = да;
    /// <summary>
    /// Если "да", то отзеркаленный Спрайтер смотрит в другую сторону
    /// </summary>
    public Флажок учитыватьОтзеркаливание { get => _учитыватьОтзеркаливание; set => _учитыватьОтзеркаливание = value; }

    [SerializeField, HideInInspector] Строка _тег;

    [SerializeField] Теги _теги = new Теги();
    /// <summary>
    /// Объект замечает только объекты с этими тегами. Если теги не выбраны, то любые объекты
    /// </summary>
    public Теги теги => _теги;

    [SerializeField] LayerMask _слоиЦелей = ~0;
    /// <summary>
    /// На каких физических слоях искать цели
    /// </summary>
    public LayerMask слоиЦелей { get => _слоиЦелей; set => _слоиЦелей = value; }

    [SerializeField] Флажок _препятствияМешают = да;
    /// <summary>
    /// Если "да", то объект не видит сквозь твёрдые коллайдеры
    /// </summary>
    public Флажок препятствияМешают { get => _препятствияМешают; set => _препятствияМешают = value; }

    [SerializeField] LayerMask _слоиПрепятствий = ~0;
    /// <summary>
    /// Какие физические слои считаются препятствиями
    /// </summary>
    public LayerMask слоиПрепятствий { get => _слоиПрепятствий; set => _слоиПрепятствий = value; }

    [SerializeField] Дробное _времяМеждуПроверками = 0.1;
    /// <summary>
    /// Как часто объект осматривается, в секундах. Чем больше, тем меньше нагрузка на игру
    /// </summary>
    public Дробное времяМеждуПроверками { get => _времяМеждуПроверками; set => _времяМеждуПроверками = value; }

    [SerializeField] События _когдаЗаметил;
    /// <summary>
    /// Срабатывает для каждого нового объекта, попавшего в поле зрения
    /// </summary>
    public События когдаЗаметил => _когдаЗаметил;

    [SerializeField] События _когдаПотерял;
    /// <summary>
    /// Срабатывает для каждого объекта, который пропал из поля зрения
    /// </summary>
    public События когдаПотерял => _когдаПотерял;

    [SerializeField] События _покаВидит;
    /// <summary>
    /// Срабатывает при каждой проверке, пока кто-то виден
    /// </summary>
    public События покаВидит => _покаВидит;

    [SerializeField] События _когдаВсеПропали;
    /// <summary>
    /// Срабатывает, когда в поле зрения не осталось ни одного объекта
    /// </summary>
    public События когдаВсеПропали => _когдаВсеПропали;

    readonly List<GameObject> видимые = new List<GameObject>();
    float таймер;

    /// <summary>
    /// Все объекты, которые видны прямо сейчас
    /// </summary>
    public IReadOnlyList<GameObject> видимыеОбъекты => видимые;

    /// <summary>
    /// Сколько объектов видно прямо сейчас
    /// </summary>
    public Число количествоВидимых => видимые.Count;

    /// <summary>
    /// Даёт "да", если виден хотя бы один объект
    /// </summary>
    public Флажок ктоТоВиден => видимые.Count > 0;

    /// <summary>
    /// Последний замеченный объект. Пока никого не замечали, здесь пусто
    /// </summary>
    public GameObject последнийЗамеченный { get; private set; }

    /// <summary>
    /// Последний потерянный объект
    /// </summary>
    public GameObject последнийПотерянный { get; private set; }

    /// <summary>
    /// Самый близкий из видимых объектов. Если никого не видно, здесь пусто
    /// </summary>
    public GameObject ближайший
    {
        get
        {
            GameObject результат = null;
            float лучшееРасстояние = float.MaxValue;

            foreach (GameObject объект in видимые)
            {
                if (объект == null) continue;

                float расстояние = (объект.transform.position - transform.position).sqrMagnitude;
                if (расстояние < лучшееРасстояние)
                {
                    лучшееРасстояние = расстояние;
                    результат = объект;
                }
            }

            return результат;
        }
    }

    /// <summary>
    /// Направление взгляда прямо сейчас
    /// </summary>
    public Направление3D взгляд => ПоискОбъектов.Взгляд(transform, режим2D, взгляд2D, учитыватьОтзеркаливание);

    /// <summary>
    /// Даёт "да", если этот объект виден прямо сейчас
    /// </summary>
    public Флажок Видит(Объект объект) => видимые.Contains(объект);

    /// <summary>
    /// Осматривается вокруг, не дожидаясь следующей проверки
    /// </summary>
    public void ПроверитьСейчас()
    {
        ПоискОбъектов.ПеренестиТег(ref _тег, _теги);
        Vector3 мойВзгляд = взгляд;
        List<GameObject> сейчас = ПоискОбъектов.ВРадиусе(transform.position, дальность, режим2D, слоиЦелей);

        сейчас.RemoveAll(объект =>
            ПоискОбъектов.Свой(объект.transform, transform) ||
            !_теги.Подходит(объект) ||
            !ПоискОбъектов.ВСекторе(transform.position, мойВзгляд, объект.transform.position, угол, режим2D) ||
            (препятствияМешают && !ПоискОбъектов.ПутьСвободен(transform, объект.transform, режим2D, слоиПрепятствий)));

        bool ктоТоБылВиден = видимые.Count > 0;

        for (int i = видимые.Count - 1; i >= 0; i--)
        {
            if (видимые[i] == null || !сейчас.Contains(видимые[i]))
            {
                последнийПотерянный = видимые[i];
                видимые.RemoveAt(i);
                _когдаПотерял.Вызвать();
            }
        }

        foreach (GameObject объект in сейчас)
        {
            if (!видимые.Contains(объект))
            {
                видимые.Add(объект);
                последнийЗамеченный = объект;
                _когдаЗаметил.Вызвать();
            }
        }

        if (видимые.Count > 0)
        {
            _покаВидит.Вызвать();
        }
        else if (ктоТоБылВиден)
        {
            _когдаВсеПропали.Вызвать();
        }
    }

    void Update()
    {
        таймер -= Время.времяСПрошлогоКадра;
        if (таймер <= 0)
        {
            таймер = времяМеждуПроверками;
            ПроверитьСейчас();
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector3 мойВзгляд = взгляд;
        Vector3 осьПоворота = режим2D ? Vector3.forward : transform.up;
        int кусочков = 24;
        float половина = Mathf.Min(угол, 360) / 2;
        Vector3 прошлаяТочка = transform.position + Quaternion.AngleAxis(-половина, осьПоворота) * мойВзгляд * дальность;

        Gizmos.color = видимые.Count > 0 ? Color.red : Color.yellow;
        if (угол < 360) Gizmos.DrawLine(transform.position, прошлаяТочка);

        for (int i = 1; i <= кусочков; i++)
        {
            float текущийУгол = Mathf.Lerp(-половина, половина, (float)i / кусочков);
            Vector3 точка = transform.position + Quaternion.AngleAxis(текущийУгол, осьПоворота) * мойВзгляд * дальность;
            Gizmos.DrawLine(прошлаяТочка, точка);
            прошлаяТочка = точка;
        }

        if (угол < 360) Gizmos.DrawLine(transform.position, прошлаяТочка);

        foreach (GameObject объект in видимые)
        {
            if (объект != null) Gizmos.DrawLine(transform.position, объект.transform.position);
        }
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(ПолеЗрения))]
internal class ПолеЗренияEditor : RunityEditor<ПолеЗрения>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект замечает других и что-то делает, когда они попадают в поле зрения или пропадают из него. У целей должны быть коллайдеры. Конус виден в сцене, когда объект выделен");

        Пробел();

        Поле("Режим 2D", x => x.режим2D,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Поле("Дальность", x => x.дальность,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Поле("Угол", x => x.угол,
            (title, value) => EditorGUILayout.Slider(title, value, 1, 360));

        if (компонент.режим2D)
        {
            Поле("Куда смотрит", x => x.взгляд2D,
                (title, value) => (НаправлениеВзгляда2D)EditorGUILayout.EnumPopup(title, value));

            Поле("Учитывать отзеркаливание", x => x.учитыватьОтзеркаливание,
                (title, value) => EditorGUILayout.Toggle(title, value));
        }

        Пробел();

        ОтобразитьТеги("Теги целей", "_теги");

        Поле("Слои целей", x => x.слоиЦелей, ПолеСлоёв);

        Поле("Препятствия мешают", x => x.препятствияМешают,
            (title, value) => EditorGUILayout.Toggle(title, value));

        if (компонент.препятствияМешают)
        {
            Поле("Слои препятствий", x => x.слоиПрепятствий, ПолеСлоёв);
        }

        Поле("Время между проверками", x => x.времяМеждуПроверками,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("Когда заметил", "_когдаЗаметил");
        ОтобразитьСобытие("Когда потерял", "_когдаПотерял");
        ОтобразитьСобытие("Пока видит", "_покаВидит");
        ОтобразитьСобытие("Когда все пропали", "_когдаВсеПропали");
        ЗакончитьСобытия();
    }
}
#endif
