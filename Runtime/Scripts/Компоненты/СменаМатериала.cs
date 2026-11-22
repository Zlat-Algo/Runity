using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// Один вариант внешности: название и материал. Подходит и обычный материал Unity, и Материал Runity
/// </summary>
[Serializable]
public class ВариантМатериала
{
    /// <summary>
    /// Как называется вариант, например «Красный скин» или «Сломан»
    /// </summary>
    public string название = "";

    /// <summary>
    /// Материал этого варианта: Material Unity или Материал Runity
    /// </summary>
    public Object материал;

    /// <summary>
    /// Настоящий материал Unity этого варианта
    /// </summary>
    public Material Получить() => материал is Материал русский ? (Material)русский : материал as Material;
}

/// <summary>
/// Меняет материал объекта на один из заранее заданных вариантов. Подходит для скинов, а ещё чтобы показывать состояния: эмоции, поломку, подсветку, команду
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Визуал/Смена Материала")]
public class СменаМатериала : RunityComponent
{
    protected override void OnEnable() { }
    protected override void OnDisable() { }

    [SerializeField] List<ВариантМатериала> _варианты = new List<ВариантМатериала>();
    /// <summary>
    /// Варианты внешности по порядку. Номера считаются с нуля
    /// </summary>
    public List<ВариантМатериала> варианты => _варианты;

    [SerializeField] int _номер;
    /// <summary>
    /// Номер варианта, который показывается сейчас. Если записать другой номер, внешность сменится
    /// </summary>
    public Число номер { get => _номер; set => Выбрать(value); }

    [SerializeField] Флажок _вместеСДетьми = да;
    /// <summary>
    /// Если "да", материал меняется и у всех частей внутри объекта, например у всей модели персонажа
    /// </summary>
    public Флажок вместеСДетьми { get => _вместеСДетьми; set => _вместеСДетьми = value; }

    [SerializeField] int _слот = -1;
    /// <summary>
    /// Какой по счёту материал менять, если у части их несколько. -1 - все
    /// </summary>
    public Число слот { get => _слот; set => _слот = value; }

    [SerializeField] Строка _сохранятьПодИменем = "";
    /// <summary>
    /// Если указать имя, выбранный вариант запоминается между запусками игры. Так работают скины. Пусто - не запоминать
    /// </summary>
    public Строка сохранятьПодИменем { get => _сохранятьПодИменем; set => _сохранятьПодИменем = value; }

    [SerializeField] События _приСмене;
    /// <summary>
    /// Срабатывает, когда внешность сменилась
    /// </summary>
    public События приСмене => _приСмене;

    /// <summary>
    /// Сколько всего вариантов
    /// </summary>
    public Число количество => варианты.Count;

    /// <summary>
    /// Название варианта, который показывается сейчас
    /// </summary>
    public Строка название => _номер >= 0 && _номер < варианты.Count ? варианты[_номер].название : "";

    void Start()
    {
        if (!string.IsNullOrEmpty(сохранятьПодИменем) && Сохранения.Есть(сохранятьПодИменем))
        {
            int сохранённый = варианты.FindIndex(в => в.название == (string)Сохранения.ЗагрузитьСтроку(сохранятьПодИменем));
            if (сохранённый >= 0) _номер = сохранённый;
        }
        Применить();
    }

    /// <summary>
    /// Показывает вариант с указанным номером
    /// </summary>
    public void Выбрать(Число номерВарианта)
    {
        if (варианты.Count == 0) return;
        _номер = Mathf.Clamp(номерВарианта, 0, варианты.Count - 1);
        Применить();
        if (!string.IsNullOrEmpty(сохранятьПодИменем) && Application.isPlaying) Сохранения.СохранитьСтроку(сохранятьПодИменем, название);
        if (Application.isPlaying) _приСмене.Вызвать();
    }

    /// <summary>
    /// Показывает вариант с указанным названием
    /// </summary>
    public void Выбрать(Строка названиеВарианта)
    {
        int найденный = варианты.FindIndex(в => в.название == (string)названиеВарианта);
        if (найденный >= 0) Выбрать((Число)найденный);
        else Консоль.НапечататьПредупреждение($"У Смены материала нет варианта «{названиеВарианта}»", this);
    }

    /// <summary>
    /// Показывает следующий вариант, а после последнего - снова первый
    /// </summary>
    public void Следующий() { if (варианты.Count > 0) Выбрать((Число)((_номер + 1) % варианты.Count)); }

    /// <summary>
    /// Показывает предыдущий вариант, а перед первым - последний
    /// </summary>
    public void Предыдущий() { if (варианты.Count > 0) Выбрать((Число)((_номер - 1 + варианты.Count) % варианты.Count)); }

    /// <summary>
    /// Показывает случайный вариант
    /// </summary>
    public void Случайный() { if (варианты.Count > 0) Выбрать((Число)UnityEngine.Random.Range(0, варианты.Count)); }

    // Материалы ставятся общими (shared): так не создаются лишние копии и в редакторе ничего не утекает
    void Применить()
    {
        if (_номер < 0 || _номер >= варианты.Count) return;
        Material материал = варианты[_номер].Получить();
        if (материал == null) return;

        foreach (Renderer отрисовщик in вместеСДетьми ? GetComponentsInChildren<Renderer>(true) : GetComponents<Renderer>())
        {
            if (отрисовщик is ParticleSystemRenderer) continue;
            Material[] набор = отрисовщик.sharedMaterials;
            for (int i = 0; i < набор.Length; i++)
                if (слот < 0 || i == слот) набор[i] = материал;
            отрисовщик.sharedMaterials = набор;
        }
        foreach (Graphic картинка in вместеСДетьми ? GetComponentsInChildren<Graphic>(true) : GetComponents<Graphic>())
        {
            картинка.material = материал;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying && варианты.Count > 0) EditorApplication.delayCall += () => { if (this != null) Применить(); };
    }
#endif
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(СменаМатериала))]
internal class СменаМатериалаEditor : RunityEditor<СменаМатериала>
{
    ReorderableList список;

    protected override void Отрисовать()
    {
        Текст("Этот объект меняет свой материал на один из вариантов: для скинов и для состояний вроде «Цел» и «Сломан»");

        Пробел();

        serializedObject.Update();
        if (список == null)
        {
            список = new ReorderableList(serializedObject, serializedObject.FindProperty("_варианты"), true, true, true, true);
            список.drawHeaderCallback = место => EditorGUI.LabelField(место, Подпись("Варианты", nameof(СменаМатериала.варианты)));
            список.elementHeight = EditorGUIUtility.singleLineHeight + 4;
            список.drawElementCallback = (место, номер, активный, фокус) =>
            {
                SerializedProperty вариант = список.serializedProperty.GetArrayElementAtIndex(номер);
                место.y += 2;
                место.height = EditorGUIUtility.singleLineHeight;
                Rect имя = new Rect(место.x, место.y, 24, место.height);
                Rect название = new Rect(место.x + 26, место.y, место.width * 0.4f - 26, место.height);
                Rect материал = new Rect(место.x + место.width * 0.4f + 4, место.y, место.width * 0.6f - 4, место.height);
                EditorGUI.LabelField(имя, номер.ToString());
                EditorGUI.PropertyField(название, вариант.FindPropertyRelative("название"), GUIContent.none);
                SerializedProperty поле = вариант.FindPropertyRelative("материал");
                Object выбранный = EditorGUI.ObjectField(материал, поле.objectReferenceValue, typeof(Object), false);
                // Подходят только материалы: Unity и Runity
                if (выбранный == null || выбранный is Material || выбранный is Материал) поле.objectReferenceValue = выбранный;
            };
        }
        список.DoLayoutList();
        serializedObject.ApplyModifiedProperties();

        if (компонент.количество > 0)
        {
            Поле("Номер", x => x.номер, (title, value) => EditorGUILayout.IntSlider(title, value, 0, компонент.количество - 1));
        }
        else
        {
            Отметить(nameof(СменаМатериала.номер));
            Текст("Добавьте варианты: название и материал. Можно перетащить и Материал Runity", MessageType.Info);
        }

        Поле("Вместе с детьми", x => x.вместеСДетьми, (title, value) => EditorGUILayout.Toggle(title, value));
        Поле("Слот", x => x.слот, (title, value) => EditorGUILayout.IntField(title, value));
        Поле("Сохранять под именем", x => x.сохранятьПодИменем, (title, value) => EditorGUILayout.TextField(title, value));

        Пробел();

        НачатьГоризонтальнуюГруппу();
        Кнопка("Предыдущий", x => x.Предыдущий());
        Кнопка("Следующий", x => x.Следующий());
        ЗакончитьГоризонтальнуюГруппу();

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При смене", "_приСмене");
        ЗакончитьСобытия();
    }
}
#endif
