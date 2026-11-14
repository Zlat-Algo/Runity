using System;
using System.Linq.Expressions;
using System.Reflection;
using Unity.Mathematics;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class RunityComponent : Компонент
{
    //protected Флажок вРедакторе => !Application.isPlaying;

    public Component оригинальныйКомпонент { get; protected set; }

    protected TAsset НайтиАссет<TAsset>(string guid) where TAsset : UnityEngine.Object
    {
        #if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<TAsset>(AssetDatabase.GUIDToAssetPath(guid));
#else
        return null;
#endif
    }

    // Компонент, который тоже зависит от оригинала
    //protected Component twinComponent;

    protected virtual void OnEnable()
    {
        if (оригинальныйКомпонент == null) return;
        if (оригинальныйКомпонент is Behaviour behaviour)
            behaviour.enabled = true;
        else if (оригинальныйКомпонент is Renderer renderer)
            renderer.enabled = true;
        else if (оригинальныйКомпонент is Collider collider)
            collider.enabled = true;
        else
            Консоль.НапечататьПредупреждение("Пустое OnEnable", this);
    }
    protected virtual void OnDisable()
    {
        if (оригинальныйКомпонент == null) return;
        if (оригинальныйКомпонент is Behaviour behaviour)
            behaviour.enabled = false;
        else if (оригинальныйКомпонент is Renderer renderer)
            renderer.enabled = false;
        else if (оригинальныйКомпонент is Collider collider)
            collider.enabled = false;
        else
            Консоль.НапечататьПредупреждение("Пустое OnDisable", this);
    }
    /*protected virtual void OnDestroy()
    {
        if (оригинальныйКомпонент == null) return;
        if (gameObject == null) return;
#if !UNITY_EDITOR
        Destroy(оригинальныйКомпонент, .1f);
#endif
#if UNITY_EDITOR
        print(gameObject);
        gameObject.AddComponent<ComponentKiller>().SetTargets((twinComponent, оригинальныйКомпонент));
#endif
    }*/
}

#if UNITY_EDITOR
internal abstract class RunityEditor<T> : Editor where T : RunityComponent
{
    /// <summary>
    /// Здесь каждый компонент рисует свои поля. Справка добавляется под ними автоматически
    /// </summary>
    protected abstract void Отрисовать();

    // Поля, события и кнопки, которые уже нарисованы в инспекторе. Их не нужно повторять в "Скрытом"
    readonly System.Collections.Generic.HashSet<string> видимые = new System.Collections.Generic.HashSet<string>();

    public sealed override void OnInspectorGUI()
    {
        видимые.Clear();
        Отрисовать();
        Пробел();
        Справка.ОтобразитьВИнспекторе(target.GetType(), видимые);
    }

    /// <summary>
    /// Подпись поля со всплывающей подсказкой из комментария к члену класса
    /// </summary>
    protected GUIContent Подпись(string title, string имяЧлена)
    {
        видимые.Add(имяЧлена);
        return new GUIContent(title, Справка.Описание(target.GetType(), имяЧлена));
    }

    /// <summary>
    /// Отмечает члены класса, нарисованные вручную, чтобы они не попали в "Скрытое"
    /// </summary>
    protected void Отметить(params string[] имена)
    {
        foreach (string имя in имена) видимые.Add(имя);
    }

    /// <summary>
    /// Кнопка, которая вызывает метод у всех выделенных компонентов. Подсказка берётся из описания метода
    /// </summary>
    protected void Кнопка(string message, Expression<Action<T>> метод)
    {
        string имя = ((MethodCallExpression)метод.Body).Method.Name;
        if (GUILayout.Button(Подпись(message, имя)))
        {
            Action<T> вызов = метод.Compile();
            foreach (T component in components)
            {
                Undo.RecordObject(component, message);
                вызов(component);
                EditorUtility.SetDirty(component);
            }
        }
    }

    protected TAsset НайтиАссет<TAsset>(string guid) where TAsset : UnityEngine.Object
    {
        #if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<TAsset>(AssetDatabase.GUIDToAssetPath(guid));
#else
        return null;
#endif
    }

    protected void Текст(string message, MessageType type = MessageType.None)
    {
        EditorGUILayout.HelpBox(message, type);
    }

    protected void ПредупреждениеПроУправление()
    {
        // Управление создаётся само при запуске игры, поэтому предупреждать не о чем

    }

    protected void Кнопка(string message, Action callback)
    {
        MethodInfo метод = callback.Method;
        bool методКомпонента = !метод.Name.Contains("<") && метод.DeclaringType.IsAssignableFrom(target.GetType());
        GUIContent подпись = методКомпонента ? Подпись(message, метод.Name) : new GUIContent(message);

        if (GUILayout.Button(подпись))
        {
            callback();
        }
    }

    /// <summary>
    /// Кнопка для статического метода. Подсказка берётся из описания метода с указанным именем
    /// </summary>
    protected void КнопкаМетода(string message, string имяМетода, Action callback)
    {
        if (GUILayout.Button(Подпись(message, имяМетода)))
        {
            callback();
        }
    }

    protected void Кнопка(string message, string tooltip, Action callback)
    {
        if (GUILayout.Button(new GUIContent(message, tooltip)))
        {
            callback();
        }
    }

    protected bool МиниКнопка(string message, string tooltip)
    {
        return GUILayout.Button(new GUIContent(message, tooltip), EditorStyles.miniButton, GUILayout.Width(25));
    }

    protected void МиниКнопка(string message, string tooltip, Action callback)
    {
        if (GUILayout.Button(new GUIContent(message, tooltip), EditorStyles.miniButton, GUILayout.Width(25)))
        {
            callback();
        }
    }

    protected void Пробел()
    {
        EditorGUILayout.Space();
    }

    protected void Пробел(float ширина)
    {
        EditorGUILayout.Space(ширина);
    }

    protected TObj ПолеОбъекта<TObj>(GUIContent title, UnityEngine.Object obj, bool fromScene = false) where TObj : UnityEngine.Object
    {
        return (TObj)EditorGUILayout.ObjectField(title, obj, typeof(TObj), fromScene);
    }

    protected void НачатьСобытия()
    {
        serializedObject.Update();
    }

    protected void ОтобразитьСобытие(string отображаемоеНазвание, string названиеСобытия)
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty(названиеСобытия), Подпись(отображаемоеНазвание, названиеСобытия.TrimStart('_')));
    }

    /// <summary>
    /// Рисует сериализованное поле или список полей стандартным способом Unity
    /// </summary>
    protected void ОтобразитьСвойство(string отображаемоеНазвание, string названиеПоля)
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty(названиеПоля), Подпись(отображаемоеНазвание, названиеПоля.TrimStart('_')), true);
    }

    /// <summary>
    /// Рисует выбор нескольких тегов для поля типа Теги
    /// </summary>
    protected void ОтобразитьТеги(string отображаемоеНазвание, string названиеПоля)
    {
        serializedObject.Update();
        ОтобразитьСвойство(отображаемоеНазвание, названиеПоля);
        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// Выпадающий список с физическими слоями
    /// </summary>
    protected LayerMask ПолеСлоёв(GUIContent title, LayerMask value)
    {
        int маска = EditorGUILayout.MaskField(title, UnityEditorInternal.InternalEditorUtility.LayerMaskToConcatenatedLayersMask(value), UnityEditorInternal.InternalEditorUtility.layers);
        return UnityEditorInternal.InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(маска);
    }

    protected void ЗакончитьСобытия()
    {
        serializedObject.ApplyModifiedProperties();
    }

    protected void НачатьГоризонтальнуюГруппу() => EditorGUILayout.BeginHorizontal();
    protected void ЗакончитьГоризонтальнуюГруппу() => EditorGUILayout.EndHorizontal();

    protected T компонент => (T)target;

    protected T[] components
    {
        get
        {
            T[] result = new T[targets.Length];

            for (int i = 0; i < targets.Length; i++)
                result[i] = (T)targets[i];

            return result;
        }
    }

    protected bool IsChanged<TValue>(Func<T, TValue> getter)
    {
        if (targets.Length < 2)
            return false;

        TValue первое = getter(компонент);

        foreach (T объект in components)
        {
            if (!Equals(первое, getter(объект)))
                return true;
        }

        return false;
    }

    protected void ChangeOriginals(string title, Func<T, UnityEngine.Object> getOriginal, Action<T> changeAction)
    {
        foreach (T component in components)
        {
            UnityEngine.Object original =
                getOriginal(component);

            if (original == null)
                continue;

            Undo.RecordObject(
                original,
                title
            );

            changeAction(component);

            EditorUtility.SetDirty(original);
        }
    }

    protected TValue Поле<TValue>(string title, Expression<Func<T, TValue>> property, Func<GUIContent, TValue, TValue> drawer)
    {
        PropertyInfo propertyInfo = GetPropertyInfo(property);

        TValue Get(T component)
        {
            return (TValue)propertyInfo.GetValue(component);
        }

        void Set(T component, TValue value)
        {
            propertyInfo.SetValue(component, value);
        }

        EditorGUI.showMixedValue = IsChanged(Get);

        EditorGUI.BeginChangeCheck();

        TValue value = НарисоватьСОбязательностью(propertyInfo, title, Get(компонент), drawer);

        if (EditorGUI.EndChangeCheck())
        {
            foreach (T component in components)
            {
                Undo.RecordObject(
                    component,
                    $"Change {title}"
                );

                Set(component, value);

                EditorUtility.SetDirty(component);
            }
        }

        EditorGUI.showMixedValue = false;

        return value;
    }

    /*protected Диапазон ПолеСДиапазоном(string title, Expression<Func<T, Диапазон>> property)
    {
        PropertyInfo propertyInfo = GetPropertyInfo(property);

        Диапазон Get(T component)
        {
            return (Диапазон)propertyInfo.GetValue(component);
        }

        void Set(T component, Диапазон value)
        {
            propertyInfo.SetValue(component, value);
        }

        EditorGUI.showMixedValue = IsChanged(Get);

        EditorGUI.BeginChangeCheck();

        НачатьГоризонтальнуюГруппу();

        Диапазон value = Get(компонент);

        /*Диапазон value = drawer(
            new GUIContent(title),
            Get(component)
        );*/

        /*if (value.диапазон)
        {
            GUILayout.Label(title);
            GUILayout.Label(" от ", GUILayout.Width(25));
            value.минимальное = EditorGUILayout.FloatField(value.минимальное, GUILayout.Width(75));
            GUILayout.Label(" до ", GUILayout.Width(25));
            value.максимальное = EditorGUILayout.FloatField(value.максимальное, GUILayout.Width(75));
            if (МиниКнопка("↕", "Случайное"))
            {
                value.диапазон = false;
                //Set(component, value);
            }
        }
        else
        {
            value.минимальное = EditorGUILayout.FloatField(new GUIContent(title), value.минимальное);
            if (МиниКнопка("=", "Постоянное"))
            {
                value.диапазон = true;
                //Set(component, value);
            }
        }
        ЗакончитьГоризонтальнуюГруппу();

        if (EditorGUI.EndChangeCheck())
        {
            foreach (T component in components)
            {
                Undo.RecordObject(
                    component,
                    $"Change {title}"
                );

                Set(component, value);

                EditorUtility.SetDirty(component);
            }
        }

        EditorGUI.showMixedValue = false;

        return value;
    }*/

    protected (Дробное, Дробное) ПолеСДиапазоном(string title, Expression<Func<T, float>> propertyMin, Expression<Func<T, float>> propertyMax, Expression<Func<T, bool>> propertyMode)
    {
        PropertyInfo propertyMinInfo = GetPropertyInfo(propertyMin);
        PropertyInfo propertyMaxInfo = GetPropertyInfo(propertyMax);
        PropertyInfo propertyModeInfo = GetPropertyInfo(propertyMode);

        (Дробное, Дробное) Get(T component)
        {
            (Дробное, Дробное) диапазон = ((float)propertyMinInfo.GetValue(component), (float)propertyMaxInfo.GetValue(component));
            //диапазон.диапазон = (bool)propertyModeInfo.GetValue(component);
            return диапазон;
        }

        void Set(T component, (Дробное, Дробное) value)
        {
            propertyMinInfo.SetValue(component, value.Item1);
            propertyMaxInfo.SetValue(component, value.Item2);
            //propertyModeInfo.SetValue(component, value.диапазон);
        }

        EditorGUI.showMixedValue = IsChanged(Get);

        EditorGUI.BeginChangeCheck();

        НачатьГоризонтальнуюГруппу();

        (Дробное, Дробное) value = Get(компонент);
        GUIContent подпись = Подпись(title, propertyMinInfo.Name);
        Отметить(propertyMaxInfo.Name, propertyModeInfo.Name);

        /*Диапазон value = drawer(
            new GUIContent(title),
            Get(component)
        );*/

        //if (value.диапазон)
        //{
            GUILayout.Label(подпись);
            GUILayout.Label(" от ", GUILayout.Width(25));
            value.Item1 = EditorGUILayout.FloatField(value.Item1, GUILayout.Width(75));
            GUILayout.Label(" до ", GUILayout.Width(25));
            value.Item2 = EditorGUILayout.FloatField(value.Item2, GUILayout.Width(75));
            /*if (МиниКнопка("↕", "Случайное"))
            {
                value.диапазон = false;
                //Set(component, value);
            }*/
        //}
        /*else
        {
            value.минимальное = EditorGUILayout.FloatField(new GUIContent(title), value.минимальное);
            if (МиниКнопка("=", "Постоянное"))
            {
                value.диапазон = true;
                //Set(component, value);
            }
        }*/
        ЗакончитьГоризонтальнуюГруппу();

        if (EditorGUI.EndChangeCheck())
        {
            foreach (T component in components)
            {
                Undo.RecordObject(
                    component,
                    $"Change {title}"
                );

                Set(component, value);

                EditorUtility.SetDirty(component);
            }
        }

        EditorGUI.showMixedValue = false;

        return value;
    }

    protected bool2 СинхрополеСДвойнымBool(string title, Expression<Func<T, bool>> propertyFirst, Expression<Func<T, bool>> propertySecond)
    {
        PropertyInfo propertyFirstInfo = GetPropertyInfo(propertyFirst);
        PropertyInfo propertySecondInfo = GetPropertyInfo(propertySecond);

        bool2 Get(T component)
        {
            return new bool2(
                (bool)propertyFirstInfo.GetValue(component),
                (bool)propertySecondInfo.GetValue(component));
        }

        void Set(T component, bool2 value)
        {
            propertyFirstInfo.SetValue(component, value.x);
            propertySecondInfo.SetValue(component, value.y);
        }

        EditorGUI.showMixedValue = IsChanged(Get);

        EditorGUI.BeginChangeCheck();

        НачатьГоризонтальнуюГруппу();

        bool2 value = Get(компонент);
        /*GUILayout.Label(title);
        GUILayout.Label("X", GUILayout.Width(25));
        value.x = EditorGUILayout.Toggle(value.x, GUILayout.Width(25));
        GUILayout.Label("Y", GUILayout.Width(25));
        value.y = EditorGUILayout.Toggle(value.y);*/
        Rect rect = EditorGUILayout.GetControlRect();
        rect = EditorGUI.PrefixLabel(rect, Подпись(title, propertyFirstInfo.Name));
        Отметить(propertySecondInfo.Name);
        float width = 30f;
        EditorGUI.ToggleLeft(new Rect(rect.x, rect.y, width, rect.height),"X", value.x);
        EditorGUI.ToggleLeft(new Rect(rect.x + width + 10f, rect.y, width, rect.height),"Y", value.y);
        ЗакончитьГоризонтальнуюГруппу();

        if (EditorGUI.EndChangeCheck())
        {
            ChangeOriginals(
                $"Change {title}",
                component => component.оригинальныйКомпонент,
                component => Set(component, value)
            );
        }

        EditorGUI.showMixedValue = false;

        return value;
    }

    // Обязательное поле, которое ещё не заполнили, подсвечивается красным, например «Кого копирует» у Спавнера
    TValue НарисоватьСОбязательностью<TValue>(PropertyInfo propertyInfo, string title, TValue текущее, Func<GUIContent, TValue, TValue> drawer)
    {
        GUIContent подпись = Подпись(title, propertyInfo.Name);
        bool пустоеОбязательное = propertyInfo.IsDefined(typeof(ОбязательноеAttribute), true) && ОтрисовкаАтрибутов.Пустое(текущее);
        if (!пустоеОбязательное) return drawer(подпись, текущее);

        подпись.tooltip = ОтрисовкаАтрибутов.ПодсказкаОбязательного(подпись.tooltip);
        Color былЦвет = GUI.color;
        GUI.color = ОтрисовкаАтрибутов.цветОбязательного;
        TValue value = drawer(подпись, текущее);
        GUI.color = былЦвет;
        ОтрисовкаАтрибутов.Обвести(GUILayoutUtility.GetLastRect());
        return value;
    }
    PropertyInfo GetPropertyInfo<TValue>(Expression<Func<T, TValue>> property)
    {
        PropertyInfo propertyInfo = ((MemberExpression)property.Body).Member as PropertyInfo;
        if (propertyInfo == null)
            throw new ArgumentException("GUIField ожидает свойство.");
        return propertyInfo;
    }

    protected TValue Синхрополе<TValue>(string title, Expression<Func<T, TValue>> property, Func<GUIContent, TValue, TValue> drawer)
    {
        PropertyInfo propertyInfo = GetPropertyInfo(property);

        TValue Get(T component)
        {
            return (TValue)propertyInfo.GetValue(component);
        }

        void Set(T component, TValue value)
        {
            propertyInfo.SetValue(component, value);
        }

        EditorGUI.showMixedValue =
            IsChanged(Get);

        EditorGUI.BeginChangeCheck();

        TValue value = НарисоватьСОбязательностью(propertyInfo, title, Get(компонент), drawer);

        if (EditorGUI.EndChangeCheck())
        {
            ChangeOriginals(
                $"Change {title}",
                component => component.оригинальныйКомпонент,
                component => Set(component, value)
            );
        }

        EditorGUI.showMixedValue = false;

        return value;
    }

    protected TEnum EnumField<TEnum>(string title, Expression<Func<T, TEnum>> property, Action<T, TEnum> setter) where TEnum : Enum
    {
        Func<T, TEnum> getter = property.Compile();
        GUIContent подпись = Подпись(title, GetPropertyInfo(property).Name);

        EditorGUI.showMixedValue =
            IsChanged(getter);

        EditorGUI.BeginChangeCheck();

        TEnum value = (TEnum)EditorGUILayout.EnumPopup(
            подпись,
            getter(компонент)
        );

        if (EditorGUI.EndChangeCheck())
        {
            ChangeOriginals(
                $"Change {title}",
                component => component.оригинальныйКомпонент,
                объект => setter(объект, value)
            );
        }

        EditorGUI.showMixedValue = false;

        return value;
    }
}
#endif