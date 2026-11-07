#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Плавно качает объект туда-обратно: парящие монеты, пульсирующие кнопки, раскачивающиеся фонари
/// </summary>
[AddComponentMenu("  Runity/ Работа с объектами/ Движение/Покачивание")]
public class Покачивание : RunityComponent
{
    protected override void OnEnable() { }
    protected override void OnDisable() { }

    [SerializeField] Направление3D _сдвиг = new Направление3D(0, 0.25, 0);
    /// <summary>
    /// Насколько далеко объект отходит от своего места по каждой оси
    /// </summary>
    public Направление3D сдвиг { get => _сдвиг; set => _сдвиг = value; }

    [SerializeField] Направление3D _наклон;
    /// <summary>
    /// На сколько градусов объект наклоняется вокруг каждой оси. Для 2D используйте Z
    /// </summary>
    public Направление3D наклон { get => _наклон; set => _наклон = value; }

    [SerializeField] Дробное _пульсация;
    /// <summary>
    /// Насколько объект увеличивается и уменьшается: 0.1 означает на десятую часть
    /// </summary>
    public Дробное пульсация { get => _пульсация; set => _пульсация = value; }

    [SerializeField] Дробное _частота = 1;
    /// <summary>
    /// Сколько качаний в секунду
    /// </summary>
    public Дробное частота { get => _частота; set => _частота = value; }

    [SerializeField] Флажок _случайноеНачало = да;
    /// <summary>
    /// Если "да", то одинаковые объекты качаются не в такт, а вразнобой
    /// </summary>
    public Флажок случайноеНачало { get => _случайноеНачало; set => _случайноеНачало = value; }

    Vector3 местоНаСтарте;
    Quaternion поворотНаСтарте;
    Vector3 размерНаСтарте;
    float фаза;

    void Start()
    {
        местоНаСтарте = transform.localPosition;
        поворотНаСтарте = transform.localRotation;
        размерНаСтарте = transform.localScale;
        фаза = случайноеНачало ? Random.value * Mathf.PI * 2 : 0;
    }

    void Update()
    {
        float волна = Mathf.Sin(Время.времяИгры * частота * Mathf.PI * 2 + фаза);

        transform.localPosition = местоНаСтарте + (Vector3)сдвиг * волна;
        transform.localRotation = поворотНаСтарте * Quaternion.Euler((Vector3)наклон * волна);
        transform.localScale = размерНаСтарте * (1 + пульсация * волна);
    }

    /// <summary>
    /// Запоминает текущее положение объекта как середину качания. Вызовите, если объект переместили
    /// </summary>
    public void ЗапомнитьМесто()
    {
        местоНаСтарте = transform.localPosition;
        поворотНаСтарте = transform.localRotation;
        размерНаСтарте = transform.localScale;
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Покачивание))]
internal class ПокачиваниеEditor : RunityEditor<Покачивание>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект плавно качается. Пока он качается, не двигайте его другими компонентами");

        Пробел();

        Поле("Сдвиг", x => x.сдвиг,
            (title, value) => EditorGUILayout.Vector3Field(title, value));

        Поле("Наклон", x => x.наклон,
            (title, value) => EditorGUILayout.Vector3Field(title, value));

        Поле("Пульсация", x => x.пульсация,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Пробел();

        Поле("Частота", x => x.частота,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Поле("Случайное начало", x => x.случайноеНачало,
            (title, value) => EditorGUILayout.Toggle(title, value));
    }
}
#endif
