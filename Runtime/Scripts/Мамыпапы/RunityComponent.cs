using System;
using System.Linq;
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
        ОтрисоватьПрочее();
        Справка.ОтобразитьВИнспекторе(target.GetType(), видимые);
    }

    // Три уровня настроек: сверху - главное (рисует сам компонент), в «Прочем» - все остальные настройки этого компонента
    // и компонентов Unity, на которых он построен, а в «Скрытом» - то, что доступно только из кода и событий
    readonly System.Collections.Generic.Dictionary<Type, Editor> встроенные = new System.Collections.Generic.Dictionary<Type, Editor>();

    // Эти компоненты Unity слишком сложные для простого списка настроек - для них показывается их собственный инспектор
    static readonly System.Collections.Generic.HashSet<string> сложные = new System.Collections.Generic.HashSet<string> { "ParticleSystem", "Animator", "PlayerInput", "NetworkManager", "UnityTransport", "NetworkObject", "VideoPlayer", "TilemapRenderer" };

    void ОтрисоватьПрочее()
    {
        serializedObject.Update();
        System.Collections.Generic.List<SerializedProperty> свои = new System.Collections.Generic.List<SerializedProperty>();
        SerializedProperty свойство = serializedObject.GetIterator();
        for (bool есть = свойство.NextVisible(true); есть; есть = свойство.NextVisible(false))
        {
            string имя = свойство.name.TrimStart('_');
            if (свойство.name == "m_Script" || видимые.Contains(имя)) continue;
            свои.Add(свойство.Copy());
            видимые.Add(имя);
        }

        System.Collections.Generic.List<Type> оригиналы = ТипыОригиналов();
        if (свои.Count == 0 && оригиналы.Count == 0) return;

        string ключ = "Runity.Прочее." + target.GetType().Name;
        bool открыто = SessionState.GetBool(ключ, false);
        bool новое = EditorGUILayout.Foldout(открыто, new GUIContent("Прочее", "Все остальные настройки: этого компонента и компонентов Unity, на которых он построен"), true);
        if (новое != открыто) SessionState.SetBool(ключ, новое);
        if (!новое) return;

        EditorGUI.indentLevel++;
        if (свои.Count > 0)
        {
            foreach (SerializedProperty поле in свои)
            {
                string имя = поле.name.TrimStart('_');
                EditorGUILayout.PropertyField(поле, new GUIContent(ПереводНастроек.ИзИмени(поле.name), Справка.Описание(target.GetType(), имя)), true);
            }
            serializedObject.ApplyModifiedProperties();
        }

        foreach (Type тип in оригиналы)
        {
            UnityEngine.Object[] части = components.Select(к => (UnityEngine.Object)к.GetComponent(тип)).Where(к => к != null).ToArray();
            if (части.Length == 0) continue;

            Пробел();
            string русское = АналогиRunity.Аналог(тип) is Type аналог ? ObjectNames.NicifyVariableName(аналог.Name) : ObjectNames.NicifyVariableName(тип.Name);
            EditorGUILayout.LabelField(new GUIContent(русское, $"Настройки компонента Unity {тип.Name}"), EditorStyles.boldLabel);

            if (сложные.Contains(тип.Name))
            {
                встроенные.TryGetValue(тип, out Editor редактор);
                Editor.CreateCachedEditor(части, null, ref редактор);
                встроенные[тип] = редактор;
                редактор.OnInspectorGUI();
                continue;
            }

            SerializedObject данные = new SerializedObject(части);
            данные.Update();
            SerializedProperty настройка = данные.GetIterator();
            for (bool есть = настройка.NextVisible(true); есть; есть = настройка.NextVisible(false))
            {
                if (настройка.name == "m_Script" || настройка.name == "m_ObjectHideFlags") continue;
                EditorGUILayout.PropertyField(настройка, new GUIContent(ПереводНастроек.Перевести(настройка.displayName), $"{настройка.displayName} - так эта настройка называется в Unity"), true);
            }
            данные.ApplyModifiedProperties();
        }
        EditorGUI.indentLevel--;
    }

    // Компоненты Unity, на которых построен этот компонент Runity: его оригинал и всё, что он требует
    System.Collections.Generic.List<Type> ТипыОригиналов()
    {
        System.Collections.Generic.List<Type> типы = new System.Collections.Generic.List<Type>();
        void Добавить(Component найденный)
        {
            if (найденный == null || найденный is Компонент || найденный is Transform || типы.Contains(найденный.GetType())) return;
            типы.Add(найденный.GetType());
        }

        Добавить(компонент.оригинальныйКомпонент);
        foreach (RequireComponent требование in target.GetType().GetCustomAttributes(typeof(RequireComponent), true))
        {
            foreach (Type нужный in new[] { требование.m_Type0, требование.m_Type1, требование.m_Type2 })
                if (нужный != null) Добавить(компонент.GetComponent(нужный));
        }
        return типы;
    }

    void OnDisable()
    {
        foreach (Editor редактор in встроенные.Values) if (редактор != null) DestroyImmediate(редактор);
        встроенные.Clear();
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

    // Поле материала коллайдера: принимает и физический материал Unity, и Физический Материал Runity
    protected PhysicsMaterial ПолеФизическогоМатериала(GUIContent title, PhysicsMaterial value)
    {
        UnityEngine.Object новое = EditorGUILayout.ObjectField(title, ВладелецМатериала(value), typeof(UnityEngine.Object), false);
        if (новое is ФизическийМатериал русский) return русский.для3D;
        return новое == null ? null : новое as PhysicsMaterial ?? value;
    }

    protected PhysicsMaterial2D ПолеФизическогоМатериала2D(GUIContent title, PhysicsMaterial2D value)
    {
        UnityEngine.Object новое = EditorGUILayout.ObjectField(title, ВладелецМатериала(value), typeof(UnityEngine.Object), false);
        if (новое is ФизическийМатериал русский) return русский.для2D;
        return новое == null ? null : новое as PhysicsMaterial2D ?? value;
    }

    // Если материал Unity лежит внутри Физического Материала Runity, в поле показывается сам Физический Материал
    static UnityEngine.Object ВладелецМатериала(UnityEngine.Object материал)
    {
        if (материал == null) return null;
        string путь = AssetDatabase.GetAssetPath(материал);
        UnityEngine.Object главный = string.IsNullOrEmpty(путь) ? null : AssetDatabase.LoadMainAssetAtPath(путь);
        return главный is ФизическийМатериал ? главный : материал;
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

    // Отражение не умеет неявные преобразования: Дробное в float, Число в int, Флажок в bool и обратно.
    // Поэтому все значения полей читаются и записываются через эти функции
    protected static void Записать(PropertyInfo свойство, object цель, object значение) => свойство.SetValue(цель, Привести(значение, свойство.PropertyType));

    protected static object Привести(object значение, Type тип)
    {
        if (значение == null || тип.IsInstanceOfType(значение)) return значение;
        Type из = значение.GetType();

        MethodInfo оператор = Оператор(из, тип);
        if (оператор != null) return оператор.Invoke(null, new[] { значение });

        // Через простой тип: например, Дробное -> float -> double
        foreach (Type промежуточный in new[] { typeof(float), typeof(int), typeof(bool), typeof(string), typeof(double) })
        {
            MethodInfo первый = Оператор(из, промежуточный);
            if (первый == null) continue;
            object простое = первый.Invoke(null, new[] { значение });
            if (тип.IsInstanceOfType(простое)) return простое;
            MethodInfo второй = Оператор(промежуточный, тип);
            if (второй != null) return второй.Invoke(null, new[] { простое });
            if (тип.IsPrimitive && промежуточный.IsPrimitive) return Convert.ChangeType(простое, тип);
        }

        if (тип.IsEnum && из.IsPrimitive) return Enum.ToObject(тип, значение);
        return Convert.ChangeType(значение, тип);
    }

    static MethodInfo Оператор(Type из, Type в)
    {
        foreach (Type владелец in new[] { в, из })
        {
            foreach (MethodInfo метод in владелец.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if ((метод.Name == "op_Implicit" || метод.Name == "op_Explicit") && метод.ReturnType == в && метод.GetParameters()[0].ParameterType == из)
                    return метод;
            }
        }
        return null;
    }

    protected TValue Поле<TValue>(string title, Expression<Func<T, TValue>> property, Func<GUIContent, TValue, TValue> drawer)
    {
        PropertyInfo propertyInfo = GetPropertyInfo(property);

        TValue Get(T component)
        {
            return (TValue)Привести(propertyInfo.GetValue(component), typeof(TValue));
        }

        void Set(T component, TValue value)
        {
            Записать(propertyInfo, component, value);
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
            Записать(propertyInfo, component, value);
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
            (Дробное, Дробное) диапазон = ((float)Привести(propertyMinInfo.GetValue(component), typeof(float)), (float)Привести(propertyMaxInfo.GetValue(component), typeof(float)));
            //диапазон.диапазон = (bool)propertyModeInfo.GetValue(component);
            return диапазон;
        }

        void Set(T component, (Дробное, Дробное) value)
        {
            Записать(propertyMinInfo, component, value.Item1);
            Записать(propertyMaxInfo, component, value.Item2);
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
                (bool)Привести(propertyFirstInfo.GetValue(component), typeof(bool)),
                (bool)Привести(propertySecondInfo.GetValue(component), typeof(bool)));
        }

        void Set(T component, bool2 value)
        {
            Записать(propertyFirstInfo, component, value.x);
            Записать(propertySecondInfo, component, value.y);
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
        // Если тип поля и свойства разный (например, float и Дробное), C# оборачивает свойство в преобразование
        Expression тело = property.Body;
        while (тело is UnaryExpression преобразование && (тело.NodeType == ExpressionType.Convert || тело.NodeType == ExpressionType.ConvertChecked)) тело = преобразование.Operand;
        PropertyInfo propertyInfo = (тело as MemberExpression)?.Member as PropertyInfo;
        if (propertyInfo == null)
            throw new ArgumentException("GUIField ожидает свойство.");
        return propertyInfo;
    }

    protected TValue Синхрополе<TValue>(string title, Expression<Func<T, TValue>> property, Func<GUIContent, TValue, TValue> drawer)
    {
        PropertyInfo propertyInfo = GetPropertyInfo(property);

        TValue Get(T component)
        {
            return (TValue)Привести(propertyInfo.GetValue(component), typeof(TValue));
        }

        void Set(T component, TValue value)
        {
            Записать(propertyInfo, component, value);
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