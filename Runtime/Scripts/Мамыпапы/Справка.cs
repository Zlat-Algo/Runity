#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Собирает список полей, методов и событий компонента. Описания берутся прямо из комментариев
/// в его коде, поэтому достаточно написать один раз summary над нужным полем или методом
/// </summary>
internal static class Справка
{
    internal class Запись
    {
        public string имя;
        public string заголовок;
        public string описание;
    }

    internal class Раздел
    {
        public Type тип;
        public string описание;
        public List<Запись> поля = new List<Запись>();
        public List<Запись> методы = new List<Запись>();
        public List<Запись> события = new List<Запись>();

        public bool Подходит(string поиск) =>
            string.IsNullOrEmpty(поиск) ||
            тип.Name.IndexOf(поиск, StringComparison.OrdinalIgnoreCase) >= 0 ||
            поля.Concat(методы).Concat(события).Any(запись =>
                запись.заголовок.IndexOf(поиск, StringComparison.OrdinalIgnoreCase) >= 0 ||
                запись.описание.IndexOf(поиск, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    const string ключТипа = "#тип";

    static readonly Dictionary<Type, Раздел> кэш = new Dictionary<Type, Раздел>();
    static readonly Dictionary<Type, Dictionary<string, string>> кэшОписаний = new Dictionary<Type, Dictionary<string, string>>();
    static GUIStyle стильЗаголовка;
    static GUIStyle стильОписания;

    public static Раздел Получить(Type тип)
    {
        if (кэш.TryGetValue(тип, out Раздел готовый)) return готовый;

        Раздел раздел = new Раздел { тип = тип };
        раздел.описание = Описания(тип).TryGetValue(ключТипа, out string описаниеТипа) ? описаниеТипа : "";

        foreach (Type текущий in ЦепочкаТипов(тип))
        {
            Dictionary<string, string> описания = Описания(текущий);
            const BindingFlags флаги = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

            foreach (PropertyInfo свойство in текущий.GetProperties(флаги))
            {
                if (свойство.Name == "main") continue;

                string описание = Описание(описания, свойство.Name, -1);
                Запись запись = new Запись
                {
                    имя = свойство.Name,
                    заголовок = $"{Название(свойство.PropertyType)} {свойство.Name}{(свойство.GetSetMethod() == null ? " (только чтение)" : "")}",
                    описание = описание
                };

                bool этоСобытие = свойство.PropertyType == typeof(События) || typeof(UnityEventBase).IsAssignableFrom(свойство.PropertyType);
                (этоСобытие ? раздел.события : раздел.поля).Add(запись);
            }

            foreach (MethodInfo метод in текущий.GetMethods(флаги).Where(м => !м.IsSpecialName))
            {
                ParameterInfo[] параметры = метод.GetParameters();
                string аргументы = string.Join(", ", параметры.Select(п => $"{Название(п.ParameterType)} {п.Name}"));
                string результат = метод.ReturnType == typeof(void) ? "" : Название(метод.ReturnType) + " ";

                раздел.методы.Add(new Запись
                {
                    имя = метод.Name,
                    заголовок = $"{результат}{метод.Name}({аргументы})",
                    описание = Описание(описания, метод.Name, параметры.Length)
                });
            }
        }

        кэш[тип] = раздел;
        return раздел;
    }

    /// <summary>
    /// Описание поля, метода или события из комментария в коде. Используется для всплывающих подсказок
    /// </summary>
    public static string Описание(Type тип, string имя)
    {
        Раздел раздел = Получить(тип);
        Запись запись = раздел.поля.Concat(раздел.события).Concat(раздел.методы).FirstOrDefault(з => з.имя == имя);
        return запись != null ? запись.описание : "";
    }

    // Сначала сам тип, потом родители-заготовки. Базовые классы Runity и Unity в справку не попадают
    static IEnumerable<Type> ЦепочкаТипов(Type тип)
    {
        for (Type текущий = тип;
             текущий != null && текущий != typeof(object) && текущий != typeof(RunityComponent) && текущий != typeof(Компонент) && текущий != typeof(MonoBehaviour) && текущий != typeof(ScriptableObject);
             текущий = текущий.BaseType)
        {
            yield return текущий;
        }
    }

    static string Описание(Dictionary<string, string> описания, string имя, int параметров)
    {
        if (параметров >= 0 && описания.TryGetValue($"{имя}/{параметров}", out string точное)) return точное;
        return описания.TryGetValue(имя, out string общее) ? общее : "";
    }

    static string Название(Type тип)
    {
        if (тип == typeof(float)) return "Дробное";
        if (тип == typeof(int)) return "Число";
        if (тип == typeof(bool)) return "Флажок";
        if (тип == typeof(string)) return "Строка";
        if (тип == typeof(Vector2)) return "Направление2D";
        if (тип == typeof(Vector3)) return "Направление3D";
        if (тип == typeof(Color)) return "Цвет";
        if (тип == typeof(GameObject)) return "Объект";
        if (тип == typeof(Sprite)) return "Спрайт";
        if (тип == typeof(Transform)) return "Трансформация";

        if (тип.IsGenericType)
        {
            string имя = тип.Name.Split('`')[0];
            if (имя == "List" || имя == "IReadOnlyList") имя = "Список";
            return $"{имя}<{string.Join(", ", тип.GetGenericArguments().Select(Название))}>";
        }

        return тип.Name;
    }

    #region Чтение комментариев из кода

    static Dictionary<string, string> Описания(Type тип)
    {
        if (кэшОписаний.TryGetValue(тип, out Dictionary<string, string> готовые)) return готовые;

        Dictionary<string, string> описания = new Dictionary<string, string>();
        string файл = НайтиФайл(тип);

        if (файл != null)
        {
            РазобратьФайл(File.ReadAllLines(файл), тип.Name, описания);
        }

        кэшОписаний[тип] = описания;
        return описания;
    }

    static string НайтиФайл(Type тип)
    {
        foreach (string guid in AssetDatabase.FindAssets($"{тип.Name} t:MonoScript"))
        {
            string путь = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(путь) == тип.Name) return путь;
        }
        return null;
    }

    static void РазобратьФайл(string[] строки, string имяТипа, Dictionary<string, string> описания)
    {
        StringBuilder комментарий = new StringBuilder();

        foreach (string строка in строки)
        {
            string чистая = строка.Trim();

            if (чистая.StartsWith("///"))
            {
                комментарий.Append(чистая.Substring(3).Trim()).Append(' ');
                continue;
            }

            if (чистая.Length == 0 || чистая.StartsWith("[")) continue;

            if (комментарий.Length > 0)
            {
                string описание = ТекстКомментария(комментарий.ToString());
                ЗапомнитьОписание(чистая, описание, имяТипа, описания);
                комментарий.Clear();
            }
        }
    }

    static string ТекстКомментария(string комментарий)
    {
        Match совпадение = Regex.Match(комментарий, "<summary>(.*?)</summary>");
        string текст = совпадение.Success ? совпадение.Groups[1].Value : комментарий;
        return Regex.Replace(текст, "<[^>]+>", "").Trim();
    }

    static void ЗапомнитьОписание(string объявление, string описание, string имяТипа, Dictionary<string, string> описания)
    {
        Match тип = Regex.Match(объявление, @"\b(class|struct|enum|interface)\s+(\w+)");
        if (тип.Success)
        {
            if (тип.Groups[2].Value == имяТипа) описания[ключТипа] = описание;
            return;
        }

        int конец = объявление.IndexOfAny(new[] { '(', '{', '=', ';' });
        string начало = (конец >= 0 ? объявление.Substring(0, конец) : объявление).Trim();
        if (начало.Length == 0) return;

        string имя = Regex.Replace(начало.Split(' ').Last(), "<.*", "").TrimStart('_');

        if (!описания.ContainsKey(имя)) описания[имя] = описание;

        if (конец >= 0 && объявление[конец] == '(')
        {
            int закрывающая = объявление.IndexOf(')', конец);
            string аргументы = закрывающая > 0 ? объявление.Substring(конец + 1, закрывающая - конец - 1).Trim() : "";
            int параметров = аргументы.Length == 0 ? 0 : аргументы.Count(символ => символ == ',') + 1;
            описания[$"{имя}/{параметров}"] = описание;
        }
    }

    #endregion

    #region Отрисовка

    static void ПодготовитьСтили()
    {
        if (стильЗаголовка != null) return;

        стильЗаголовка = new GUIStyle(EditorStyles.label) { wordWrap = true, fontStyle = FontStyle.Bold };
        стильОписания = new GUIStyle(EditorStyles.wordWrappedMiniLabel);
    }

    /// <summary>
    /// Рисует под компонентом всё, чего нет в инспекторе: поля, методы и события, доступные только из кода
    /// </summary>
    public static void ОтобразитьВИнспекторе(Type тип, ICollection<string> видимые)
    {
        Раздел раздел = Получить(тип);
        bool Скрыт(Запись запись) => !видимые.Contains(запись.имя);

        if (!раздел.поля.Concat(раздел.методы).Concat(раздел.события).Any(Скрыт)) return;

        string ключ = $"Runity.Справка.{тип.Name}";
        bool открыта = SessionState.GetBool(ключ, false);
        bool новое = EditorGUILayout.Foldout(открыта, new GUIContent("Скрытое", "Поля, методы и события этого компонента, которые доступны только из кода"), true);

        if (новое != открыта) SessionState.SetBool(ключ, новое);
        if (!новое) return;

        ПодготовитьСтили();
        ОтобразитьГруппу("Поля", раздел.поля.Where(Скрыт));
        ОтобразитьГруппу("Методы", раздел.методы.Where(Скрыт));
        ОтобразитьГруппу("События", раздел.события.Where(Скрыт));
    }

    public static void ОтобразитьСодержимое(Раздел раздел)
    {
        ПодготовитьСтили();

        if (!string.IsNullOrEmpty(раздел.описание))
        {
            EditorGUILayout.LabelField(раздел.описание, EditorStyles.wordWrappedLabel);
        }

        ОтобразитьГруппу("Поля", раздел.поля);
        ОтобразитьГруппу("Методы", раздел.методы);
        ОтобразитьГруппу("События", раздел.события);
    }

    static void ОтобразитьГруппу(string название, IEnumerable<Запись> отобранные)
    {
        List<Запись> записи = отобранные.ToList();
        if (записи.Count == 0) return;

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(название, EditorStyles.boldLabel);

        foreach (Запись запись in записи)
        {
            EditorGUILayout.SelectableLabel(запись.заголовок, стильЗаголовка, GUILayout.Height(стильЗаголовка.CalcHeight(new GUIContent(запись.заголовок), EditorGUIUtility.currentViewWidth - 40)));

            if (!string.IsNullOrEmpty(запись.описание))
            {
                EditorGUILayout.LabelField(запись.описание, стильОписания);
            }

            // Отступ после каждой записи, чтобы соседние методы не сливались в один текст
            EditorGUILayout.Space(6);
        }
    }

    #endregion
}

/// <summary>
/// Окно со справкой по всем компонентам и инструментам Runity
/// </summary>
internal class ОкноСправки : EditorWindow
{
    static readonly Type[] инструменты =
    {
        typeof(Консоль), typeof(Время), typeof(Математика), typeof(Случайности), typeof(Мышка), typeof(Сохранения), typeof(ПоискОбъектов),
        typeof(Объект), typeof(Трансформация), typeof(Флажок), typeof(Число), typeof(Дробное), typeof(Строка)
    };

    string поиск = "";
    Vector2 прокрутка;
    readonly HashSet<Type> открытые = new HashSet<Type>();
    List<Справка.Раздел> компоненты;
    List<Справка.Раздел> статика;

    [MenuItem("Runity/Справка", false, 100)]
    static void Открыть()
    {
        GetWindow<ОкноСправки>("Справка Runity");
    }

    void Собрать()
    {
        компоненты = TypeCache.GetTypesDerivedFrom<RunityComponent>()
            .Where(тип => !тип.IsAbstract)
            .OrderBy(тип => тип.Name)
            .Select(Справка.Получить)
            .ToList();

        статика = инструменты.Select(Справка.Получить).ToList();
    }

    void OnGUI()
    {
        if (компоненты == null) Собрать();

        поиск = EditorGUILayout.TextField("Поиск", поиск);
        EditorGUILayout.Space();

        прокрутка = EditorGUILayout.BeginScrollView(прокрутка);

        ОтобразитьСписок("Компоненты", компоненты);
        EditorGUILayout.Space();
        ОтобразитьСписок("Инструменты для кода", статика);

        EditorGUILayout.EndScrollView();
    }

    void ОтобразитьСписок(string название, List<Справка.Раздел> разделы)
    {
        EditorGUILayout.LabelField(название, EditorStyles.boldLabel);

        foreach (Справка.Раздел раздел in разделы.Where(р => р.Подходит(поиск)))
        {
            bool открыт = открытые.Contains(раздел.тип) || !string.IsNullOrEmpty(поиск);
            bool новое = EditorGUILayout.Foldout(открыт, раздел.тип.Name, true);

            if (новое) открытые.Add(раздел.тип);
            else открытые.Remove(раздел.тип);

            if (новое)
            {
                EditorGUI.indentLevel++;
                Справка.ОтобразитьСодержимое(раздел);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
        }
    }
}
#endif
