#if UNITY_EDITOR
using UnityEditor;
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
    /// Ссылка на видео в интернете. Если её указать, то видео возьмётся по ссылке, а не из проекта
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
    public void Играть() => оригинал.Play();

    /// <summary>
    /// Ставит видео на паузу
    /// </summary>
    public void Приостановить() => оригинал.Pause();

    /// <summary>
    /// Останавливает видео и возвращает его в начало
    /// </summary>
    public void Остановить() => оригинал.Stop();

    /// <summary>
    /// Перематывает видео на указанную секунду
    /// </summary>
    public void Перемотать(Дробное секунда) => оригинал.time = секунда;
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
        ОтобразитьСобытие("При завершении", "_приЗавершении");
        ЗакончитьСобытия();
    }
}
#endif
