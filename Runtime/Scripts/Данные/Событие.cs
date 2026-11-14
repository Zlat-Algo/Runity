using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif
using UnityEngine;
using Component = UnityEngine.Component;

[Serializable]
public struct События
{
    public List<Событие> события;

    public void Вызвать()
    {
        //Консоль.Напечатать(события.Count);
        if (события == null) return;
        foreach(Событие событие in события)
        {
            событие.Вызвать();
            //Консоль.Напечатать(2);
        }
    }
}


[Serializable]
public struct Событие
{
    public GameObject объект;
    public string компонент;
    //public string данные;

    public string _string;
    public int _int;
    public bool _bool;
    public float _float;
    public Color _Color;
    public UnityEngine.Object _Object;
    public Vector2 _Vector2;
    public Vector3 _Vector3;
    public Sprite _Sprite;
    public GameObject _GameObject;
    public Transform _Transform;

    public string _string1;
    public int _int1;
    public bool _bool1;
    public float _float1;
    public Color _Color1;
    public UnityEngine.Object _Object1;
    public Vector2 _Vector21;
    public Vector3 _Vector31;
    public Sprite _Sprite1;
    public GameObject _GameObject1;
    public Transform _Transform1;

    public string _string2;
    public int _int2;
    public bool _bool2;
    public float _float2;
    public Color _Color2;
    public UnityEngine.Object _Object2;
    public Vector2 _Vector22;
    public Vector3 _Vector32;
    public Sprite _Sprite2;
    public GameObject _GameObject2;
    public Transform _Transform2;

    public string _string3;
    public int _int3;
    public bool _bool3;
    public float _float3;
    public Color _Color3;
    public UnityEngine.Object _Object3;
    public Vector2 _Vector23;
    public Vector3 _Vector33;
    public Sprite _Sprite3;
    public GameObject _GameObject3;
    public Transform _Transform3;

    public string _string4;
    public int _int4;
    public bool _bool4;
    public float _float4;
    public Color _Color4;
    public UnityEngine.Object _Object4;
    public Vector2 _Vector24;
    public Vector3 _Vector34;
    public Sprite _Sprite4;
    public GameObject _GameObject4;
    public Transform _Transform4;

    internal const BindingFlags ФлагиЧленов =
        BindingFlags.Instance |
        BindingFlags.Static |
        BindingFlags.Public |
        BindingFlags.NonPublic |
        BindingFlags.DeclaredOnly;

    // Типы от самого компонента до его родителей-заготовок (Передвижение, ДвижениеКТочке и т.п.)
    static IEnumerable<Type> ТипыКомпонента(Type type)
    {
        yield return type;

        for (Type родитель = type.BaseType;
             родитель != null && родитель != typeof(RunityComponent) && родитель != typeof(Компонент) && родитель != typeof(MonoBehaviour) && родитель != typeof(Behaviour) && родитель != typeof(Component);
             родитель = родитель.BaseType)
        {
            if (typeof(Компонент).IsAssignableFrom(родитель))
                yield return родитель;
        }
    }

    internal static IEnumerable<MethodInfo> МетодыТипа(Type type)
        => ТипыКомпонента(type).SelectMany(t => t.GetMethods(ФлагиЧленов)).Where(m => !m.IsSpecialName && !m.IsGenericMethod);

    internal static IEnumerable<PropertyInfo> СвойстваТипа(Type type)
        => ТипыКомпонента(type).SelectMany(t => t.GetProperties(ФлагиЧленов));

    // Что можно выбрать в событии: действия с самим объектом, члены его компонентов и включение-выключение компонентов
    internal struct Вариант
    {
        public string название;
        public object цель;
        public MemberInfo член;
    }

    static string Префикс(string имя) => ((Строка)имя).кириллица ? "" : "_/";

    static IEnumerable<Вариант> ВариантыТипа(string раздел, object цель, Type тип)
    {
        foreach (PropertyInfo свойство in СвойстваТипа(тип))
        {
            if (свойство.GetSetMethod(false) == null || свойство.GetIndexParameters().Length > 0) continue;
            yield return new Вариант { название = $"{Префикс(свойство.Name)}{раздел}/ {свойство.PropertyType.Name} {свойство.Name}", цель = цель, член = свойство };
        }

        foreach (MethodInfo метод in МетодыТипа(тип))
        {
            string аргументы = string.Join(", ", метод.GetParameters().Select(п => $"{п.ParameterType.Name} {п.Name}"));
            yield return new Вариант { название = $"{Префикс(метод.Name)}{раздел}/ {метод.Name} ({аргументы})", цель = цель, член = метод };
        }
    }

    internal static IEnumerable<Вариант> Варианты(GameObject объект)
    {
        foreach (Вариант вариант in ВариантыТипа("Объект", new ДействияОбъекта(объект), typeof(ДействияОбъекта)))
            yield return вариант;

        foreach (Component компонент in объект.GetComponents<Component>())
        {
            if (компонент == null) continue;

            Type тип = компонент.GetType();
            foreach (Вариант вариант in ВариантыТипа(тип.Name, компонент, тип))
                yield return вариант;

            if (компонент is Behaviour || компонент is Renderer || компонент is Collider)
            {
                foreach (Вариант вариант in ВариантыТипа(тип.Name, new ДействияКомпонента(компонент), typeof(ДействияКомпонента)))
                    yield return вариант;
            }
        }
    }

    public void Вызвать()
    {
        if (объект == null || string.IsNullOrEmpty(компонент))
            return;

        foreach (Вариант вариант in Варианты(объект))
        {
            if (вариант.название != компонент)
                continue;

            try
            {
                if (вариант.член is PropertyInfo свойство)
                {
                    свойство.SetValue(вариант.цель, GetPropertyValue(свойство.PropertyType));
                }
                else if (вариант.член is MethodInfo method)
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    object[] values = new object[parameters.Length];

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        Type parameterType = parameters[i].ParameterType;
                        string suffix = i == 0 ? "" : i.ToString();

                        if (parameterType == typeof(string) || parameterType == typeof(Строка))
                            values[i] = GetFieldValue<string>("_string" + suffix);
                        else if (parameterType == typeof(int) || parameterType == typeof(Число))
                            values[i] = GetFieldValue<int>("_int" + suffix);
                        else if (parameterType == typeof(bool) || parameterType == typeof(Флажок))
                            values[i] = GetFieldValue<bool>("_bool" + suffix);
                        else if (parameterType == typeof(float) || parameterType == typeof(Дробное))
                            values[i] = GetFieldValue<float>("_float" + suffix);
                        else if (parameterType == typeof(Color) || parameterType == typeof(Цвет))
                            values[i] = GetFieldValue<Color>("_Color" + suffix);
                        else if (parameterType == typeof(Vector2) || parameterType == typeof(Направление2D))
                            values[i] = GetFieldValue<Vector2>("_Vector2" + suffix);
                        else if (parameterType == typeof(Vector3) || parameterType == typeof(Направление3D))
                            values[i] = GetFieldValue<Vector3>("_Vector3" + suffix);
                        else if (parameterType == typeof(Sprite) || parameterType == typeof(Спрайт))
                            values[i] = GetFieldValue<Sprite>("_Sprite" + suffix);
                        else if (parameterType == typeof(GameObject) || parameterType == typeof(Объект))
                            values[i] = GetFieldValue<GameObject>("_GameObject" + suffix);
                        else if (parameterType == typeof(Transform))
                            values[i] = GetFieldValue<Transform>("_Transform" + suffix);
                        else if (parameterType == typeof(Трансформация))
                            values[i] = (Трансформация)GetFieldValue<Transform>("_Transform" + suffix);
                        else if (typeof(UnityEngine.Object).IsAssignableFrom(parameterType))
                            values[i] = GetFieldValue<UnityEngine.Object>("_Object" + suffix);
                        else
                            throw new NotSupportedException($"Тип аргумента не поддерживается: {parameterType.Name}");

                        values[i] = ПреобразоватьВТипRunity(values[i], parameterType);
                    }

                    method.Invoke(method.IsStatic ? null : вариант.цель, values);
                }
            }
            catch (TargetInvocationException e)
            {
                Debug.LogException(e.InnerException ?? e, объект);
            }
            catch (Exception e)
            {
                Debug.LogException(e, объект);
            }

            return;
        }

        Debug.LogWarning($"Не удалось найти выбранный метод или свойство: {компонент}", объект);
    }

    // Отражение не применяет неявные преобразования, поэтому int в параметр Число нужно превратить самим
    private static object ПреобразоватьВТипRunity(object value, Type type)
    {
        if (type == typeof(Строка)) return (Строка)(string)value;
        if (type == typeof(Число)) return (Число)(int)value;
        if (type == typeof(Флажок)) return (Флажок)(bool)value;
        if (type == typeof(Дробное)) return (Дробное)(float)value;
        if (type == typeof(Цвет)) return (Цвет)(Color)value;
        if (type == typeof(Направление2D)) return (Направление2D)(Vector2)value;
        if (type == typeof(Направление3D)) return (Направление3D)(Vector3)value;
        if (type == typeof(Спрайт)) return (Спрайт)(Sprite)value;
        if (type == typeof(Объект)) return (Объект)(GameObject)value;
        return value;
    }

    private T GetFieldValue<T>(string fieldName)
    {
        FieldInfo field = typeof(Событие).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.Public);

        if (field == null)
            throw new MissingFieldException(typeof(Событие).Name, fieldName);

        return (T)field.GetValue(this);
    }

    private object GetPropertyValue(Type type)
    {
        if (type == typeof(string))
            return _string;
        if (type == typeof(Строка))
            return (Строка)_string;
        if (type == typeof(int))
            return _int;
        if (type == typeof(Число))
            return (Число)_int;
        if (type == typeof(bool))
            return _bool;
        if (type == typeof(Флажок))
            return (Флажок)_bool;
        if (type == typeof(float))
            return _float;
        if (type == typeof(Дробное))
            return (Дробное)_float;
        if (type == typeof(Color))
            return _Color;
        if (type == typeof(Цвет))
            return (Цвет)_Color;
        if (type == typeof(Vector2))
            return _Vector2;
        if (type == typeof(Направление2D))
            return (Направление2D)_Vector2;
        if (type == typeof(Vector3))
            return _Vector3;
        if (type == typeof(Направление3D))
            return (Направление3D)_Vector3;
        if (type == typeof(Sprite))
            return _Sprite;
        if (type == typeof(Спрайт))
            return (Спрайт)_Sprite;
        if (type == typeof(GameObject))
            return _GameObject;
        if (type == typeof(Объект))
            return (Объект)_GameObject;
        if (type == typeof(Transform))
            return _Transform;
        if (type == typeof(Трансформация))
            return (Трансформация)_Transform;
        if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            return _Object;

        throw new NotSupportedException($"Тип свойства не поддерживается: {type.Name}");
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(События))]
internal class СобытияDrawer : PropertyDrawer
{
    // Один drawer рисует все События из списка (например, в Читере), поэтому у каждого свой ReorderableList
    private readonly Dictionary<string, ReorderableList> lists = new Dictionary<string, ReorderableList>();

    private ReorderableList Init(SerializedProperty property, GUIContent label)
    {
        if (lists.TryGetValue(property.propertyPath, out ReorderableList существующий) &&
            существующий.serializedProperty.serializedObject == property.serializedObject)
            return существующий;

        var items = property.FindPropertyRelative("события");

        ReorderableList list = new ReorderableList(
            property.serializedObject,
            items,
            true, true, true, true
        );

        GUIContent заголовок = new GUIContent(label.text, label.tooltip);
        list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, заголовок);

        list.elementHeightCallback = index =>
        {
            SerializedProperty element =
                items.GetArrayElementAtIndex(index);

            return EditorGUI.GetPropertyHeight(element, true) + 4f;
        };

        list.drawElementCallback = (rect, index, active, focused) =>
        {
            SerializedProperty element =
                items.GetArrayElementAtIndex(index);

            rect.y += 2f;
            rect.height = EditorGUI.GetPropertyHeight(element, true);

            EditorGUI.PropertyField(
                rect,
                element,
                GUIContent.none,
                true
            );
        };

        lists[property.propertyPath] = list;
        return list;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        Init(property, label).DoList(position);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return Init(property, label).GetHeight();
    }
}


[CustomPropertyDrawer(typeof(Событие))]
internal class СобытиеDrawer : PropertyDrawer
{
    const int максимумПараметров = 5;

    enum FieldType { unknow, _string, _int, _bool, _float, _Color, _Object, _Vector2, _Vector3, _Sprite, _GameObject, _Transform }

    FieldType GetFieldType(Type type)
    {
        if (type == typeof(string) || type == typeof(Строка)) return FieldType._string;
        if (type == typeof(int) || type == typeof(Число)) return FieldType._int;
        if (type == typeof(bool) || type == typeof(Флажок)) return FieldType._bool;
        if (type == typeof(float) || type == typeof(Дробное)) return FieldType._float;
        if (type == typeof(Color) || type == typeof(Цвет)) return FieldType._Color;
        if (type == typeof(Vector2) || type == typeof(Направление2D)) return FieldType._Vector2;
        if (type == typeof(Vector3) || type == typeof(Направление3D)) return FieldType._Vector3;
        if (type == typeof(Sprite) || type == typeof(Спрайт)) return FieldType._Sprite;
        if (type == typeof(GameObject) || type == typeof(Объект)) return FieldType._GameObject;
        if (type == typeof(Transform) || type == typeof(Трансформация)) return FieldType._Transform;
        if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return FieldType._Object;
        return FieldType.unknow;
    }

    // Звук, плитка, материал и т.п. хранятся в общем поле _Object, но выбирать можно только подходящий тип
    void НарисоватьЗначение(Rect rect, SerializedProperty infoProp, FieldType fieldType, Type type, string title)
    {
        if (fieldType == FieldType._Object)
            EditorGUI.ObjectField(rect, infoProp, type, new GUIContent(title));
        else
            EditorGUI.PropertyField(rect, infoProp, new GUIContent(title));
    }

    // Что нужно заполнить у выбранного варианта: тип и подпись каждого значения
    static List<(Type тип, string подпись)> Значения(MemberInfo член)
    {
        List<(Type, string)> значения = new List<(Type, string)>();
        if (член is PropertyInfo свойство) значения.Add((свойство.PropertyType, "Значение"));
        else if (член is MethodInfo метод)
            foreach (ParameterInfo параметр in метод.GetParameters().Take(максимумПараметров))
                значения.Add((параметр.ParameterType, параметр.Name));
        return значения;
    }

    static GameObject ВыбранныйОбъект(SerializedProperty property)
    {
        UnityEngine.Object ссылка = property.FindPropertyRelative("объект").objectReferenceValue;
        if (ссылка is GameObject g) return g;
        if (ссылка is Component c) return c.gameObject;
        return null;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty objProp = property.FindPropertyRelative("объект");
        SerializedProperty compProp = property.FindPropertyRelative("компонент");

        float singleLineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = 2f;

        Rect GetRect(int i) => new Rect(position.x, position.y + i * (singleLineHeight + spacing), position.width, singleLineHeight);

        EditorGUI.PropertyField(GetRect(0), objProp, new GUIContent("  Объект"));

        GameObject go = ВыбранныйОбъект(property);

        if (go != null)
        {
            List<Событие.Вариант> варианты = Событие.Варианты(go).ToList();
            string[] названия = варианты.Select(в => в.название).ToArray();
            int selectedIndex = Mathf.Max(0, Array.IndexOf(названия, compProp.stringValue));

            int newIndex = EditorGUI.Popup(GetRect(1), "  Действие", selectedIndex, названия);
            compProp.stringValue = названия[newIndex];

            List<(Type тип, string подпись)> значения = Значения(варианты[newIndex].член);
            for (int i = 0; i < значения.Count; i++)
            {
                FieldType fieldType = GetFieldType(значения[i].тип);
                if (fieldType == FieldType.unknow)
                {
                    EditorGUI.LabelField(GetRect(2 + i), new GUIContent($"  {значения[i].подпись} (неизвестный тип)"));
                    continue;
                }

                SerializedProperty infoProp = property.FindPropertyRelative(fieldType.ToString() + (i > 0 ? i.ToString() : ""));
                НарисоватьЗначение(GetRect(2 + i), infoProp, fieldType, значения[i].тип, "  " + значения[i].подпись);
            }
        }
        else
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.Popup(GetRect(1), "  Действие", 0, new string[] { "Сначала выберите объект" });
            EditorGUI.EndDisabledGroup();
            compProp.stringValue = null;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int argumentCount = 0;
        GameObject go = ВыбранныйОбъект(property);
        string выбранный = property.FindPropertyRelative("компонент").stringValue;

        if (go != null && !string.IsNullOrEmpty(выбранный))
        {
            foreach (Событие.Вариант вариант in Событие.Варианты(go))
            {
                if (вариант.название != выбранный) continue;
                argumentCount = Значения(вариант.член).Count;
                break;
            }
        }

        return (EditorGUIUtility.singleLineHeight + 2f) * (2 + argumentCount);
    }
}
#endif
