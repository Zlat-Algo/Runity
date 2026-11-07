#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Проигрывает звуки и музыку
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
[AddComponentMenu("  Runity/ Звук/Источник звука")]
public class ИсточникЗвука : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<AudioSource>();
    void OnValidate() => Awake();
    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public AudioSource оригинал => (AudioSource)оригинальныйКомпонент;

    /// <summary>
    /// Основной звук этого источника
    /// </summary>
    public AudioClip звук
    {
        get => оригинал.clip;
        set => оригинал.clip = value;
    }

    /// <summary>
    /// Громкость от 0 до 1
    /// </summary>
    public Дробное громкость
    {
        get => оригинал.volume;
        set => оригинал.volume = value;
    }

    /// <summary>
    /// Высота звука: 1 - обычная, больше - выше и быстрее, меньше - ниже и медленнее
    /// </summary>
    public Дробное высота
    {
        get => оригинал.pitch;
        set => оригинал.pitch = value;
    }

    /// <summary>
    /// Если "да", то звук будет повторяться бесконечно. Подходит для музыки
    /// </summary>
    public Флажок повторять
    {
        get => оригинал.loop;
        set => оригинал.loop = value;
    }

    /// <summary>
    /// Если "да", то звук начнёт играть при появлении объекта
    /// </summary>
    public Флажок играетНаСтарте
    {
        get => оригинал.playOnAwake;
        set => оригинал.playOnAwake = value;
    }

    /// <summary>
    /// Если "да", то звук тише вдали от объекта и слышен с нужной стороны
    /// </summary>
    public Флажок объёмныйЗвук
    {
        get => оригинал.spatialBlend > 0;
        set => оригинал.spatialBlend = value ? 1 : 0;
    }

    /// <summary>
    /// Даёт "да", пока звук играет
    /// </summary>
    public Флажок играет => оригинал.isPlaying;

    /// <summary>
    /// Даёт "да", пока звук на паузе
    /// </summary>
    public Флажок наПаузе { get; private set; }

    [SerializeField] Флажок _случайнаяВысота = нет;
    /// <summary>
    /// Если "да", то каждый запуск немного меняет высоту. Так одинаковые звуки меньше надоедают
    /// </summary>
    public Флажок случайнаяВысота { get => _случайнаяВысота; set => _случайнаяВысота = value; }

    [SerializeField] События _приЗавершении;
    /// <summary>
    /// Срабатывает, когда звук доиграл до конца
    /// </summary>
    public События приЗавершении => _приЗавершении;

    bool игралВПрошломКадре;
    float исходнаяВысота;

    void Start()
    {
        исходнаяВысота = высота;
    }

    void Update()
    {
        if (игралВПрошломКадре && !играет && !наПаузе)
        {
            _приЗавершении.Вызвать();
        }
        игралВПрошломКадре = играет;
    }

    void ВыбратьВысоту()
    {
        if (случайнаяВысота)
        {
            высота = исходнаяВысота * Случайности.СлучайноеИзДиапазона(0.9, 1.1);
        }
    }

    /// <summary>
    /// Запускает основной звук сначала
    /// </summary>
    public void Играть()
    {
        ВыбратьВысоту();
        наПаузе = нет;
        оригинал.Play();
    }

    /// <summary>
    /// Запускает указанный звук один раз поверх всего, что уже играет
    /// </summary>
    public void СыгратьОдинРаз(AudioClip какойЗвук)
    {
        if (какойЗвук == null) return;
        ВыбратьВысоту();
        оригинал.PlayOneShot(какойЗвук);
    }

    /// <summary>
    /// Ставит звук на паузу
    /// </summary>
    public void Приостановить()
    {
        наПаузе = да;
        оригинал.Pause();
    }

    /// <summary>
    /// Продолжает звук с того места, где была пауза
    /// </summary>
    public void Продолжить()
    {
        наПаузе = нет;
        оригинал.UnPause();
    }

    /// <summary>
    /// Полностью останавливает звук
    /// </summary>
    public void Остановить()
    {
        наПаузе = нет;
        игралВПрошломКадре = нет;
        оригинал.Stop();
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(ИсточникЗвука))]
internal class ИсточникЗвукаEditor : RunityEditor<ИсточникЗвука>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект проигрывает звуки и музыку");

        Пробел();

        Синхрополе("Звук", x => x.звук,
            (title, value) => ПолеОбъекта<AudioClip>(title, value));

        Синхрополе("Громкость", x => x.громкость,
            (title, value) => EditorGUILayout.Slider(title, value, 0, 1));

        Синхрополе("Высота", x => x.высота,
            (title, value) => EditorGUILayout.Slider(title, value, 0.1f, 3));

        Поле("Случайная высота", x => x.случайнаяВысота,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        Синхрополе("Повторять", x => x.повторять,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Играет на старте", x => x.играетНаСтарте,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Объёмный звук", x => x.объёмныйЗвук,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При завершении", "_приЗавершении");
        ЗакончитьСобытия();
    }
}
#endif
