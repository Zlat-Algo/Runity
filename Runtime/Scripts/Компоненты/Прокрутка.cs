#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Область интерфейса, содержимое которой можно прокручивать: список уровней, чат, длинный текст
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(ScrollRect))]
[AddComponentMenu("  Runity/ Интерфейс/ Основа/Прокрутка")]
public class Прокрутка : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<ScrollRect>();
    void OnValidate() => Awake();
    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public ScrollRect оригинал => (ScrollRect)оригинальныйКомпонент;

    [SerializeField] События _приПрокрутке;
    /// <summary>
    /// Срабатывает при каждом движении содержимого
    /// </summary>
    public События приПрокрутке => _приПрокрутке;

    void Start()
    {
        оригинал.onValueChanged.AddListener(_ => _приПрокрутке.Вызвать());
    }

    /// <summary>
    /// Можно ли прокручивать вверх и вниз
    /// </summary>
    public Флажок вертикальная
    {
        get => оригинал.vertical;
        set => оригинал.vertical = value;
    }

    /// <summary>
    /// Можно ли прокручивать влево и вправо
    /// </summary>
    public Флажок горизонтальная
    {
        get => оригинал.horizontal;
        set => оригинал.horizontal = value;
    }

    /// <summary>
    /// Если "да", то содержимое продолжает ехать по инерции после того, как его отпустили
    /// </summary>
    public Флажок инерция
    {
        get => оригинал.inertia;
        set => оригинал.inertia = value;
    }

    /// <summary>
    /// Если "да", то на краях содержимое пружинит. Если "нет", то просто упирается
    /// </summary>
    public Флажок пружинитНаКраях
    {
        get => оригинал.movementType == ScrollRect.MovementType.Elastic;
        set => оригинал.movementType = value ? ScrollRect.MovementType.Elastic : ScrollRect.MovementType.Clamped;
    }

    /// <summary>
    /// Насколько прокручено вниз: 0 - в самом верху, 1 - в самом низу
    /// </summary>
    public Дробное прокрученоВниз
    {
        get => 1 - оригинал.verticalNormalizedPosition;
        set => оригинал.verticalNormalizedPosition = 1 - Mathf.Clamp01(value);
    }

    /// <summary>
    /// Насколько прокручено вправо: 0 - у левого края, 1 - у правого
    /// </summary>
    public Дробное прокрученоВправо
    {
        get => оригинал.horizontalNormalizedPosition;
        set => оригинал.horizontalNormalizedPosition = Mathf.Clamp01(value);
    }

    /// <summary>
    /// Объект, внутри которого лежат прокручиваемые элементы. Новые элементы создавайте в нём
    /// </summary>
    public Transform содержимое => оригинал.content;

    /// <summary>
    /// Прокручивает в самый верх
    /// </summary>
    public void ВНачало() => прокрученоВниз = 0;

    /// <summary>
    /// Прокручивает в самый низ. Удобно для чатов
    /// </summary>
    public void ВКонец() => прокрученоВниз = 1;
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Прокрутка))]
internal class ПрокруткаEditor : RunityEditor<Прокрутка>
{
    protected override void Отрисовать()
    {
        Текст("Содержимое этого объекта можно прокручивать колёсиком мыши или перетаскиванием. Элементы кладите внутрь объекта Content");

        Пробел();

        Синхрополе("Вертикальная", x => x.вертикальная,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Горизонтальная", x => x.горизонтальная,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Инерция", x => x.инерция,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Пружинит на краях", x => x.пружинитНаКраях,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При прокрутке", "_приПрокрутке");
        ЗакончитьСобытия();
    }
}
#endif
