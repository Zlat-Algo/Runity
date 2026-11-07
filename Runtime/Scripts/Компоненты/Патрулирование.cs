using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Как объект проходит список точек
/// </summary>
public enum РежимПатрулирования { ПоКругу, ТудаИОбратно, ОдинРаз }

/// <summary>
/// Объект ходит по указанным точкам и ждёт на каждой из них
/// </summary>
[AddComponentMenu("  Runity/ Передвижение/ Самостоятельное/Патрулирование")]
public class Патрулирование : ДвижениеКТочке
{
    [SerializeField] List<Transform> _точки = new List<Transform>();
    /// <summary>
    /// Точки маршрута. Подойдут любые объекты сцены, например Пустышки
    /// </summary>
    public List<Transform> точки { get => _точки; set => _точки = value; }

    [SerializeField] РежимПатрулирования _режим;
    /// <summary>
    /// Как объект проходит маршрут
    /// </summary>
    public РежимПатрулирования режим { get => _режим; set => _режим = value; }

    [SerializeField] Дробное _ожиданиеНаТочке = 1;
    /// <summary>
    /// Сколько секунд объект стоит на каждой точке
    /// </summary>
    public Дробное ожиданиеНаТочке { get => _ожиданиеНаТочке; set => _ожиданиеНаТочке = value; }

    [SerializeField] События _приЗавершенииМаршрута;
    /// <summary>
    /// Срабатывает, когда объект прошёл весь маршрут
    /// </summary>
    public События приЗавершенииМаршрута => _приЗавершенииМаршрута;

    /// <summary>
    /// Номер точки, к которой объект идёт сейчас. Точки считаются с нуля
    /// </summary>
    public Число номерТочки { get; private set; }

    int шаг = 1;
    float ожидание;
    bool маршрутЗавершён;

    void Start()
    {
        if (точки.Count > 0) ИдтиК(точки[0].position);
    }

    /// <summary>
    /// Запускает маршрут заново с первой точки
    /// </summary>
    public void НачатьМаршрутЗаново()
    {
        if (точки.Count == 0) return;

        маршрутЗавершён = false;
        ожидание = 0;
        шаг = 1;
        номерТочки = 0;
        ИдтиК(точки[0].position);
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (пауза || идёт || маршрутЗавершён || точки.Count == 0) return;

        ожидание -= Время.времяМеждуFixedUpdate;
        if (ожидание <= 0)
        {
            ВыбратьСледующуюТочку();
        }
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
        if (точки[номерТочки] != null) ИдтиК(точки[номерТочки].position);
    }

    protected override void ПриДостиженииТочки()
    {
        ожидание = ожиданиеНаТочке;
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
[CustomEditor(typeof(Патрулирование))]
internal class ПатрулированиеEditor : ДвижениеКТочкеEditor<Патрулирование>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект ходит по указанным точкам. Создайте несколько Пустышек в сцене и перетащите их в список");

        Пробел();

        serializedObject.Update();
        ОтобразитьСвойство("Точки", "_точки");
        serializedObject.ApplyModifiedProperties();

        Поле("Режим", x => x.режим,
            (title, value) => (РежимПатрулирования)EditorGUILayout.EnumPopup(title, value));

        Поле("Ожидание на точке", x => x.ожиданиеНаТочке,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Пробел();

        ОбщиеНастройки();

        Пробел();

        ОбщиеСобытия();
        НачатьСобытия();
        ОтобразитьСобытие("При завершении маршрута", "_приЗавершенииМаршрута");
        ЗакончитьСобытия();
    }
}
#endif
