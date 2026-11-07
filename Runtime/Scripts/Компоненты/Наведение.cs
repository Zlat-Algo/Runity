#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// На что поворачивается объект
/// </summary>
public enum ЦельНаведения { Объект, Мышка, Замеченный }

/// <summary>
/// Поворачивает объект к цели или к курсору: башня, глаз, оружие в руке
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Работа с объектами/ Движение/Наведение")]
public class Наведение : RunityComponent
{
    protected override void OnEnable() { }
    protected override void OnDisable() { }

    [SerializeField] ЦельНаведения _наЧто;
    /// <summary>
    /// На что поворачивается объект. Замеченный - это ближайший объект в Поле зрения
    /// </summary>
    public ЦельНаведения наЧто { get => _наЧто; set => _наЧто = value; }

    [SerializeField] Transform _цель;
    /// <summary>
    /// К какому объекту поворачиваться
    /// </summary>
    public Transform цель { get => _цель; set => _цель = value; }

    [SerializeField] Флажок _режим2D = да;
    /// <summary>
    /// "Да" для 2D-игр: объект поворачивается только в плоскости экрана
    /// </summary>
    public Флажок режим2D { get => _режим2D; set => _режим2D = value; }

    [SerializeField] НаправлениеВзгляда2D _взгляд2D;
    /// <summary>
    /// Куда смотрит картинка объекта, если её не поворачивали
    /// </summary>
    public НаправлениеВзгляда2D взгляд2D { get => _взгляд2D; set => _взгляд2D = value; }

    [SerializeField] Дробное _скоростьПоворота = 0;
    /// <summary>
    /// Сколько градусов в секунду поворачивается объект. 0 означает, что поворот мгновенный
    /// </summary>
    public Дробное скоростьПоворота { get => _скоростьПоворота; set => _скоростьПоворота = value; }

    [SerializeField] Флажок _толькоПоГоризонтали = да;
    /// <summary>
    /// В 3D: если "да", то объект не наклоняется вверх и вниз, а только поворачивается в стороны
    /// </summary>
    public Флажок толькоПоГоризонтали { get => _толькоПоГоризонтали; set => _толькоПоГоризонтали = value; }

    /// <summary>
    /// Даёт "да", если объект уже смотрит на цель
    /// </summary>
    public Флажок навёлся { get; private set; }

    Vector3? Точка()
    {
        switch (наЧто)
        {
            case ЦельНаведения.Мышка:
                return режим2D ? (Vector3)Мышка.позицияВМире2D : МышкаВ3D();
            case ЦельНаведения.Замеченный:
                return TryGetComponent(out ПолеЗрения поле) && поле.ближайший != null ? поле.ближайший.transform.position : (Vector3?)null;
            default:
                return цель != null ? цель.position : (Vector3?)null;
        }
    }

    Vector3? МышкаВ3D()
    {
        if (Camera.main == null) return null;

        Ray луч = Camera.main.ScreenPointToRay(Мышка.позицияНаЭкране);
        Plane пол = new Plane(Vector3.up, transform.position);
        return пол.Raycast(луч, out float расстояние) ? луч.GetPoint(расстояние) : (Vector3?)null;
    }

    void Update()
    {
        Vector3? точка = Точка();
        if (точка == null) return;

        Vector3 направление = точка.Value - transform.position;
        Quaternion нужныйПоворот;

        if (режим2D)
        {
            направление.z = 0;
            if (направление == Vector3.zero) return;

            float сдвиг = взгляд2D switch
            {
                НаправлениеВзгляда2D.Вверх => -90,
                НаправлениеВзгляда2D.Влево => 180,
                НаправлениеВзгляда2D.Вниз => 90,
                _ => 0
            };
            нужныйПоворот = Quaternion.Euler(0, 0, Mathf.Atan2(направление.y, направление.x) * Mathf.Rad2Deg + сдвиг);
        }
        else
        {
            if (толькоПоГоризонтали) направление.y = 0;
            if (направление == Vector3.zero) return;
            нужныйПоворот = Quaternion.LookRotation(направление);
        }

        transform.rotation = скоростьПоворота > 0
            ? Quaternion.RotateTowards(transform.rotation, нужныйПоворот, скоростьПоворота * Время.времяСПрошлогоКадра)
            : нужныйПоворот;

        навёлся = Quaternion.Angle(transform.rotation, нужныйПоворот) < 2;
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Наведение))]
internal class НаведениеEditor : RunityEditor<Наведение>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект поворачивается к цели, к курсору или к тому, кого заметил");

        Пробел();

        Поле("На что", x => x.наЧто,
            (title, value) => (ЦельНаведения)EditorGUILayout.EnumPopup(title, value));

        if (компонент.наЧто == ЦельНаведения.Объект)
        {
            Поле("Цель", x => x.цель,
                (title, value) => ПолеОбъекта<Transform>(title, value, true));
        }
        else if (компонент.наЧто == ЦельНаведения.Замеченный && !компонент.TryGetComponent(out ПолеЗрения _))
        {
            Текст("Добавьте этому объекту Поле зрения", MessageType.Warning);
        }

        Поле("Скорость поворота", x => x.скоростьПоворота,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Пробел();

        Поле("Режим 2D", x => x.режим2D,
            (title, value) => EditorGUILayout.Toggle(title, value));

        if (компонент.режим2D)
        {
            Поле("Куда смотрит картинка", x => x.взгляд2D,
                (title, value) => (НаправлениеВзгляда2D)EditorGUILayout.EnumPopup(title, value));
        }
        else
        {
            Поле("Только по горизонтали", x => x.толькоПоГоризонтали,
                (title, value) => EditorGUILayout.Toggle(title, value));
        }
    }
}
#endif
