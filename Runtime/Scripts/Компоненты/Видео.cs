#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Где показывается видео
/// </summary>
public enum КудаВыводитьВидео { ПоверхЭкрана, ФономЭкрана, НаЭтомОбъекте, ВКартинкуИнтерфейса }

/// <summary>
/// Проигрывает видеоролик: заставку, фон или телевизор в игровом мире
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(VideoPlayer))]
[AddComponentMenu("  Runity/ Визуал/Видео")]
public class Видео : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<VideoPlayer>();
    void OnValidate() => Awake();
    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public VideoPlayer оригинал => (VideoPlayer)оригинальныйКомпонент;

    /// <summary>
    /// Видеофайл из проекта
    /// </summary>
    public VideoClip ролик
    {
        get => оригинал.clip;
        set
        {
            оригинал.source = VideoSource.VideoClip;
            оригинал.clip = value;
        }
    }

    /// <summary>
    /// Прямая ссылка на видеофайл в интернете или путь к файлу на компьютере. Если её указать, то видео возьмётся по ссылке, а не из проекта.
    /// Ссылка должна вести на сам файл, а не на страницу с плеером: ссылки на YouTube или ВКонтакте не подойдут.
    /// Подходят файлы .mp4 (H.264), .webm (VP8), .mov, .m4v и .ogv; в Windows ещё .avi, .wmv, .asf, .mpg, .mpeg и .dv.
    /// Надёжнее всего .mp4 с кодеком H.264 - он работает везде
    /// </summary>
    public Строка ссылка
    {
        get => оригинал.url;
        set
        {
            оригинал.url = value;
            оригинал.source = string.IsNullOrEmpty(value) ? VideoSource.VideoClip : VideoSource.Url;
        }
    }

    /// <summary>
    /// Если "да", то видео начинается заново, когда закончится
    /// </summary>
    public Флажок повторять
    {
        get => оригинал.isLooping;
        set => оригинал.isLooping = value;
    }

    /// <summary>
    /// Если "да", то видео начнётся при появлении объекта
    /// </summary>
    public Флажок играетНаСтарте
    {
        get => оригинал.playOnAwake;
        set => оригинал.playOnAwake = value;
    }

    /// <summary>
    /// Скорость проигрывания: 1 - обычная, 2 - в два раза быстрее
    /// </summary>
    public Дробное скорость
    {
        get => оригинал.playbackSpeed;
        set => оригинал.playbackSpeed = value;
    }

    /// <summary>
    /// Громкость звука видео: от 0 до 1
    /// </summary>
    public Дробное громкость
    {
        get => оригинал.GetDirectAudioVolume(0);
        set
        {
            оригинал.audioOutputMode = VideoAudioOutputMode.Direct;
            оригинал.SetDirectAudioVolume(0, value);
        }
    }

    [SerializeField] КудаВыводитьВидео _куда;
    /// <summary>
    /// Где показывается видео. "На этом объекте" рисует видео на его 3D-модели или спрайте
    /// </summary>
    public КудаВыводитьВидео куда { get => _куда; set { _куда = value; НастроитьВывод(); } }

    [SerializeField] RawImage _картинка;
    /// <summary>
    /// Картинка интерфейса, в которой показывается видео, если выбран вывод "В картинку интерфейса"
    /// </summary>
    public RawImage картинка { get => _картинка; set { _картинка = value; НастроитьВывод(); } }

    [SerializeField] События _приЗавершении;
    /// <summary>
    /// Срабатывает, когда видео доиграло до конца
    /// </summary>
    public События приЗавершении => _приЗавершении;

    [SerializeField] События _приВоспроизведении;
    /// <summary>
    /// Срабатывает, когда видео запустили или сняли с паузы
    /// </summary>
    public События приВоспроизведении => _приВоспроизведении;

    [SerializeField] События _приПаузе;
    /// <summary>
    /// Срабатывает, когда видео поставили на паузу
    /// </summary>
    public События приПаузе => _приПаузе;

    [SerializeField] События _приПеремотке;
    /// <summary>
    /// Срабатывает, когда видео перемотали
    /// </summary>
    public События приПеремотке => _приПеремотке;

    [SerializeField] События _приОшибке;
    /// <summary>
    /// Срабатывает, если видео не получилось открыть, например по неправильной ссылке
    /// </summary>
    public События приОшибке => _приОшибке;

    /// <summary>
    /// Текст последней ошибки видео
    /// </summary>
    public Строка последняяОшибка { get; private set; } = "";

    /// <summary>
    /// Даёт "да", пока видео играет
    /// </summary>
    public Флажок играет => оригинал.isPlaying;

    /// <summary>
    /// Сколько секунд видео уже проиграно
    /// </summary>
    public Дробное текущееВремя => (float)оригинал.time;

    /// <summary>
    /// Длина видео в секундах
    /// </summary>
    public Дробное длительность => (float)оригинал.length;

    RenderTexture текстура;

    void Start()
    {
        оригинал.loopPointReached += _ => _приЗавершении.Вызвать();
        оригинал.started += _ => _приВоспроизведении.Вызвать();
        оригинал.seekCompleted += _ => _приПеремотке.Вызвать();
        оригинал.errorReceived += (_, ошибка) => { последняяОшибка = ошибка; _приОшибке.Вызвать(); };
        НастроитьВывод();
    }

    void OnDestroy()
    {
        if (текстура != null)
        {
            текстура.Release();
            Destroy(текстура);
        }
    }

    void НастроитьВывод()
    {
        if (оригинал == null) return;

        switch (_куда)
        {
            case КудаВыводитьВидео.ПоверхЭкрана:
                оригинал.renderMode = VideoRenderMode.CameraNearPlane;
                оригинал.targetCamera = Camera.main;
                break;

            case КудаВыводитьВидео.ФономЭкрана:
                оригинал.renderMode = VideoRenderMode.CameraFarPlane;
                оригинал.targetCamera = Camera.main;
                break;

            case КудаВыводитьВидео.НаЭтомОбъекте:
                оригинал.renderMode = VideoRenderMode.MaterialOverride;
                оригинал.targetMaterialRenderer = GetComponent<Renderer>();
                break;

            case КудаВыводитьВидео.ВКартинкуИнтерфейса:
                оригинал.renderMode = VideoRenderMode.RenderTexture;
                if (Application.isPlaying && _картинка != null)
                {
                    if (текстура == null) текстура = new RenderTexture(1280, 720, 0);
                    оригинал.targetTexture = текстура;
                    _картинка.texture = текстура;
                }
                break;
        }
    }

    /// <summary>
    /// Запускает или продолжает видео
    /// </summary>
    public void Играть()
    {
        // Когда видео снимают с паузы, Unity не сообщает о старте, поэтому событие вызывается здесь
        bool былоНаПаузе = оригинал.isPaused;
        оригинал.Play();
        if (былоНаПаузе) _приВоспроизведении.Вызвать();
    }

    /// <summary>
    /// Ставит видео на паузу
    /// </summary>
    public void Приостановить()
    {
        if (!оригинал.isPlaying) return;
        оригинал.Pause();
        _приПаузе.Вызвать();
    }

    /// <summary>
    /// Ставит на паузу играющее видео и запускает стоящее
    /// </summary>
    public void ПереключитьПаузу()
    {
        if (оригинал.isPlaying) Приостановить();
        else Играть();
    }

    /// <summary>
    /// Перематывает видео вперёд на указанное количество секунд
    /// </summary>
    public void ПеремотатьВперёд(Дробное секунд) => Перемотать((float)текущееВремя + секунд);

    /// <summary>
    /// Перематывает видео назад на указанное количество секунд
    /// </summary>
    public void ПеремотатьНазад(Дробное секунд) => Перемотать((float)текущееВремя - секунд);

    /// <summary>
    /// Останавливает видео и возвращает его в начало
    /// </summary>
    public void Остановить() => оригинал.Stop();

    /// <summary>
    /// Перематывает видео на указанную секунду
    /// </summary>
    public void Перемотать(Дробное секунда)
    {
        float конец = длительность > 0 ? (float)длительность : float.MaxValue;
        оригинал.time = Mathf.Clamp(секунда, 0, конец);
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Видео))]
internal class ВидеоEditor : RunityEditor<Видео>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект проигрывает видео. Перетащите видеофайл в проект, а потом в поле Ролик");

        Пробел();

        Синхрополе("Ролик", x => x.ролик,
            (title, value) => ПолеОбъекта<VideoClip>(title, value));

        Синхрополе("Ссылка", x => x.ссылка,
            (title, value) => EditorGUILayout.TextField(title, value));

        if (!string.IsNullOrEmpty(компонент.ссылка))
        {
            ПроверкаВидео.Отрисовать(компонент.ссылка);
        }
        Пробел();

        Поле("Куда выводить", x => x.куда,
            (title, value) => (КудаВыводитьВидео)EditorGUILayout.EnumPopup(title, value));

        if (компонент.куда == КудаВыводитьВидео.ВКартинкуИнтерфейса)
        {
            Поле("Картинка", x => x.картинка,
                (title, value) => ПолеОбъекта<RawImage>(title, value, true));
        }
        else if (компонент.куда == КудаВыводитьВидео.НаЭтомОбъекте && компонент.GetComponent<Renderer>() == null)
        {
            Текст("Добавьте этому объекту Моделлер или Спрайтер", MessageType.Warning);
        }

        Пробел();

        Синхрополе("Повторять", x => x.повторять,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Играет на старте", x => x.играетНаСтарте,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Скорость", x => x.скорость,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Громкость", x => x.громкость,
            (title, value) => EditorGUILayout.Slider(title, value, 0, 1));

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При воспроизведении", "_приВоспроизведении");
        ОтобразитьСобытие("При паузе", "_приПаузе");
        ОтобразитьСобытие("При перемотке", "_приПеремотке");
        ОтобразитьСобытие("При завершении", "_приЗавершении");
        ОтобразитьСобытие("При ошибке", "_приОшибке");
        ЗакончитьСобытия();
    }
}

// Проверяет ссылку заранее: создаёт невидимый проигрыватель и пробует подготовить видео
internal static class ПроверкаВидео
{
    static string проверяемая;
    static string результат;
    static MessageType вид;
    static VideoPlayer проигрыватель;
    static double начало;

    internal static void Отрисовать(string ссылка)
    {
        if (проверяемая != ссылка && проигрыватель == null) результат = null;

        EditorGUI.BeginDisabledGroup(проигрыватель != null);
        if (GUILayout.Button(new GUIContent(проигрыватель != null ? "Проверяю видео..." : "Проверить видео", "Пробует открыть видео по ссылке и сообщает, получилось ли")))
        {
            Начать(ссылка);
        }
        EditorGUI.EndDisabledGroup();

        if (!string.IsNullOrEmpty(результат) && проверяемая == ссылка)
        {
            EditorGUILayout.HelpBox(результат, вид);
        }
    }

    static void Начать(string ссылка)
    {
        проверяемая = ссылка;
        результат = null;

        GameObject объект = new GameObject("Проверка видео") { hideFlags = HideFlags.HideAndDontSave };
        проигрыватель = объект.AddComponent<VideoPlayer>();
        проигрыватель.playOnAwake = false;
        проигрыватель.audioOutputMode = VideoAudioOutputMode.None;
        проигрыватель.renderMode = VideoRenderMode.APIOnly;
        проигрыватель.source = VideoSource.Url;
        проигрыватель.url = ссылка;
        проигрыватель.errorReceived += (_, ошибка) => Закончить($"Видео не открывается: {ошибка}", MessageType.Error);
        проигрыватель.prepareCompleted += п => Закончить($"Видео открывается. Размер {п.width}×{п.height}, длина {п.length:0.#} с.", MessageType.Info);
        проигрыватель.Prepare();

        начало = EditorApplication.timeSinceStartup;
        EditorApplication.update += Ждать;
    }

    static void Ждать()
    {
        if (проигрыватель == null) { EditorApplication.update -= Ждать; return; }
        if (EditorApplication.timeSinceStartup - начало > 20)
            Закончить("Видео не ответило за 20 секунд. Проверьте ссылку и интернет", MessageType.Warning);
    }

    static void Закончить(string текст, MessageType тип)
    {
        результат = текст;
        вид = тип;
        EditorApplication.update -= Ждать;
        if (проигрыватель != null) Object.DestroyImmediate(проигрыватель.gameObject);
        проигрыватель = null;
        InternalEditorUtility.RepaintAllViews();
    }
}
#endif