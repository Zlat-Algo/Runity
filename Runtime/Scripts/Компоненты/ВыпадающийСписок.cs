using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Выпадающий список вариантов в интерфейсе: сложность, разрешение экрана, язык
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Dropdown))]
[AddComponentMenu("  Runity/ Интерфейс/ Элементы/Выпадающий список")]
public class ВыпадающийСписок : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<Dropdown>();
    void OnValidate() => Awake();
    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public Dropdown оригинал => (Dropdown)оригинальныйКомпонент;

    [SerializeField] События _приИзменении;
    /// <summary>
    /// Срабатывает, когда игрок выбрал другой вариант
    /// </summary>
    public События приИзменении => _приИзменении;

    void Start()
    {
        оригинал.onValueChanged.AddListener(_ => _приИзменении.Вызвать());
    }

    /// <summary>
    /// Все варианты по порядку
    /// </summary>
    public List<string> варианты
    {
        get => оригинал.options.Select(вариант => вариант.text).ToList();
        set
        {
            оригинал.ClearOptions();
            оригинал.AddOptions(value);
            оригинал.RefreshShownValue();
        }
    }

    /// <summary>
    /// Номер выбранного варианта, считая с нуля
    /// </summary>
    public Число выбранныйНомер
    {
        get => оригинал.value;
        set => оригинал.value = value;
    }

    /// <summary>
    /// Текст выбранного варианта
    /// </summary>
    public Строка выбранныйТекст => оригинал.options.Count > 0 ? оригинал.options[оригинал.value].text : "";

    /// <summary>
    /// Если "нет", то список станет серым и перестанет открываться
    /// </summary>
    public Флажок активен
    {
        get => оригинал.interactable;
        set => оригинал.interactable = value;
    }

    /// <summary>
    /// Добавляет вариант в конец списка
    /// </summary>
    public void ДобавитьВариант(Строка текст)
    {
        оригинал.options.Add(new Dropdown.OptionData(текст));
        оригинал.RefreshShownValue();
    }

    /// <summary>
    /// Выбирает вариант с таким текстом. Если его нет, ничего не меняет
    /// </summary>
    public void Выбрать(Строка текст)
    {
        int номер = оригинал.options.FindIndex(вариант => вариант.text == текст);
        if (номер >= 0) оригинал.value = номер;
    }

    /// <summary>
    /// Убирает все варианты
    /// </summary>
    public void Очистить() => оригинал.ClearOptions();
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(ВыпадающийСписок))]
internal class ВыпадающийСписокEditor : RunityEditor<ВыпадающийСписок>
{
    protected override void Отрисовать()
    {
        Текст("В этом списке игрок может выбрать один из вариантов");

        Пробел();

        EditorGUILayout.LabelField(Подпись("Варианты", nameof(ВыпадающийСписок.варианты)), EditorStyles.boldLabel);

        List<string> варианты = компонент.варианты;
        bool изменено = false;
        int удалить = -1;

        for (int i = 0; i < варианты.Count; i++)
        {
            НачатьГоризонтальнуюГруппу();
            string новый = EditorGUILayout.TextField($"{i}", варианты[i]);
            if (новый != варианты[i])
            {
                варианты[i] = новый;
                изменено = true;
            }
            if (МиниКнопка("-", "Убрать вариант")) удалить = i;
            ЗакончитьГоризонтальнуюГруппу();
        }

        if (удалить >= 0)
        {
            варианты.RemoveAt(удалить);
            изменено = true;
        }

        if (GUILayout.Button(Подпись("Добавить вариант", nameof(ВыпадающийСписок.ДобавитьВариант))))
        {
            варианты.Add($"Вариант {варианты.Count + 1}");
            изменено = true;
        }

        if (изменено)
        {
            foreach (ВыпадающийСписок список in components)
            {
                Undo.RecordObject(список.оригинал, "Варианты списка");
                список.варианты = варианты;
                EditorUtility.SetDirty(список.оригинал);
            }
        }

        Пробел();

        Синхрополе("Выбранный номер", x => x.выбранныйНомер,
            (title, value) => EditorGUILayout.IntSlider(title, value, 0, Mathf.Max(0, варианты.Count - 1)));

        Синхрополе("Активен", x => x.активен,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При изменении", "_приИзменении");
        ЗакончитьСобытия();
    }
}
#endif
