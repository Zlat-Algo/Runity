#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Объект сам ходит в случайные точки вокруг места, где появился
/// </summary>
[AddComponentMenu("  Runity/ Передвижение/ Самостоятельное/Брождение")]
public class Брождение : ДвижениеКТочке
{
    [SerializeField] Дробное _радиус = 5;
    /// <summary>
    /// Как далеко объект может уйти от центра брождения
    /// </summary>
    public Дробное радиус { get => _радиус; set => _радиус = value; }

    [SerializeField] Дробное _паузаМинимальная = 1;
    /// <summary>
    /// Минимальное время отдыха между прогулками, в секундах
    /// </summary>
    public Дробное паузаМинимальная { get => _паузаМинимальная; set => _паузаМинимальная = value; }

    [SerializeField] Дробное _паузаМаксимальная = 3;
    /// <summary>
    /// Максимальное время отдыха между прогулками, в секундах
    /// </summary>
    public Дробное паузаМаксимальная { get => _паузаМаксимальная; set => _паузаМаксимальная = value; }

    [SerializeField] Дробное _терпение = 5;
    /// <summary>
    /// Сколько секунд объект идёт к точке, прежде чем решить, что застрял, и выбрать другую
    /// </summary>
    public Дробное терпение { get => _терпение; set => _терпение = value; }

    /// <summary>
    /// Вокруг какой точки бродит объект. По умолчанию это место его появления
    /// </summary>
    public Направление3D центр { get; set; }

    float отдых;

    void Start()
    {
        центр = transform.position;
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (пауза) return;

        if (идёт)
        {
            if (времяВПути > терпение) Остановиться();
            return;
        }

        отдых -= Время.времяМеждуFixedUpdate;
        if (отдых <= 0)
        {
            ИдтиК(СлучайнаяТочка());
            отдых = Случайности.СлучайноеИзДиапазона(паузаМинимальная, паузаМаксимальная);
        }
    }

    Vector3 СлучайнаяТочка()
    {
        Vector2 смещение = Random.insideUnitCircle * радиус;
        return режим2D
            ? (Vector3)центр + new Vector3(смещение.x, смещение.y, 0)
            : (Vector3)центр + new Vector3(смещение.x, 0, смещение.y);
    }

    protected override void ПриДостиженииТочки() { }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Application.isPlaying ? (Vector3)центр : transform.position, радиус);
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Брождение))]
internal class БрождениеEditor : ДвижениеКТочкеEditor<Брождение>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект сам бродит в случайные точки вокруг места, где появился");

        Пробел();

        Поле("Радиус", x => x.радиус,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Поле("Пауза минимальная", x => x.паузаМинимальная,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Поле("Пауза максимальная", x => x.паузаМаксимальная,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Поле("Терпение", x => x.терпение,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Пробел();

        ОбщиеНастройки();

        Пробел();

        ОбщиеСобытия();
    }
}
#endif
