using System;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Экран, который закрывает игру, пока грузится следующая сцена, и показывает, сколько загрузилось.
/// Он хранится образцом (префабом) в папке Resources с именем «Экран загрузки», появляется сам при каждом переходе через Сцены
/// и исчезает, когда сцена загрузилась. На сцену его класть не нужно. Создать образец: Assets - Create - Runity - Экран загрузки
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
[AddComponentMenu("  Runity/ Интерфейс/ Основа/Экран загрузки")]
public class ЭкранЗагрузки : RunityComponent
{
    protected override void OnEnable() { }
    protected override void OnDisable() { }

    [SerializeField] Картинка _полоска;
    /// <summary>
    /// Картинка в режиме Заполнение, которая показывает, сколько загрузилось
    /// </summary>
    public Картинка полоска { get => _полоска; set => _полоска = value; }

    [SerializeField] Graphic _текст;
    /// <summary>
    /// Текст, в котором пишется, сколько процентов загрузилось
    /// </summary>
    public Graphic текст { get => _текст; set => _текст = value; }

    [SerializeField] Строка _надпись = "Загрузка... {0}%";
    /// <summary>
    /// Что писать в тексте. Вместо {0} подставляются проценты
    /// </summary>
    public Строка надпись { get => _надпись; set => _надпись = value; }

    [SerializeField] Дробное _минимальноеВремя = 0.5f;
    /// <summary>
    /// Сколько секунд экран показывается хотя бы, даже если сцена загрузилась мгновенно. Так экран не мелькает
    /// </summary>
    public Дробное минимальноеВремя { get => _минимальноеВремя; set => _минимальноеВремя = value; }

    [SerializeField] Дробное _плавность = 0.3f;
    /// <summary>
    /// За сколько секунд экран плавно появляется и исчезает. 0 - без плавности
    /// </summary>
    public Дробное плавность { get => _плавность; set => _плавность = value; }

    [SerializeField] События _приНачалеЗагрузки;
    /// <summary>
    /// Срабатывает, когда началась загрузка сцены
    /// </summary>
    public События приНачалеЗагрузки => _приНачалеЗагрузки;

    [SerializeField] События _приОкончанииЗагрузки;
    /// <summary>
    /// Срабатывает, когда новая сцена загрузилась и экран исчез
    /// </summary>
    public События приОкончанииЗагрузки => _приОкончанииЗагрузки;

    /// <summary>
    /// Под каким именем образец экрана лежит в папке Resources
    /// </summary>
    public const string имяОбразца = "Экран загрузки";

    /// <summary>
    /// Образец экрана из папки Resources. Пусто, если образца нет: тогда сцены загружаются без экрана
    /// </summary>
    public static ЭкранЗагрузки образец => Resources.Load<ЭкранЗагрузки>(имяОбразца);

    /// <summary>
    /// Экран, который показывается прямо сейчас. Пусто, если ничего не грузится
    /// </summary>
    public static ЭкранЗагрузки текущий { get; private set; }

    /// <summary>
    /// Даёт "да", пока идёт загрузка
    /// </summary>
    public Флажок идётЗагрузка { get; private set; }

    /// <summary>
    /// Сколько загрузилось: от 0 до 1
    /// </summary>
    public Дробное прогресс { get; private set; }

    CanvasGroup группа;

    void Awake()
    {
        группа = GetComponent<CanvasGroup>();
        Показать(0);
    }

    void OnDestroy()
    {
        if (текущий == this) текущий = null;
    }

    // Создаёт экран из образца на время загрузки. Даёт "нет", если образца нет
    internal static bool Начать(Func<AsyncOperation> начать)
    {
        if (текущий != null) return true;
        ЭкранЗагрузки шаблон = образец;
        if (шаблон == null) return false;

        GameObject копия = Instantiate(шаблон.transform.root.gameObject);
        копия.name = шаблон.transform.root.name;
        ЭкранЗагрузки экран = копия.GetComponentInChildren<ЭкранЗагрузки>();
        экран.StartCoroutine(экран.Загрузка(начать));
        return true;
    }

    void Показать(float непрозрачность)
    {
        группа.alpha = непрозрачность;
        группа.blocksRaycasts = непрозрачность > 0;
        группа.interactable = непрозрачность > 0;
    }

    /// <summary>
    /// Показывает этот экран и загружает сцену по номеру
    /// </summary>
    public void Загрузить(Число номерСцены)
    {
        if (идётЗагрузка) return;
        StartCoroutine(Загрузка(() => SceneManager.LoadSceneAsync((int)номерСцены)));
    }

    /// <summary>
    /// Показывает этот экран и загружает сцену по имени. В редакторе сцену можно запустить, даже если её нет в списке сцен
    /// </summary>
    public void Загрузить(Строка имяСцены)
    {
        if (идётЗагрузка) return;
        string имя = имяСцены;
        StartCoroutine(Загрузка(() => Сцены.ЗагрузитьФоном(имя)));
    }

    /// <summary>
    /// Показывает этот экран и загружает текущую сцену заново
    /// </summary>
    public void Перезагрузить()
    {
        if (идётЗагрузка) return;
        Scene сцена = SceneManager.GetActiveScene();
        StartCoroutine(Загрузка(() => Сцены.ЗагрузитьФоном(сцена)));
    }

    IEnumerator Загрузка(Func<AsyncOperation> начать)
    {
        текущий = this;
        идётЗагрузка = да;
        прогресс = 0;
        ОбновитьПрогресс();
        _приНачалеЗагрузки.Вызвать();

        // Во время загрузки старая сцена выгружается, а экран должен дожить до конца
        DontDestroyOnLoad(transform.root.gameObject);

        yield return Плавно(0, 1);

        float начало = Time.unscaledTime;
        AsyncOperation загрузка = начать();
        if (загрузка != null)
        {
            загрузка.allowSceneActivation = false;

            // Unity доводит загрузку до 0.9 и ждёт разрешения включить сцену
            while (загрузка.progress < 0.9f || Time.unscaledTime - начало < минимальноеВремя)
            {
                прогресс = Mathf.Min(загрузка.progress / 0.9f, минимальноеВремя > 0 ? (Time.unscaledTime - начало) / минимальноеВремя : 1);
                ОбновитьПрогресс();
                yield return null;
            }

            прогресс = 1;
            ОбновитьПрогресс();
            загрузка.allowSceneActivation = true;
            while (!загрузка.isDone) yield return null;
        }

        yield return Плавно(1, 0);
        идётЗагрузка = нет;
        _приОкончанииЗагрузки.Вызвать();

        // Экран нужен только на время загрузки
        текущий = null;
        Destroy(transform.root.gameObject);
    }

    IEnumerator Плавно(float от, float до)
    {
        for (float t = 0; t < плавность; t += Time.unscaledDeltaTime)
        {
            Показать(Mathf.Lerp(от, до, t / плавность));
            yield return null;
        }
        Показать(до);
    }

    void ОбновитьПрогресс()
    {
        if (полоска != null)
        {
            полоска.максимум = 1;
            полоска.значение = прогресс;
        }
        if (текст != null)
        {
            ЗаписьТекста.Записать(текст, string.Format(надпись, Mathf.RoundToInt(прогресс * 100)));
        }
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(ЭкранЗагрузки))]
internal class ЭкранЗагрузкиEditor : RunityEditor<ЭкранЗагрузки>
{
    protected override void Отрисовать()
    {
        Текст("Этот экран закрывает игру, пока грузится следующая сцена. Он появляется сам при переходах через Сцены и исчезает после загрузки");

        if (!EditorUtility.IsPersistent(компонент))
        {
            Текст($"Экрану не нужно лежать на сцене. Сохраните его образцом (префабом) в папке Resources с именем «{ЭкранЗагрузки.имяОбразца}» - " +
                  "или создайте готовый через Assets - Create - Runity - Экран загрузки", MessageType.Warning);
        }
        else if (компонент.transform.root.name != ЭкранЗагрузки.имяОбразца || !AssetDatabase.GetAssetPath(компонент).Contains("/Resources/"))
        {
            Текст($"Чтобы экран появлялся при переходах, образец должен лежать в папке Resources и называться «{ЭкранЗагрузки.имяОбразца}»", MessageType.Warning);
        }

        Пробел();

        Поле("Полоска", x => x.полоска, (title, value) => ПолеОбъекта<Картинка>(title, value, true));
        Поле("Текст", x => x.текст, (title, value) => ПолеОбъекта<Graphic>(title, value, true));
        Поле("Надпись", x => x.надпись, (title, value) => EditorGUILayout.TextField(title, value));

        Пробел();

        Поле("Минимальное время", x => x.минимальноеВремя, (title, value) => EditorGUILayout.FloatField(title, value));
        Поле("Плавность", x => x.плавность, (title, value) => EditorGUILayout.FloatField(title, value));

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При начале загрузки", "_приНачалеЗагрузки");
        ОтобразитьСобытие("При окончании загрузки", "_приОкончанииЗагрузки");
        ЗакончитьСобытия();
    }
}
#endif
