#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Предмет на сцене: кладёт себя в Инвентарь того, кто его коснулся
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Механики/ Инвентарь/Подбираемое")]
public class Подбираемое : RunityComponent
{
    protected override void OnEnable() { }
    protected override void OnDisable() { }

    [SerializeField] Вещь _вещь;
    /// <summary>
    /// Какая вещь попадёт в Инвентарь
    /// </summary>
    public Вещь вещь { get => _вещь; set => _вещь = value; }

    [SerializeField] Строка _название = "Монета";
    /// <summary>
    /// Под каким названием предмет попадёт в Инвентарь, если Вещь не указана
    /// </summary>
    public Строка название { get => _название; set => _название = value; }

    [SerializeField] Число _количество = 1;
    /// <summary>
    /// Сколько предметов получит игрок
    /// </summary>
    public Число количество { get => _количество; set => _количество = value; }

    [SerializeField] Флажок _удалитьПослеПодбора = да;
    /// <summary>
    /// Если "да", то предмет исчезнет со сцены, когда его подберут целиком
    /// </summary>
    public Флажок удалитьПослеПодбора { get => _удалитьПослеПодбора; set => _удалитьПослеПодбора = value; }

    [SerializeField] События _приПодборе;
    /// <summary>
    /// Срабатывает, когда предмет подобрали
    /// </summary>
    public События приПодборе => _приПодборе;

    [SerializeField] События _приНехваткеМеста;
    /// <summary>
    /// Срабатывает, когда в инвентаре не нашлось места для предмета
    /// </summary>
    public События приНехваткеМеста => _приНехваткеМеста;

    bool подобран;

    void Подобрать(GameObject кто)
    {
        if (подобран) return;

        Инвентарь инвентарь = кто.GetComponentInParent<Инвентарь>();
        if (инвентарь == null) return;

        int осталось = вещь != null ? инвентарь.Добавить(вещь, количество) : инвентарь.Добавить(название, количество);

        if (осталось == (int)количество)
        {
            _приНехваткеМеста.Вызвать();
            return;
        }

        _приПодборе.Вызвать();

        if (осталось > 0)
        {
            // Поместилась только часть: остаток ждёт на сцене
            количество = осталось;
            _приНехваткеМеста.Вызвать();
            return;
        }

        if (удалитьПослеПодбора)
        {
            подобран = да;
            Удалить(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D collider) => Подобрать(collider.gameObject);
    void OnCollisionEnter2D(Collision2D collision) => Подобрать(collision.gameObject);
    void OnTriggerEnter(Collider other) => Подобрать(other.gameObject);
    void OnCollisionEnter(Collision collision) => Подобрать(collision.gameObject);
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Подбираемое))]
internal class ПодбираемоеEditor : RunityEditor<Подбираемое>
{
    protected override void Отрисовать()
    {
        Текст("Этот предмет попадёт в Инвентарь того, кто его коснётся. Предмету нужен нетвёрдый коллайдер, а игроку - физическое тело и Инвентарь");

        Пробел();

        Поле("Вещь", x => x.вещь,
            (title, value) => ВещьDrawer.Выбрать(title, value));

        if (компонент.вещь == null)
        {
            Поле("Название", x => x.название,
                (title, value) => EditorGUILayout.TextField(title, value));
        }

        Поле("Количество", x => x.количество,
            (title, value) => EditorGUILayout.IntField(title, value));

        Поле("Удалить после подбора", x => x.удалитьПослеПодбора,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При подборе", "_приПодборе");
        ОтобразитьСобытие("При нехватке места", "_приНехваткеМеста");
        ЗакончитьСобытия();
    }
}
#endif
