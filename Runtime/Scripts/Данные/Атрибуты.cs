using System;
using System.Linq;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

// Русские атрибуты для своих скриптов. Пишутся в квадратных скобках над полем, например [Подсказка("Сколько жизней у героя")]
// Атрибут [SerializeField] перевести нельзя: Unity узнаёт его только под этим именем

/// <summary>
/// Общая основа атрибутов Runity, которые меняют отрисовку поля
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public abstract class АтрибутRunity : PropertyAttribute { }

/// <summary>
/// Поле обязательно нужно заполнить. Пока оно пустое, в инспекторе оно подсвечивается красным
/// </summary>
public sealed class ОбязательноеAttribute : АтрибутRunity { }

/// <summary>
/// Всплывающая подсказка, которая появляется при наведении на поле
/// </summary>
public sealed class ПодсказкаAttribute : АтрибутRunity
{
    public readonly string текст;
    public ПодсказкаAttribute(string текст) => this.текст = текст;
}

/// <summary>
/// Другое название поля в инспекторе
/// </summary>
public sealed class НазваниеAttribute : АтрибутRunity
{
    public readonly string название;
    public НазваниеAttribute(string название) => this.название = название;
}

/// <summary>
/// Числовое поле превращается в ползунок от минимума до максимума
/// </summary>
public sealed class ДиапазонAttribute : АтрибутRunity
{
    public readonly float минимум, максимум;
    public ДиапазонAttribute(float минимум, float максимум) { this.минимум = минимум; this.максимум = максимум; }
}

/// <summary>
/// Число в поле не может быть меньше указанного
/// </summary>
public sealed class МинимумAttribute : АтрибутRunity
{
    public readonly float минимум;
    public МинимумAttribute(float минимум) => this.минимум = минимум;
}

/// <summary>
/// Большое поле для длинного текста в несколько строк
/// </summary>
public sealed class МногострочныйТекстAttribute : АтрибутRunity
{
    public readonly int строк;
    public МногострочныйТекстAttribute(int строк = 3) => this.строк = Mathf.Max(1, строк);
}

/// <summary>
/// Поле видно в инспекторе, но менять его там нельзя
/// </summary>
public sealed class ТолькоДляЧтенияAttribute : АтрибутRunity { }

/// <summary>
/// Поле сохраняется, но в инспекторе не показывается
/// </summary>
public sealed class СкрытьAttribute : АтрибутRunity { }

/// <summary>
/// Заголовок жирным шрифтом над полем. Помогает разделить настройки на группы
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
public sealed class ЗаголовокAttribute : PropertyAttribute
{
    public readonly string текст;
    public ЗаголовокAttribute(string текст) { this.текст = текст; order = -1; }
}

/// <summary>
/// Пустое место над полем
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
public sealed class ОтступAttribute : PropertyAttribute
{
    public readonly float высота;
    public ОтступAttribute(float высота = 8) { this.высота = высота; order = -1; }
}

/// <summary>
/// Над методом: в инспекторе появится кнопка, которая его вызывает. Метод не должен ничего принимать
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class КнопкаAttribute : Attribute
{
    public readonly string название;
    public КнопкаAttribute(string название = null) => this.название = название;
}

#if UNITY_EDITOR
internal static class ОтрисовкаАтрибутов
{
    internal static readonly Color цветОбязательного = new Color(1f, 0.55f, 0.55f);

    // Поля Runity вроде Дробного или Строки хранят значение внутри, в поле «значение»
    internal static SerializedProperty Внутреннее(SerializedProperty свойство)
    {
        if (свойство.propertyType != SerializedPropertyType.Generic) return свойство;
        SerializedProperty внутри = свойство.FindPropertyRelative("значение");
        return внутри != null && !внутри.isArray || внутри != null && внутри.propertyType == SerializedPropertyType.String ? внутри : свойство;
    }

    internal static bool Пустое(SerializedProperty свойство)
    {
        SerializedProperty внутри = Внутреннее(свойство);
        switch (внутри.propertyType)
        {
            case SerializedPropertyType.ObjectReference: return внутри.objectReferenceValue == null;
            case SerializedPropertyType.ExposedReference: return внутри.exposedReferenceValue == null;
            case SerializedPropertyType.String: return string.IsNullOrEmpty(внутри.stringValue);
            default: return false;
        }
    }

    internal static bool Пустое(object значение) =>
        значение == null ||
        значение is UnityEngine.Object объект && объект == null ||
        значение is Объект обёртка && (GameObject)обёртка == null ||
        значение is string строка && строка.Length == 0 ||
        значение is Строка русская && string.IsNullOrEmpty(русская);

    internal static void Обвести(Rect область)
    {
        if (Event.current.type != EventType.Repaint) return;
        Color красный = new Color(0.9f, 0.2f, 0.2f);
        EditorGUI.DrawRect(new Rect(область.x - 1, область.y - 1, область.width + 2, 1), красный);
        EditorGUI.DrawRect(new Rect(область.x - 1, область.yMax, область.width + 2, 1), красный);
        EditorGUI.DrawRect(new Rect(область.x - 1, область.y, 1, область.height), красный);
        EditorGUI.DrawRect(new Rect(область.xMax, область.y, 1, область.height), красный);
    }

    internal static string ПодсказкаОбязательного(string подсказка) =>
        "Это поле обязательно нужно заполнить" + (string.IsNullOrEmpty(подсказка) ? "" : ". " + подсказка);
}

[CustomPropertyDrawer(typeof(АтрибутRunity), true)]
internal class АтрибутRunityDrawer : PropertyDrawer
{
    T Атрибут<T>() where T : Attribute => fieldInfo.GetCustomAttributes(typeof(T), true).FirstOrDefault() as T;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (Атрибут<СкрытьAttribute>() != null) return -EditorGUIUtility.standardVerticalSpacing;

        МногострочныйТекстAttribute многострочный = Атрибут<МногострочныйТекстAttribute>();
        if (многострочный != null && ОтрисовкаАтрибутов.Внутреннее(property).propertyType == SerializedPropertyType.String)
            return EditorGUIUtility.singleLineHeight * (многострочный.строк + 1) + 2;

        SerializedProperty внутри = ОтрисовкаАтрибутов.Внутреннее(property);
        return EditorGUI.GetPropertyHeight(внутри, label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (Атрибут<СкрытьAttribute>() != null) return;

        GUIContent подпись = new GUIContent(label);
        НазваниеAttribute название = Атрибут<НазваниеAttribute>();
        if (название != null) подпись.text = название.название;
        ПодсказкаAttribute подсказка = Атрибут<ПодсказкаAttribute>();
        if (подсказка != null) подпись.tooltip = подсказка.текст;

        bool пустоеОбязательное = Атрибут<ОбязательноеAttribute>() != null && ОтрисовкаАтрибутов.Пустое(property);
        if (пустоеОбязательное) подпись.tooltip = ОтрисовкаАтрибутов.ПодсказкаОбязательного(подпись.tooltip);

        Color былЦвет = GUI.color;
        if (пустоеОбязательное) GUI.color = ОтрисовкаАтрибутов.цветОбязательного;
        EditorGUI.BeginDisabledGroup(Атрибут<ТолькоДляЧтенияAttribute>() != null);

        SerializedProperty внутри = ОтрисовкаАтрибутов.Внутреннее(property);
        ДиапазонAttribute диапазон = Атрибут<ДиапазонAttribute>();
        МногострочныйТекстAttribute многострочный = Атрибут<МногострочныйТекстAttribute>();
        МинимумAttribute минимум = Атрибут<МинимумAttribute>();

        EditorGUI.BeginProperty(position, подпись, property);
        if (диапазон != null && внутри.propertyType == SerializedPropertyType.Float)
        {
            EditorGUI.Slider(position, внутри, диапазон.минимум, диапазон.максимум, подпись);
        }
        else if (диапазон != null && внутри.propertyType == SerializedPropertyType.Integer)
        {
            EditorGUI.IntSlider(position, внутри, (int)диапазон.минимум, (int)диапазон.максимум, подпись);
        }
        else if (многострочный != null && внутри.propertyType == SerializedPropertyType.String)
        {
            Rect строкаПодписи = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(строкаПодписи, подпись);
            Rect поле = new Rect(position.x, строкаПодписи.yMax + 2, position.width, position.height - строкаПодписи.height - 2);
            EditorGUI.BeginChangeCheck();
            string текст = EditorGUI.TextArea(поле, внутри.stringValue, EditorStyles.textArea);
            if (EditorGUI.EndChangeCheck()) внутри.stringValue = текст;
        }
        else
        {
            EditorGUI.PropertyField(position, внутри, подпись, true);
        }
        EditorGUI.EndProperty();

        if (минимум != null)
        {
            if (внутри.propertyType == SerializedPropertyType.Float && внутри.floatValue < минимум.минимум) внутри.floatValue = минимум.минимум;
            if (внутри.propertyType == SerializedPropertyType.Integer && внутри.intValue < минимум.минимум) внутри.intValue = (int)минимум.минимум;
        }

        EditorGUI.EndDisabledGroup();
        GUI.color = былЦвет;
        if (пустоеОбязательное) ОтрисовкаАтрибутов.Обвести(position);
    }
}

[CustomPropertyDrawer(typeof(ЗаголовокAttribute))]
internal class ЗаголовокDrawer : DecoratorDrawer
{
    public override float GetHeight() => EditorGUIUtility.singleLineHeight * 1.5f;

    public override void OnGUI(Rect position)
    {
        position.yMin += EditorGUIUtility.singleLineHeight * 0.5f;
        EditorGUI.LabelField(position, ((ЗаголовокAttribute)attribute).текст, EditorStyles.boldLabel);
    }
}

[CustomPropertyDrawer(typeof(ОтступAttribute))]
internal class ОтступDrawer : DecoratorDrawer
{
    public override float GetHeight() => ((ОтступAttribute)attribute).высота;
    public override void OnGUI(Rect position) { }
}

// Свои скрипты пользователя рисуются как обычно, но под полями появляются кнопки для методов с атрибутом [Кнопка]
[CanEditMultipleObjects]
[CustomEditor(typeof(Компонент), true)]
internal class КомпонентEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        foreach (MethodInfo метод in target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            КнопкаAttribute кнопка = метод.GetCustomAttribute<КнопкаAttribute>(true);
            if (кнопка == null) continue;

            string название = string.IsNullOrEmpty(кнопка.название) ? ObjectNames.NicifyVariableName(метод.Name) : кнопка.название;
            if (метод.GetParameters().Length > 0)
            {
                EditorGUILayout.HelpBox($"Метод {метод.Name} не может быть кнопкой: у него есть параметры", MessageType.Warning);
                continue;
            }

            if (GUILayout.Button(название))
            {
                foreach (UnityEngine.Object цель in targets)
                {
                    Undo.RecordObject(цель, название);
                    метод.Invoke(метод.IsStatic ? null : цель, null);
                    EditorUtility.SetDirty(цель);
                }
            }
        }
    }
}
#endif
