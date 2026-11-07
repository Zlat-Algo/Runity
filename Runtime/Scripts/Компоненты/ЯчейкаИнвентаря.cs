#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Что показывает ячейка
/// </summary>
public enum ЧтоПоказываетЯчейка { ЯчейкуПоНомеру, ОпределённуюВещь }

/// <summary>
/// Показывает в интерфейсе одну ячейку инвентаря: иконку вещи и её количество
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Механики/ Инвентарь/Ячейка инвентаря")]
public class ЯчейкаИнвентаря : RunityComponent, IPointerClickHandler
{
    protected override void OnEnable()
    {
        Подписаться();
        Обновить();
    }
    protected override void OnDisable()
    {
        if (подписан != null) подписан.Изменился -= Обновить;
        подписан = null;
    }

    [SerializeField] Инвентарь _инвентарь;
    /// <summary>
    /// Какой инвентарь показывает ячейка. Если не указать, найдётся первый инвентарь на сцене
    /// </summary>
    public Инвентарь инвентарь { get => _инвентарь; set { _инвентарь = value; Подписаться(); Обновить(); } }

    [SerializeField] ЧтоПоказываетЯчейка _показывает;
    /// <summary>
    /// Ячейку по номеру: вещи заполняют ячейки по очереди, как в Minecraft. Определённую вещь: ячейка всегда показывает одну и ту же вещь
    /// </summary>
    public ЧтоПоказываетЯчейка показывает { get => _показывает; set { _показывает = value; Обновить(); } }

    [SerializeField] Число _номер;
    /// <summary>
    /// Номер ячейки инвентаря, считая с нуля
    /// </summary>
    public Число номер { get => _номер; set { _номер = value; Обновить(); } }

    [SerializeField] Вещь _вещь;
    /// <summary>
    /// Какую вещь показывает ячейка в режиме "Определённую вещь"
    /// </summary>
    public Вещь вещь { get => _вещь; set { _вещь = value; Обновить(); } }

    [SerializeField] Image _иконка;
    /// <summary>
    /// Картинка интерфейса, в которой показывается иконка вещи
    /// </summary>
    public Image иконка { get => _иконка; set { _иконка = value; Обновить(); } }

    [SerializeField] Text _текстКоличества;
    /// <summary>
    /// Текст интерфейса, в котором показывается количество
    /// </summary>
    public Text текстКоличества { get => _текстКоличества; set { _текстКоличества = value; Обновить(); } }

    [SerializeField] Image _рамкаВыделения;
    /// <summary>
    /// Картинка, которая видна, когда ячейка выделена. Её включает компонент Выбор
    /// </summary>
    public Image рамкаВыделения { get => _рамкаВыделения; set { _рамкаВыделения = value; Обновить(); } }

    [SerializeField] Флажок _скрыватьЕдиницу = да;
    /// <summary>
    /// Если "да", то количество не пишется, когда вещь одна
    /// </summary>
    public Флажок скрыватьЕдиницу { get => _скрыватьЕдиницу; set { _скрыватьЕдиницу = value; Обновить(); } }

    [SerializeField] События _приНажатии;
    /// <summary>
    /// Срабатывает, когда игрок нажал на ячейку мышью
    /// </summary>
    public События приНажатии => _приНажатии;

    /// <summary>
    /// Даёт "да", если ячейка выделена
    /// </summary>
    public Флажок выделена
    {
        get => _выделена;
        set
        {
            _выделена = value;
            if (рамкаВыделения != null) рамкаВыделения.enabled = value;
        }
    }
    Флажок _выделена;

    /// <summary>
    /// Сколько вещей показывает ячейка
    /// </summary>
    public Число количество { get; private set; }

    /// <summary>
    /// Какая вещь сейчас в ячейке. Если ячейка пуста, будет пусто
    /// </summary>
    public Вещь вещьВЯчейке { get; private set; }

    /// <summary>
    /// Название предмета в ячейке
    /// </summary>
    public Строка названиеВЯчейке { get; private set; }

    /// <summary>
    /// Даёт "да", если в ячейке ничего нет
    /// </summary>
    public Флажок пуста => количество <= 0;

    Инвентарь подписан;

    void Start()
    {
        Подписаться();
        Обновить();
    }

    void Подписаться()
    {
        if (!Application.isPlaying) return;
        if (_инвентарь == null) _инвентарь = FindAnyObjectByType<Инвентарь>();
        if (подписан == _инвентарь) return;

        if (подписан != null) подписан.Изменился -= Обновить;
        подписан = _инвентарь;
        if (подписан != null) подписан.Изменился += Обновить;
    }

    /// <summary>
    /// Заново показывает содержимое. Обычно вызывается само, когда инвентарь меняется
    /// </summary>
    public void Обновить()
    {
        Sprite картинка = null;
        bool прозрачная = false;

        if (показывает == ЧтоПоказываетЯчейка.ОпределённуюВещь)
        {
            вещьВЯчейке = вещь;
            названиеВЯчейке = вещь != null ? вещь.name : "";
            количество = инвентарь != null ? инвентарь.Количество(вещь) : 0;

            if (вещь != null)
            {
                картинка = количество > 0 || вещь.иконкаОтсутствия == null ? вещь.иконка : вещь.иконкаОтсутствия;
                прозрачная = количество <= 0 && вещь.иконкаОтсутствия == null;
            }
        }
        else
        {
            ЗаписьИнвентаря запись = инвентарь != null ? инвентарь.Ячейка(номер) : null;
            bool есть = запись != null && !запись.пуста;
            вещьВЯчейке = есть ? запись.вещь : null;
            названиеВЯчейке = есть ? запись.имя : "";
            количество = есть ? запись.количество : 0;
            картинка = вещьВЯчейке != null ? вещьВЯчейке.иконка : null;
        }

        if (иконка != null)
        {
            иконка.sprite = картинка;
            иконка.enabled = картинка != null;
            Color цвет = иконка.color;
            цвет.a = прозрачная ? 0.35f : 1;
            иконка.color = цвет;
        }

        if (текстКоличества != null)
        {
            bool показать = количество > 1 || (количество == 1 && !скрыватьЕдиницу);
            текстКоличества.text = показать ? количество.ToString() : "";
        }

        if (рамкаВыделения != null) рамкаВыделения.enabled = выделена;
    }

    void OnValidate()
    {
        if (!Application.isPlaying) Обновить();
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData данные) => _приНажатии.Вызвать();

    /// <summary>
    /// Создаёт ячейку со стандартным оформлением внутри указанного объекта
    /// </summary>
    public static ЯчейкаИнвентаря СоздатьСтандартную(Transform родитель, Sprite фон = null)
    {
        GameObject объект = new GameObject("Ячейка инвентаря", typeof(RectTransform));
        объект.transform.SetParent(родитель, false);
        объект.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 80);
        Image подложка = объект.AddComponent<Image>();
        подложка.sprite = фон;
        подложка.type = фон != null ? Image.Type.Sliced : Image.Type.Simple;
        подложка.color = new Color(0, 0, 0, 0.5f);

        Image иконкаВещи = Дочерний(объект, "Иконка", 8).AddComponent<Image>();
        иконкаВещи.preserveAspect = true;
        иконкаВещи.raycastTarget = false;

        Text текст = Дочерний(объект, "Количество", 4).AddComponent<Text>();
        текст.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        текст.fontSize = 22;
        текст.fontStyle = FontStyle.Bold;
        текст.alignment = TextAnchor.LowerRight;
        текст.color = Color.white;
        текст.raycastTarget = false;
        текст.gameObject.AddComponent<Outline>();

        Image рамка = Дочерний(объект, "Выделение", -3).AddComponent<Image>();
        рамка.sprite = фон;
        рамка.type = фон != null ? Image.Type.Sliced : Image.Type.Simple;
        рамка.color = new Color(1, 0.85f, 0.3f, 0.6f);
        рамка.raycastTarget = false;

        ЯчейкаИнвентаря ячейка = объект.AddComponent<ЯчейкаИнвентаря>();
        ячейка._иконка = иконкаВещи;
        ячейка._текстКоличества = текст;
        ячейка._рамкаВыделения = рамка;
        рамка.transform.SetAsFirstSibling();
        ячейка.Обновить();
        return ячейка;
    }

    static GameObject Дочерний(GameObject родитель, string имя, float отступ)
    {
        GameObject объект = new GameObject(имя, typeof(RectTransform));
        объект.transform.SetParent(родитель.transform, false);
        RectTransform область = объект.GetComponent<RectTransform>();
        область.anchorMin = Vector2.zero;
        область.anchorMax = Vector2.one;
        область.offsetMin = new Vector2(отступ, отступ);
        область.offsetMax = new Vector2(-отступ, -отступ);
        return объект;
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(ЯчейкаИнвентаря))]
internal class ЯчейкаИнвентаряEditor : RunityEditor<ЯчейкаИнвентаря>
{
    protected override void Отрисовать()
    {
        Текст("Эта ячейка показывает иконку и количество вещей из Инвентаря");

        Пробел();

        Поле("Инвентарь", x => x.инвентарь,
            (title, value) => ПолеОбъекта<Инвентарь>(title, value, true));

        Поле("Показывает", x => x.показывает,
            (title, value) => (ЧтоПоказываетЯчейка)EditorGUILayout.EnumPopup(title, value));

        if (компонент.показывает == ЧтоПоказываетЯчейка.ЯчейкуПоНомеру)
        {
            Поле("Номер", x => x.номер,
                (title, value) => EditorGUILayout.IntField(title, value));
        }
        else
        {
            Поле("Вещь", x => x.вещь,
                (title, value) => ВещьDrawer.Выбрать(title, value));
        }

        Поле("Скрывать единицу", x => x.скрыватьЕдиницу,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        Поле("Иконка", x => x.иконка,
            (title, value) => ПолеОбъекта<Image>(title, value, true));

        Поле("Текст количества", x => x.текстКоличества,
            (title, value) => ПолеОбъекта<Text>(title, value, true));

        Поле("Рамка выделения", x => x.рамкаВыделения,
            (title, value) => ПолеОбъекта<Image>(title, value, true));

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При нажатии", "_приНажатии");
        ЗакончитьСобытия();
    }
}
#endif
