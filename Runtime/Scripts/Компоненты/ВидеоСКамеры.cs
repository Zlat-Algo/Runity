#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Показывает в интерфейсе картинку с другой игровой камеры: мини-карта, камера наблюдения, вид сзади
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RawImage))]
[AddComponentMenu("  Runity/ Интерфейс/ Элементы/Видео с камеры")]
public class ВидеоСКамеры : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<RawImage>();

    void OnValidate()
    {
        Awake();
        Обновить();
    }

    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public RawImage оригинал => (RawImage)оригинальныйКомпонент;

    protected override void OnEnable()
    {
        base.OnEnable();
        Включить();
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        Выключить();
    }

    [SerializeField] Camera _камера;
    /// <summary>
    /// Игровая камера, с которой идёт картинка. Это должна быть отдельная камера, а не та, через которую смотрит игрок
    /// </summary>
    public Camera камера { get => _камера; set { _камера = value; Обновить(); } }

    [SerializeField] Направление2D _разрешение = new Направление2D(512, 512);
    /// <summary>
    /// Размер картинки в пикселях. Чем он больше, тем чётче видео и выше нагрузка
    /// </summary>
    public Направление2D разрешение { get => _разрешение; set { _разрешение = value; Обновить(); } }

    RenderTexture текстура;
    Camera подключённая;

    // Во время игры любое изменение настроек сразу видно, выключать и включать компонент не нужно
    void Обновить()
    {
        if (Application.isPlaying && isActiveAndEnabled) Включить();
    }

    /// <summary>
    /// Запускает видео
    /// </summary>
    public void Включить()
    {
        Выключить();
        if (камера == null) return;

        текстура = new RenderTexture(Mathf.Max(1, (int)разрешение.x), Mathf.Max(1, (int)разрешение.y), 16);
        камера.targetTexture = текстура;
        подключённая = камера;
        оригинал.texture = текстура;
    }

    /// <summary>
    /// Останавливает видео
    /// </summary>
    public void Выключить()
    {
        if (подключённая != null)
        {
            if (подключённая.targetTexture == текстура) подключённая.targetTexture = null;
            подключённая = null;
        }

        if (текстура != null)
        {
            if (оригинал != null && оригинал.texture == текстура) оригинал.texture = null;
            текстура.Release();
            Destroy(текстура);
            текстура = null;
        }
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(ВидеоСКамеры))]
internal class ВидеоСКамерыEditor : RunityEditor<ВидеоСКамеры>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект показывает в интерфейсе, что видит другая игровая камера. Картинка появляется во время игры");

        Пробел();

        Поле("Камера", x => x.камера,
            (title, value) => ПолеОбъекта<Camera>(title, value, true));

        if (компонент.камера != null && компонент.камера.CompareTag("MainCamera"))
        {
            Текст("Это главная камера игрока: если направить её сюда, игрок перестанет видеть игру. Создайте отдельную камеру", MessageType.Warning);
        }

        Поле("Разрешение", x => x.разрешение,
            (title, value) => EditorGUILayout.Vector2Field(title, value));
    }
}
#endif
