using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Показывает в тексте значение любого поля любого компонента: здоровье, количество монет, скорость и так далее
/// </summary>
[AddComponentMenu("  Runity/ Интерфейс/ Элементы/Вывод значения")]
public class ВыводЗначения : RunityComponent
{
    protected override void OnEnable() { }
    protected override void OnDisable() { }

    [SerializeField] GameObject _объект;
    /// <summary>
    /// У какого объекта брать значение
    /// </summary>
    [Обязательное]
    public GameObject объект { get => _объект; set { _объект = value; найдено = false; } }

    [SerializeField] string _поле;
    /// <summary>
    /// Какое поле показывать. Записывается как «Компонент/поле»
    /// </summary>
    public Строка поле { get => _поле; set { _поле = value; найдено = false; } }

    [SerializeField] Graphic _текст;
    /// <summary>
    /// В какой текст выводить значение. Если не указать, возьмётся текст с этого объекта
    /// </summary>
    public Graphic текст { get => _текст; set => _текст = value; }

    [SerializeField] Строка _текстДо = "";
    /// <summary>
    /// Что написать перед значением, например «Монеты: »
    /// </summary>
    public Строка текстДо { get => _текстДо; set => _текстДо = value; }

    [SerializeField] Строка _текстПосле = "";
    /// <summary>
    /// Что написать после значения
    /// </summary>
    public Строка текстПосле { get => _текстПосле; set => _текстПосле = value; }

    [SerializeField] Число _знаковПослеЗапятой = 0;
    /// <summary>
    /// Сколько знаков после запятой показывать у дробных чисел
    /// </summary>
    public Число знаковПослеЗапятой { get => _знаковПослеЗапятой; set => _знаковПослеЗапятой = Mathf.Clamp(value, 0, 5); }

    [SerializeField] Строка _текстДа = "да";
    /// <summary>
    /// Что писать, если значение - флажок со значением "да"
    /// </summary>
    public Строка текстДа { get => _текстДа; set => _текстДа = value; }

    [SerializeField] Строка _текстНет = "нет";
    /// <summary>
    /// Что писать, если значение - флажок со значением "нет"
    /// </summary>
    public Строка текстНет { get => _текстНет; set => _текстНет = value; }

    /// <summary>
    /// Текст, который сейчас выводится
    /// </summary>
    public Строка выведено { get; private set; } = "";

    bool найдено;
    object цель;
    MemberInfo член;

    // Что можно выбрать в поле: все свойства с чтением и открытые поля всех компонентов объекта
    internal static IEnumerable<(string название, object цель, MemberInfo член)> Варианты(GameObject объект)
    {
        foreach (Component компонент in объект.GetComponents<Component>())
        {
            if (компонент == null) continue;
            Type тип = компонент.GetType();

            foreach (PropertyInfo свойство in тип.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (свойство.GetGetMethod() == null || свойство.GetIndexParameters().Length > 0 || !МожноПоказать(свойство.PropertyType)) continue;
                if (свойство.IsDefined(typeof(ObsoleteAttribute), true)) continue;
                yield return ($"{тип.Name}/{свойство.Name}", компонент, свойство);
            }

            foreach (FieldInfo поле in тип.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!МожноПоказать(поле.FieldType)) continue;
                yield return ($"{тип.Name}/{поле.Name}", компонент, поле);
            }
        }
    }

    static bool МожноПоказать(Type тип) =>
        тип.IsPrimitive || тип.IsEnum || тип == typeof(string) || тип == typeof(Строка) || тип == typeof(Число) || тип == typeof(Дробное) ||
        тип == typeof(Флажок) || тип == typeof(Vector2) || тип == typeof(Vector3) || тип == typeof(Направление2D) || тип == typeof(Направление3D) ||
        тип == typeof(Color) || тип == typeof(Цвет) || typeof(UnityEngine.Object).IsAssignableFrom(тип);

    void Найти()
    {
        найдено = true;
        цель = null;
        член = null;
        if (объект == null || string.IsNullOrEmpty(_поле)) return;

        foreach (var вариант in Варианты(объект))
        {
            if (вариант.название != _поле) continue;
            цель = вариант.цель;
            член = вариант.член;
            return;
        }
        Debug.LogWarning($"У объекта {объект.name} нет поля {_поле}", this);
    }

    /// <summary>
    /// Значение выбранного поля прямо сейчас
    /// </summary>
    public object значение
    {
        get
        {
            if (!найдено) Найти();
            if (член == null || цель is UnityEngine.Object o && o == null) return null;

            try
            {
                return член is PropertyInfo свойство ? свойство.GetValue(цель) : ((FieldInfo)член).GetValue(цель);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    void Start()
    {
        if (текст == null) _текст = ЗаписьТекста.НайтиРядом(this);
    }

    void LateUpdate()
    {
        выведено = (string)текстДо + ВСтроку(значение) + (string)текстПосле;
        if (текст != null) ЗаписьТекста.Записать(текст, выведено);
    }

    string ВСтроку(object значение)
    {
        string формат = "F" + Mathf.Clamp(знаковПослеЗапятой, 0, 5);
        switch (значение)
        {
            case null: return "";
            case Флажок флажок: return флажок ? текстДа : текстНет;
            case bool логическое: return логическое ? текстДа : текстНет;
            case Дробное дробное: return ((float)дробное).ToString(формат);
            case float дробное: return дробное.ToString(формат);
            case double дробное: return дробное.ToString(формат);
            case Vector2 v: return $"({v.x.ToString(формат)}; {v.y.ToString(формат)})";
            case Vector3 v: return $"({v.x.ToString(формат)}; {v.y.ToString(формат)}; {v.z.ToString(формат)})";
            case UnityEngine.Object объект: return объект != null ? объект.name : "";
            default: return значение.ToString();
        }
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(ВыводЗначения))]
internal class ВыводЗначенияEditor : RunityEditor<ВыводЗначения>
{
    public override bool RequiresConstantRepaint() => Application.isPlaying;

    protected override void Отрисовать()
    {
        Текст("Этот объект пишет в текст значение любого поля. Выберите объект, а потом поле одного из его компонентов");

        Пробел();

        Поле("Объект", x => x.объект,
            (title, value) => ПолеОбъекта<GameObject>(title, value, true));

        if (компонент.объект != null)
        {
            string[] варианты = ВыводЗначения.Варианты(компонент.объект).Select(в => в.название).ToArray();
            int выбранный = Array.IndexOf(варианты, (string)компонент.поле);

            if (варианты.Length == 0)
            {
                Текст("У этого объекта нет полей, которые можно показать", MessageType.Warning);
            }
            else
            {
                GUIContent[] пункты = new[] { new GUIContent("Не выбрано") }.Concat(варианты.Select(в => new GUIContent(в))).ToArray();
                Поле("Поле", x => x.поле, (title, value) =>
                {
                    int новый = EditorGUILayout.Popup(title, выбранный + 1, пункты);
                    return новый == 0 ? "" : варианты[новый - 1];
                });

                if (выбранный < 0)
                {
                    Текст("Выберите, какое поле показывать", MessageType.Warning);
                }
            }
        }
        else
        {
            Отметить(nameof(ВыводЗначения.поле));
        }

        Пробел();

        Поле("Текст", x => x.текст,
            (title, value) => ПолеОбъекта<Graphic>(title, value, true));

        Поле("Текст до", x => x.текстДо, (title, value) => EditorGUILayout.TextField(title, value));
        Поле("Текст после", x => x.текстПосле, (title, value) => EditorGUILayout.TextField(title, value));
        Поле("Знаков после запятой", x => x.знаковПослеЗапятой, (title, value) => EditorGUILayout.IntSlider(title, value, 0, 5));
        Поле("Текст для «да»", x => x.текстДа, (title, value) => EditorGUILayout.TextField(title, value));
        Поле("Текст для «нет»", x => x.текстНет, (title, value) => EditorGUILayout.TextField(title, value));

        if (Application.isPlaying)
        {
            Пробел();
            Текст("Сейчас выводится: " + компонент.выведено);
        }
    }
}
#endif
