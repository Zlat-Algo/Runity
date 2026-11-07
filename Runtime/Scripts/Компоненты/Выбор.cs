using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Что происходит с выбранным объектом
/// </summary>
public enum РежимВыбора { ТолькоСобытия, ВключатьТолькоВыбранный, ПодсвечиватьВыбранный }

/// <summary>
/// Хранит список объектов, из которых выбран один: оружие в руках, ячейка панели быстрого доступа, пункт меню
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Работа с объектами/Выбор")]
public class Выбор : RunityComponent
{
    protected override void OnEnable() { }
    protected override void OnDisable() { }

    [SerializeField] List<GameObject> _варианты = new List<GameObject>();
    /// <summary>
    /// Объекты, из которых делается выбор
    /// </summary>
    public List<GameObject> варианты => _варианты;

    [SerializeField] Число _номер;
    /// <summary>
    /// Номер выбранного объекта, считая с нуля
    /// </summary>
    public Число номер
    {
        get => _номер;
        set => Выбрать(value);
    }

    [SerializeField] РежимВыбора _режим = РежимВыбора.ПодсвечиватьВыбранный;
    /// <summary>
    /// Что происходит с выбранным объектом: его можно включать, подсвечивать или только сообщать о выборе событием
    /// </summary>
    public РежимВыбора режим { get => _режим; set { _режим = value; Применить(); } }

    [SerializeField] Цвет _цветВыделения = new Цвет(1, 0.85, 0.3);
    /// <summary>
    /// Каким цветом подсвечивается выбранный объект
    /// </summary>
    public Цвет цветВыделения { get => _цветВыделения; set { _цветВыделения = value; Применить(); } }

    [SerializeField] Флажок _зацикливать = да;
    /// <summary>
    /// Если "да", то после последнего объекта снова выбирается первый
    /// </summary>
    public Флажок зацикливать { get => _зацикливать; set => _зацикливать = value; }

    [SerializeField] Флажок _выбиратьКолесом = нет;
    /// <summary>
    /// Если "да", то выбор меняется прокруткой колёсика мыши
    /// </summary>
    public Флажок выбиратьКолесом { get => _выбиратьКолесом; set => _выбиратьКолесом = value; }

    [SerializeField] Флажок _выбиратьЦифрами = нет;
    /// <summary>
    /// Если "да", то клавиши 1-9 выбирают объект с таким номером
    /// </summary>
    public Флажок выбиратьЦифрами { get => _выбиратьЦифрами; set => _выбиратьЦифрами = value; }

    [SerializeField] События _приСмене;
    /// <summary>
    /// Срабатывает, когда выбран другой объект
    /// </summary>
    public События приСмене => _приСмене;

    /// <summary>
    /// Выбранный объект. Если список пуст, то пусто
    /// </summary>
    public GameObject выбранный => _номер >= 0 && _номер < _варианты.Count ? _варианты[_номер] : null;

    /// <summary>
    /// Сколько объектов в списке
    /// </summary>
    public Число количество => _варианты.Count;

    readonly Dictionary<Object, Color> исходныеЦвета = new Dictionary<Object, Color>();

    void Start() => Применить();

    void Update()
    {
        if (выбиратьКолесом)
        {
            if (Мышка.прокрутка > 0) ВыбратьПредыдущий();
            else if (Мышка.прокрутка < 0) ВыбратьСледующий();
        }

        if (выбиратьЦифрами)
        {
            for (int i = 0; i < 9; i++)
            {
                if (Управление.КлавишаНажата(Клавиша.Цифра1 + i)) Выбрать(i);
            }
        }
    }

    /// <summary>
    /// Выбирает объект с указанным номером, считая с нуля
    /// </summary>
    public void Выбрать(Число номерОбъекта)
    {
        if (_варианты.Count == 0) return;

        int новый = зацикливать
            ? ((номерОбъекта % _варианты.Count) + _варианты.Count) % _варианты.Count
            : Mathf.Clamp(номерОбъекта, 0, _варианты.Count - 1);

        bool изменился = новый != _номер;
        _номер = новый;
        Применить();
        if (изменился && Application.isPlaying) _приСмене.Вызвать();
    }

    /// <summary>
    /// Выбирает этот объект, если он есть в списке
    /// </summary>
    public void ВыбратьОбъект(GameObject объект)
    {
        int номерОбъекта = _варианты.IndexOf(объект);
        if (номерОбъекта >= 0) Выбрать(номерОбъекта);
    }

    /// <summary>
    /// Выбирает следующий объект
    /// </summary>
    public void ВыбратьСледующий() => Выбрать(_номер + 1);

    /// <summary>
    /// Выбирает предыдущий объект
    /// </summary>
    public void ВыбратьПредыдущий() => Выбрать(_номер - 1);

    void Применить()
    {
        for (int i = 0; i < _варианты.Count; i++)
        {
            GameObject объект = _варианты[i];
            if (объект == null) continue;

            bool выбран = i == _номер;

            if (режим == РежимВыбора.ВключатьТолькоВыбранный)
            {
                объект.SetActive(выбран);
            }
            else if (режим == РежимВыбора.ПодсвечиватьВыбранный)
            {
                Подсветить(объект, выбран);
            }
        }
    }

    void Подсветить(GameObject объект, bool выбран)
    {
        if (объект.TryGetComponent(out ЯчейкаИнвентаря ячейка))
        {
            ячейка.выделена = выбран;
            return;
        }

        if (объект.TryGetComponent(out Graphic графика)) графика.color = ЦветДля(графика, графика.color, выбран);
        else if (объект.TryGetComponent(out SpriteRenderer спрайт)) спрайт.color = ЦветДля(спрайт, спрайт.color, выбран);
    }

    Color ЦветДля(Object что, Color сейчас, bool выбран)
    {
        if (!исходныеЦвета.TryGetValue(что, out Color исходный))
        {
            исходный = сейчас;
            исходныеЦвета[что] = исходный;
        }
        return выбран ? (Color)цветВыделения : исходный;
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Выбор))]
internal class ВыборEditor : RunityEditor<Выбор>
{
    protected override void Отрисовать()
    {
        Текст("Из объектов этого списка всегда выбран один. Переключайте выбор колёсиком, цифрами или методами «Выбрать следующий» и «Выбрать предыдущий», например из Реакции на клавишу");

        Пробел();

        serializedObject.Update();
        ОтобразитьСвойство("Варианты", "_варианты");
        serializedObject.ApplyModifiedProperties();

        Поле("Номер", x => x.номер,
            (title, value) => EditorGUILayout.IntSlider(title, value, 0, Mathf.Max(0, компонент.количество - 1)));

        Поле("Режим", x => x.режим,
            (title, value) => (РежимВыбора)EditorGUILayout.EnumPopup(title, value));

        if (компонент.режим == РежимВыбора.ПодсвечиватьВыбранный)
        {
            Поле("Цвет выделения", x => x.цветВыделения,
                (title, value) => EditorGUILayout.ColorField(title, value));
        }

        Пробел();

        Поле("Зацикливать", x => x.зацикливать,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Поле("Выбирать колесом", x => x.выбиратьКолесом,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Поле("Выбирать цифрами", x => x.выбиратьЦифрами,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        НачатьГоризонтальнуюГруппу();
        Кнопка("Предыдущий", x => x.ВыбратьПредыдущий());
        Кнопка("Следующий", x => x.ВыбратьСледующий());
        ЗакончитьГоризонтальнуюГруппу();

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При смене", "_приСмене");
        ЗакончитьСобытия();
    }
}
#endif
