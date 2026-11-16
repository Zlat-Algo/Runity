#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Где рисуется интерфейс: поверх экрана, перед камерой или в мире
/// </summary>
public enum РежимИнтерфейса { ПоверхЭкрана, ПередКамерой, ВМире }

/// <summary>
/// Как интерфейс подстраивается под экран, пропорции которого отличаются от эталонного разрешения
/// </summary>
public enum ПодстройкаПодЭкран { ВсёВидно, БезПустыхКраёв, ПоШирине, ПоВысоте }

/// <summary>
/// Основа для всего интерфейса: внутри этого объекта создаются текст, кнопки, слайдеры и другие элементы
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
[AddComponentMenu("  Runity/ Интерфейс/ Основа/Интерфейс")]
public class Интерфейс : RunityComponent
{
    void Awake()
    {
        оригинальныйКомпонент = GetComponent<Canvas>();
        if (Application.isPlaying)
        {
            ПроверитьСистемуСобытий();
        }
    }
    void OnValidate() => оригинальныйКомпонент = GetComponent<Canvas>();
    void Reset()
    {
        оригинальныйКомпонент = GetComponent<Canvas>();
        СтандартныеНастройки();
    }
    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public Canvas оригинал => (Canvas)оригинальныйКомпонент;
    CanvasScaler масштабатор => GetComponent<CanvasScaler>();

    // Настройки хранятся в другом компоненте, поэтому его нужно отметить изменённым, иначе сцена их не сохранит
    void ОтметитьИзменение()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && масштабатор != null) EditorUtility.SetDirty(масштабатор);
#endif
    }

    /// <summary>
    /// Если "да", то интерфейс рисуется прямо в мире, а не поверх экрана. Подходит для полосок здоровья над головой
    /// </summary>
    public Флажок вМире
    {
        get => оригинал.renderMode == RenderMode.WorldSpace;
        set => оригинал.renderMode = value ? RenderMode.WorldSpace : RenderMode.ScreenSpaceOverlay;
    }

    /// <summary>
    /// Где рисуется интерфейс. Поверх экрана - над всем остальным. Перед камерой - на заданном расстоянии от камеры: так на интерфейс действуют эффекты камеры, а 3D-объекты могут оказаться перед ним. В мире - как обычный объект на сцене
    /// </summary>
    public РежимИнтерфейса режим
    {
        get => оригинал.renderMode switch
        {
            RenderMode.ScreenSpaceCamera => РежимИнтерфейса.ПередКамерой,
            RenderMode.WorldSpace => РежимИнтерфейса.ВМире,
            _ => РежимИнтерфейса.ПоверхЭкрана
        };
        set
        {
            оригинал.renderMode = value switch
            {
                РежимИнтерфейса.ПередКамерой => RenderMode.ScreenSpaceCamera,
                РежимИнтерфейса.ВМире => RenderMode.WorldSpace,
                _ => RenderMode.ScreenSpaceOverlay
            };
            if (value == РежимИнтерфейса.ПередКамерой && оригинал.worldCamera == null) оригинал.worldCamera = Camera.main;
        }
    }

    /// <summary>
    /// Через какую камеру смотрит интерфейс в режиме «Перед камерой» и кто нажимает на него в режиме «В мире». Если пусто - главная камера
    /// </summary>
    public Camera камера
    {
        get => оригинал.worldCamera;
        set => оригинал.worldCamera = value;
    }

    /// <summary>
    /// На каком расстоянии от камеры висит интерфейс в режиме «Перед камерой»
    /// </summary>
    public Дробное расстояниеОтКамеры
    {
        get => оригинал.planeDistance;
        set => оригинал.planeDistance = Mathf.Max(0.01f, value);
    }

    /// <summary>
    /// Если "да", картинки и текст выравниваются по пикселям экрана и не размываются. Подходит для пиксельной графики
    /// </summary>
    public Флажок попиксельно
    {
        get => оригинал.pixelPerfect;
        set => оригинал.pixelPerfect = value;
    }

    /// <summary>
    /// Какой интерфейс рисуется поверх других: чем больше число, тем выше
    /// </summary>
    public Число порядокОтрисовки
    {
        get => оригинал.sortingOrder;
        set => оригинал.sortingOrder = value;
    }

    /// <summary>
    /// Если "да", то интерфейс выглядит одинаково на любых экранах
    /// </summary>
    public Флажок подстраиватьПодЭкран
    {
        get => масштабатор.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize;
        set
        {
            масштабатор.uiScaleMode = value ? CanvasScaler.ScaleMode.ScaleWithScreenSize : CanvasScaler.ScaleMode.ConstantPixelSize;
            ОтметитьИзменение();
        }
    }

    /// <summary>
    /// Что делать, если пропорции экрана другие. Всё видно - интерфейс целиком помещается на экран. Без пустых краёв - заполняет экран, но края могут обрезаться. По ширине и По высоте - подгоняет по одной стороне
    /// </summary>
    public ПодстройкаПодЭкран подстройка
    {
        get
        {
            if (масштабатор.screenMatchMode == CanvasScaler.ScreenMatchMode.Expand) return ПодстройкаПодЭкран.ВсёВидно;
            if (масштабатор.screenMatchMode == CanvasScaler.ScreenMatchMode.Shrink) return ПодстройкаПодЭкран.БезПустыхКраёв;
            return масштабатор.matchWidthOrHeight < 0.5f ? ПодстройкаПодЭкран.ПоШирине : ПодстройкаПодЭкран.ПоВысоте;
        }
        set
        {
            масштабатор.screenMatchMode = value switch
            {
                ПодстройкаПодЭкран.ВсёВидно => CanvasScaler.ScreenMatchMode.Expand,
                ПодстройкаПодЭкран.БезПустыхКраёв => CanvasScaler.ScreenMatchMode.Shrink,
                _ => CanvasScaler.ScreenMatchMode.MatchWidthOrHeight
            };
            if (value == ПодстройкаПодЭкран.ПоШирине) масштабатор.matchWidthOrHeight = 0;
            if (value == ПодстройкаПодЭкран.ПоВысоте) масштабатор.matchWidthOrHeight = 1;
            ОтметитьИзменение();
        }
    }

    /// <summary>
    /// Под какой размер экрана вы делаете интерфейс
    /// </summary>
    public Направление2D эталонноеРазрешение
    {
        get => масштабатор.referenceResolution;
        set
        {
            масштабатор.referenceResolution = value;
            ОтметитьИзменение();
        }
    }

    /// <summary>
    /// Показывает интерфейс
    /// </summary>
    public void Показать() => оригинал.enabled = да;

    /// <summary>
    /// Прячет интерфейс
    /// </summary>
    public void Спрятать() => оригинал.enabled = нет;

    /// <summary>
    /// Прячет интерфейс, если он виден, и показывает, если спрятан
    /// </summary>
    public void Переключить() => оригинал.enabled = !оригинал.enabled;

    /// <summary>
    /// Настраивает интерфейс так, чтобы он подходил большинству игр
    /// </summary>
    public void СтандартныеНастройки()
    {
        вМире = нет;
        подстраиватьПодЭкран = да;
        эталонноеРазрешение = new Направление2D(1920, 1080);
        подстройка = ПодстройкаПодЭкран.ВсёВидно;
    }

    /// <summary>
    /// Без системы событий интерфейс не реагирует на мышку. Создаёт её во время игры, если на сцене её ещё нет
    /// </summary>
    public static void ПроверитьСистемуСобытий()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;

        GameObject система = new GameObject("Система событий");
        система.SetActive(false);
        система.AddComponent<EventSystem>();
        InputSystemUIInputModule модуль = система.AddComponent<InputSystemUIInputModule>();

        // AssignDefaultActions хранит общий набор кнопок, а Unity выгружает его при смене сцены,
        // поэтому у каждой системы событий свой набор
        DefaultInputActions кнопки = new DefaultInputActions();
        модуль.actionsAsset = кнопки.asset;
        модуль.point = InputActionReference.Create(кнопки.UI.Point);
        модуль.leftClick = InputActionReference.Create(кнопки.UI.Click);
        модуль.rightClick = InputActionReference.Create(кнопки.UI.RightClick);
        модуль.middleClick = InputActionReference.Create(кнопки.UI.MiddleClick);
        модуль.scrollWheel = InputActionReference.Create(кнопки.UI.ScrollWheel);
        модуль.move = InputActionReference.Create(кнопки.UI.Navigate);
        модуль.submit = InputActionReference.Create(кнопки.UI.Submit);
        модуль.cancel = InputActionReference.Create(кнопки.UI.Cancel);

        система.SetActive(true);
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Интерфейс))]
internal class ИнтерфейсEditor : RunityEditor<Интерфейс>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект - основа интерфейса. Создавайте внутри него Текст, Кнопки, Слайдеры и другие элементы через меню Runity - Интерфейс");

        Пробел();

        Отметить(nameof(Интерфейс.вМире));
        Синхрополе("Режим", x => x.режим,
            (title, value) => (РежимИнтерфейса)EditorGUILayout.EnumPopup(title, value));
        if (компонент.режим != РежимИнтерфейса.ПоверхЭкрана)
        {
            Синхрополе("Камера", x => x.камера,
                (title, value) => ПолеОбъекта<Camera>(title, value, true));
        }
        if (компонент.режим == РежимИнтерфейса.ПередКамерой)
        {
            Синхрополе("Расстояние от камеры", x => x.расстояниеОтКамеры,
                (title, value) => EditorGUILayout.FloatField(title, value));
        }
        if (компонент.режим != РежимИнтерфейса.ВМире)
        {
            Синхрополе("Попиксельно", x => x.попиксельно,
                (title, value) => EditorGUILayout.Toggle(title, value));
        }

        Синхрополе("Порядок отрисовки", x => x.порядокОтрисовки,
            (title, value) => EditorGUILayout.IntField(title, value));

        Пробел();

        Поле("Подстраивать под экран", x => x.подстраиватьПодЭкран,
            (title, value) => EditorGUILayout.Toggle(title, value));

        if (компонент.подстраиватьПодЭкран)
        {
            Поле("Эталонное разрешение", x => x.эталонноеРазрешение,
                (title, value) => EditorGUILayout.Vector2Field(title, value));

            Поле("Подстройка", x => x.подстройка,
                (title, value) => (ПодстройкаПодЭкран)EditorGUILayout.EnumPopup(title, value));
        }
    }
}
#endif
