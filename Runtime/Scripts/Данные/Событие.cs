using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Component = UnityEngine.Component;

[Serializable]
public struct События
{
    public List<Событие> события;

    public void Вызвать()
    {
        //Консоль.Напечатать(события.Count);
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

    public void Вызвать()
    {
        if (объект == null || string.IsNullOrEmpty(компонент))
            return;

        const BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly;

        string selected = компонент;

        foreach (Component target in объект.GetComponents<Component>())
        {
            if (target == null)
                continue;

            Type type = target.GetType();
            string prefix = "";

            // В твоём Inspector для имён латиницей добавляется "_/"
            // Для кириллицы префикса нет.
            foreach (MethodInfo method in type.GetMethods(flags)
                         .Where(m => !m.IsSpecialName))
            {
                ParameterInfo[] parameters = method.GetParameters();

                string args = string.Join(", ", parameters.Select(
                    p => $"{p.ParameterType.Name} {p.Name}"));

                prefix = ((Строка)method.Name).кириллица ? "" : "_/";

                string option =
                    $"{prefix}{type.Name}/ {method.Name} ({args})";

                if (option != selected)
                    continue;

                object[] values = new object[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    Type parameterType = parameters[i].ParameterType;

                    string suffix = i == 0 ? "" : i.ToString();

                    if (parameterType == typeof(string) ||
                        parameterType == typeof(Строка))
                        values[i] = GetFieldValue<string>("_string" + suffix);

                    else if (parameterType == typeof(int) ||
                             parameterType == typeof(Число))
                        values[i] = GetFieldValue<int>("_int" + suffix);

                    else if (parameterType == typeof(bool) ||
                             parameterType == typeof(Флажок))
                        values[i] = GetFieldValue<bool>("_bool" + suffix);

                    else if (parameterType == typeof(float) ||
                             parameterType == typeof(Дробное))
                        values[i] = GetFieldValue<float>("_float" + suffix);

                    else if (parameterType == typeof(Color) ||
                             parameterType == typeof(Цвет))
                        values[i] = GetFieldValue<Color>("_Color" + suffix);

                    else if (parameterType == typeof(Vector2) ||
                             parameterType == typeof(Направление2D))
                        values[i] = GetFieldValue<Vector2>(
                            i == 0 ? "_Vector2" : "_Vector2" + suffix);

                    else if (parameterType == typeof(Vector3) ||
                             parameterType == typeof(Направление3D))
                        values[i] = GetFieldValue<Vector3>(
                            i == 0 ? "_Vector3" : "_Vector3" + suffix);

                    else if (parameterType == typeof(Sprite) ||
                             parameterType == typeof(Спрайт))
                        values[i] = GetFieldValue<Sprite>("_Sprite" + suffix);

                    else if (parameterType == typeof(GameObject) ||
                             parameterType == typeof(Объект))
                        values[i] = GetFieldValue<GameObject>(
                            "_GameObject" + suffix);

                    else if (parameterType == typeof(Transform))
                        values[i] = GetFieldValue<Transform>(
                            "_Transform" + suffix);

                    else if (typeof(UnityEngine.Object)
                                 .IsAssignableFrom(parameterType))
                        values[i] = GetFieldValue<UnityEngine.Object>(
                            "_Object" + suffix);

                    else
                        throw new NotSupportedException(
                            $"Тип аргумента не поддерживается: {parameterType.Name}");
                }

                try
                {
                    method.Invoke(target, values);
                }
                catch (TargetInvocationException e)
                {
                    Debug.LogException(e.InnerException ?? e, target);
                }

                return;
            }

            foreach (PropertyInfo property in type.GetProperties(flags))
            {
                if (property.GetSetMethod(false) == null)
                    continue;

                prefix = ((Строка)property.Name).кириллица ? "" : "_/";

                string option =
                    $"{prefix}{type.Name}/ {property.PropertyType.Name} {property.Name}";

                if (option != selected)
                    continue;

                object value = GetPropertyValue(property.PropertyType);

                property.SetValue(target, value);
                return;
            }
        }

        Debug.LogWarning($"Не удалось найти выбранный метод или свойство: {selected}", объект);
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
        if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            return _Object;

        throw new NotSupportedException($"Тип свойства не поддерживается: {type.Name}");
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(События))]
internal class СобытияDrawer : PropertyDrawer
{
    private ReorderableList list;

    private void Init(SerializedProperty property)
    {
        if (list != null)
            return;

        var items = property.FindPropertyRelative("события");

        list = new ReorderableList(
            property.serializedObject,
            items,
            true, true, true, true
        );

        list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, property.displayName);

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
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        Init(property);

        list.DoList(position);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        Init(property);
        return list.GetHeight();
    }
}


[CustomPropertyDrawer(typeof(Событие))]
internal class СобытиеDrawer : PropertyDrawer
{
    //float height = 3;
    int maxParameterIndex = 4;

    enum FieldType { unknow, _string, _int, _bool, _float, _Color, _Object, _Vector2, _Vector3, _Sprite, _GameObject, _Transform }

    FieldType GetFieldType(Type type)
    {
        if (type == typeof(string) || type == typeof(Строка)) return FieldType._string;
        if (type == typeof(int) || type == typeof(Число)) return FieldType._int;
        if (type == typeof(bool) || type == typeof(Флажок)) return FieldType._bool;
        if (type == typeof(float) || type == typeof(Дробное)) return FieldType._float;
        if (type == typeof(Color) || type == typeof(Цвет)) return FieldType._Color;
        if (type == typeof(UnityEngine.Object)) return FieldType._Object;
        if (type == typeof(Vector2) || type == typeof(Направление2D)) return FieldType._Vector2;
        if (type == typeof(Vector3) || type == typeof(Направление3D)) return FieldType._Vector3;
        if (type == typeof(Sprite) || type == typeof(Спрайт)) return FieldType._Sprite;
        if (type == typeof(GameObject) || type == typeof(Объект)) return FieldType._GameObject;
        if (type == typeof(Transform)) return FieldType._Transform;
        return FieldType.unknow;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty objProp = property.FindPropertyRelative("объект");
        SerializedProperty compProp = property.FindPropertyRelative("компонент");
        //SerializedProperty infoProp = property.FindPropertyRelative("данные");

        float singleLineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = 2f;

        Rect GetRect(int i) => new Rect(position.x, position.y + i * (singleLineHeight + spacing), position.width, singleLineHeight);

        //EditorGUI.LabelField(GetRect(0), label);

        // Поле для выбора объекта
        EditorGUI.PropertyField(GetRect(0), objProp, new GUIContent("  Объект"));

        // Определяем GameObject из ссылки (если в поле объект или компонент этого объекта)
        GameObject go = null;
        if (objProp.objectReferenceValue is GameObject g)
        {
            go = g;
        }
        else if (objProp.objectReferenceValue is Component c)
        {
            go = c.gameObject;
        }

        if (go != null)
        {
            // Получаем все компоненты объекта
            Component[] components = go.GetComponents<Component>();
            List<string> options = new List<string>();
            int selectedIndex = 0;

            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null)
                {
                    //options.Add($"{i}: {components[i].GetType().Name}");
                    Список<MemberInfo> members = new Список<MemberInfo>();
                    var properties = components[i].GetType().GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    members.добавить(properties);
                    MethodInfo[] methods = components[i].GetType().GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                        .Where(m => !m.IsSpecialName).ToArray();
                    members.добавить(methods);
                    foreach (MemberInfo member in members)
                    {
                        Строка option = "";
                        Строка имяКомпонента = components[i].GetType().Name;
                        Строка имяЧлена = member.Name;
                        Строка прочее = ((Строка)member.Name).кириллица ? "" : "_/";
                        if (member is PropertyInfo && ((PropertyInfo)member).GetSetMethod(false) != null)
                        {
                            Type типПоля = ((PropertyInfo)member).PropertyType;
                            option = $"{прочее}{имяКомпонента}/ {типПоля.Name} {имяЧлена}";

                            if (option == compProp.stringValue)
                            {
                                SerializedProperty infoProp = null;
                                FieldType fieldType = GetFieldType(типПоля);
                                if (fieldType == FieldType.unknow)
                                {
                                    //Консоль.НапечататьОшибку($"Неизвестный тип поля {имяКомпонента}/{типПоля.Name} {имяЧлена}!");
                                    EditorGUI.LabelField(GetRect(2), new GUIContent($"  Значение (неизвестный тип)"));
                                }
                                else
                                {
                                    infoProp = property.FindPropertyRelative(fieldType.ToString());
                                    EditorGUI.PropertyField(GetRect(2), infoProp, new GUIContent("  Значение"));
                                    //height = 3;
                                }
                            }
                        }
                        if (member is MethodInfo)
                        {
                            Строка параметры = "";
                            ParameterInfo[] parameters = ((MethodInfo)member).GetParameters();
                            foreach (ParameterInfo parameter in parameters)
                            {
                                if (параметры.чтоТоЕсть)
                                {
                                    параметры += ", ";
                                }
                                Type типПараметра = parameter.ParameterType;
                                параметры += типПараметра.Name + " " + parameter.Name;

                                
                            }
                            параметры = $"({параметры})";
                            option = $"{прочее}{имяКомпонента}/ {имяЧлена} {параметры}";

                            int parameterIndex = 0;
                            foreach (ParameterInfo parameter in parameters)
                            {
                                if (option == compProp.stringValue)
                                {
                                    Type типПараметра = parameter.ParameterType;
                                    SerializedProperty infoProp = null;
                                    FieldType fieldType = GetFieldType(типПараметра);
                                    if (fieldType == FieldType.unknow)
                                    {
                                        //Консоль.НапечататьОшибку($"Неизвестный тип параметра {имяКомпонента}/{имяЧлена}: {типПараметра.Name} {parameter.Name}!");
                                        EditorGUI.LabelField(GetRect(2 + parameterIndex), new GUIContent($"  {parameter.Name} (неизвестный тип)"));
                                    }
                                    else
                                    {
                                        infoProp = property.FindPropertyRelative(fieldType.ToString() + (parameterIndex > 0 ? parameterIndex.ToString() : ""));
                                        EditorGUI.PropertyField(GetRect(2 + parameterIndex), infoProp, new GUIContent("  " + parameter.Name));
                                        //height = 3;
                                    }
                                    parameterIndex++;
                                    if (parameterIndex > maxParameterIndex)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                        if (option == compProp.stringValue)
                        {
                            selectedIndex = options.Count;
                        }
                        if (option.чтоТоЕсть)
                        {
                            options.Add(option);
                        }
                    }
                }
                else
                {
                    options.Add("Null Component");
                }
            }

            // Отрисовываем выпадающий список
            int newIndex = EditorGUI.Popup(GetRect(1), "  Компонент", selectedIndex, options.ToArray());
            // Если индекс изменился, записываем новый компонент в свойство
            compProp.stringValue = options[newIndex >= options.Count ? options.Count - 1 : newIndex];

            //EditorGUI.PropertyField(GetRect(2), infoProp, new GUIContent("Инфо"));
        }
        else
        {
            // Если объект не выбран, выводим заблокированное предупреждение
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.Popup(GetRect(1), "  Компонент", 0, new string[] { "Сначала выберите объект" });
            EditorGUI.EndDisabledGroup();
            compProp.stringValue = null;
            //height = 2;
        }

        EditorGUI.EndProperty();
    }

    /*public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Высота для двух строк полей
        return EditorGUIUtility.singleLineHeight * height + 2f;
    }*/

    private int GetArgumentCount(SerializedProperty property)
    {
        var objProp = property.FindPropertyRelative("объект");
        var compProp = property.FindPropertyRelative("компонент");

        GameObject go = null;

        if (objProp.objectReferenceValue is GameObject g)
            go = g;
        else if (objProp.objectReferenceValue is Component c)
            go = c.gameObject;

        if (go == null || string.IsNullOrEmpty(compProp.stringValue))
            return 0;

        const BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly;

        foreach (Component component in go.GetComponents<Component>())
        {
            if (component == null)
                continue;

            Type componentType = component.GetType();
            string componentName = componentType.Name;

            foreach (PropertyInfo p in componentType.GetProperties(flags))
            {
                if (p.GetSetMethod(false) == null)
                    continue;

                string prefix = ((Строка)p.Name).кириллица ? "" : "_/";
                string option = $"{prefix}{componentName}/ {p.PropertyType.Name} {p.Name}";

                if (option == compProp.stringValue)
                    return 1;
            }

            foreach (MethodInfo m in componentType.GetMethods(flags)
                         .Where(m => !m.IsSpecialName))
            {
                ParameterInfo[] parameters = m.GetParameters();

                string args = string.Join(", ", parameters.Select(
                    p => $"{p.ParameterType.Name} {p.Name}"));

                string prefix = ((Строка)m.Name).кириллица ? "" : "_/";
                string option = $"{prefix}{componentName}/ {m.Name} ({args})";

                if (option == compProp.stringValue)
                    return parameters.Length;
            }
        }

        return 0;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int argumentCount = GetArgumentCount(property);

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = 2f;

        return (lineHeight + spacing) * (2 + argumentCount);
    }
}
#endif