using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Навигационный агент ходит по указанным точкам, обходя препятствия, и ждёт на каждой из них
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Передвижение/ Навигация/Навигационное патрулирование")]
public class НавигационноеПатрулирование : РасширениеАгента
{
    [SerializeField] List<Transform> _точки = new List<Transform>();
    /// <summary>
    /// Точки маршрута. Подойдут любые объекты сцены, например Пустышки
    /// </summary>
    public List<Transform> точки { get => _точки; set => _точки = value; }

    [SerializeField] РежимПатрулирования _режим;
    /// <summary>
    /// Как агент проходит маршрут
    /// </summary>
    public РежимПатрулирования режим { get => _режим; set => _режим = value; }

    [SerializeField] Дробное _ожиданиеНаТочке = 1;
    /// <summary>
    /// Сколько секунд агент стоит на каждой точке
    /// </summary>
    public Дробное ожиданиеНаТочке { get => _ожиданиеНаТочке; set => _ожиданиеНаТочке = value; }

    [SerializeField] Флажок _уступатьЦели = да;
    /// <summary>
    /// Если "да", то, пока у агента есть цель, патруль ждёт. Когда цель пропадёт, агент вернётся на маршрут
    /// </summary>
    public Флажок уступатьЦели { get => _уступатьЦели; set => _уступатьЦели = value; }

    [SerializeField] События _приДостиженииТочки;
    /// <summary>
    /// Срабатывает, когда агент дошёл до очередной точки
    /// </summary>
    public События приДостиженииТочки => _приДостиженииТочки;

    [SerializeField] События _приЗавершенииМаршрута;
    /// <summary>
    /// Срабатывает, когда агент прошёл весь маршрут
    /// </summary>
    public События приЗавершенииМаршрута => _приЗавершенииМаршрута;

    /// <summary>
    /// Номер точки, к которой агент идёт сейчас. Точки считаются с нуля
    /// </summary>
    public Число номерТочки { get; private set; }

    /// <summary>
    /// Пока "да", агент не идёт дальше по маршруту
    /// </summary>
    public Флажок пауза { get; set; }

    int шаг = 1;
    float ожидание;
    bool идёт;
    bool маршрутЗавершён;
    bool нужноОтправить;

    void Start() => НачатьМаршрутЗаново();

    /// <summary>
    /// Запускает маршрут заново с первой точки
    /// </summary>
    public void НачатьМаршрутЗаново()
    {
        маршрутЗавершён = false;
        ожидание = 0;
        шаг = 1;
        номерТочки = 0;
        нужноОтправить = true;
    }

    void ИдтиКТекущей()
    {
        идёт = false;
        if (номерТочки >= точки.Count || точки[номерТочки] == null) return;

        агент.ИдтиВТочку(точки[номерТочки].position);
        идёт = true;
    }

    void FixedUpdate()
    {
        if (пауза || маршрутЗавершён || точки.Count == 0) return;

        if (!агент.оригинал.isOnNavMesh) return;

        if (уступатьЦели && агент.цель != null)
        {
            нужноОтправить = true;
            return;
        }
        if (нужноОтправить)
        {
            нужноОтправить = false;
            ИдтиКТекущей();
            return;
        }

        if (идёт)
        {
            if (агент.оригинал.pathPending || !(агент.дошёл || !агент.оригинал.hasPath)) return;

            идёт = false;
            ожидание = ожиданиеНаТочке;
            _приДостиженииТочки.Вызвать();
            return;
        }

        ожидание -= Время.времяМеждуFixedUpdate;
        if (ожидание <= 0) ВыбратьСледующуюТочку();
    }

    void ВыбратьСледующуюТочку()
    {
        int следующая = номерТочки + шаг;

        if (следующая >= точки.Count || следующая < 0)
        {
            _приЗавершенииМаршрута.Вызвать();

            switch (режим)
            {
                case РежимПатрулирования.ПоКругу:
                    следующая = 0;
                    break;
                case РежимПатрулирования.ТудаИОбратно:
                    шаг = -шаг;
                    следующая = Mathf.Clamp(номерТочки + шаг, 0, точки.Count - 1);
                    break;
                case РежимПатрулирования.ОдинРаз:
                    маршрутЗавершён = true;
                    return;
            }
        }

        номерТочки = следующая;
        ИдтиКТекущей();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        for (int i = 0; i < точки.Count; i++)
        {
            if (точки[i] == null) continue;

            Gizmos.DrawWireSphere(точки[i].position, .2f);

            Transform следующая = точки[(i + 1) % точки.Count];
            if (следующая != null && (i + 1 < точки.Count || режим == РежимПатрулирования.ПоКругу))
            {
                Gizmos.DrawLine(точки[i].position, следующая.position);
            }
        }
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(НавигационноеПатрулирование))]
internal class НавигационноеПатрулированиеEditor : RunityEditor<НавигационноеПатрулирование>
{
    protected override void Отрисовать()
    {
        Текст("Навигационный агент на этом объекте ходит по указанным точкам. Создайте несколько Пустышек в сцене и перетащите их в список");

        Пробел();

        serializedObject.Update();
        ОтобразитьСвойство("Точки", "_точки");
        serializedObject.ApplyModifiedProperties();

        Поле("Режим", x => x.режим,
            (title, value) => (РежимПатрулирования)EditorGUILayout.EnumPopup(title, value));

        Поле("Ожидание на точке", x => x.ожиданиеНаТочке,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Поле("Уступать цели", x => x.уступатьЦели,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При достижении точки", "_приДостиженииТочки");
        ОтобразитьСобытие("При завершении маршрута", "_приЗавершенииМаршрута");
        ЗакончитьСобытия();
    }
}
#endif
