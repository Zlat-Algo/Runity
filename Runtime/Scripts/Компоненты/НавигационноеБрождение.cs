#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Навигационный агент сам ходит в случайные точки вокруг места, где появился, и обходит препятствия
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Передвижение/ Навигация/Навигационное брождение")]
public class НавигационноеБрождение : РасширениеАгента
{
    [SerializeField] Дробное _радиус = 5;
    /// <summary>
    /// Как далеко агент может уйти от центра брождения
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

    [SerializeField] Дробное _терпение = 8;
    /// <summary>
    /// Сколько секунд агент идёт к точке, прежде чем решить, что застрял, и выбрать другую
    /// </summary>
    public Дробное терпение { get => _терпение; set => _терпение = value; }

    [SerializeField] Флажок _уступатьЦели = да;
    /// <summary>
    /// Если "да", то, пока у агента есть цель, брождение ждёт. Например, агент гонится за игроком, а потеряв его, снова бродит
    /// </summary>
    public Флажок уступатьЦели { get => _уступатьЦели; set => _уступатьЦели = value; }

    [SerializeField] События _приНачалеПути;
    /// <summary>
    /// Срабатывает, когда агент пошёл к новой точке
    /// </summary>
    public События приНачалеПути => _приНачалеПути;

    [SerializeField] События _приДостиженииТочки;
    /// <summary>
    /// Срабатывает, когда агент дошёл до точки
    /// </summary>
    public События приДостиженииТочки => _приДостиженииТочки;

    /// <summary>
    /// Вокруг какой точки бродит агент. По умолчанию это место его появления
    /// </summary>
    public Направление3D центр { get; set; }

    /// <summary>
    /// Пока "да", агент не выбирает новые точки
    /// </summary>
    public Флажок пауза { get; set; }

    /// <summary>
    /// Даёт "да", пока агент идёт к точке
    /// </summary>
    public Флажок идёт { get; private set; }

    float отдых;
    float времяВПути;

    void Start()
    {
        центр = transform.position;
    }

    void FixedUpdate()
    {
        if (пауза || !агент.оригинал.isOnNavMesh) return;

        if (уступатьЦели && агент.цель != null)
        {
            идёт = нет;
            return;
        }

        if (идёт)
        {
            времяВПути += Время.времяМеждуFixedUpdate;
            if (агент.оригинал.pathPending) return;

            if (агент.дошёл || !агент.оригинал.hasPath)
            {
                идёт = нет;
                отдых = Случайности.СлучайноеИзДиапазона(паузаМинимальная, паузаМаксимальная);
                _приДостиженииТочки.Вызвать();
            }
            else if (времяВПути > терпение)
            {
                идёт = нет;
                отдых = 0;
            }
            return;
        }

        отдых -= Время.времяМеждуFixedUpdate;
        if (отдых <= 0 && НайтиТочку(out Vector3 точка))
        {
            агент.ИдтиВТочку(точка);
            идёт = да;
            времяВПути = 0;
            _приНачалеПути.Вызвать();
        }
    }

    /// <summary>
    /// Сразу отправляет агента в новую случайную точку
    /// </summary>
    public void ИдтиВНовуюТочку()
    {
        идёт = нет;
        отдых = 0;
    }

    bool НайтиТочку(out Vector3 точка)
    {
        for (int попытка = 0; попытка < 10; попытка++)
        {
            Vector2 смещение = Random.insideUnitCircle * радиус;
            Vector3 желаемая = (Vector3)центр + new Vector3(смещение.x, 0, смещение.y);

            if (NavMesh.SamplePosition(желаемая, out NavMeshHit попадание, 2f, NavMesh.AllAreas))
            {
                точка = попадание.position;
                return true;
            }
        }
        точка = transform.position;
        return false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Application.isPlaying ? (Vector3)центр : transform.position, радиус);
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(НавигационноеБрождение))]
internal class НавигационноеБрождениеEditor : RunityEditor<НавигационноеБрождение>
{
    protected override void Отрисовать()
    {
        Текст("Навигационный агент на этом объекте сам бродит по навигационной карте вокруг места, где появился");

        Пробел();

        Поле("Радиус", x => x.радиус,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Поле("Пауза минимальная", x => x.паузаМинимальная,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Поле("Пауза максимальная", x => x.паузаМаксимальная,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Поле("Терпение", x => x.терпение,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Поле("Уступать цели", x => x.уступатьЦели,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При начале пути", "_приНачалеПути");
        ОтобразитьСобытие("При достижении точки", "_приДостиженииТочки");
        ЗакончитьСобытия();
    }
}
#endif
