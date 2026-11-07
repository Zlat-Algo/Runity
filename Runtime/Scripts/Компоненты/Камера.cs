using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Камера, через которую игрок видит игру
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("  Runity/Камера")]
public class Камера : RunityComponent
{
    void Awake()
    {
        оригинальныйКомпонент = GetComponent<Camera>();
        //if (TryGetComponent(out UniversalAdditionalCameraData cameraData)) twinComponent = cameraData;
        главная = this;
    }
    void OnValidate() => Awake();

    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public Camera оригинал => (Camera)оригинальныйКомпонент;

    /// <summary>
    /// "Да" для 3D-игр: дальние объекты кажутся меньше. "Нет" для 2D-игр
    /// </summary>
    public Флажок перспектива
    {
        get => !оригинал.orthographic;
        set => оригинал.orthographic = !value;
    }

    /// <summary>
    /// Сколько мира видно в 2D: чем больше число, тем дальше камера
    /// </summary>
    public Дробное размер
    {
        get => оригинал.orthographicSize;
        set => оригинал.orthographicSize = Математика.ОграничитьСнизу(value, 0.0001);
    }

    /// <summary>
    /// Объекты ближе этого расстояния не видны
    /// </summary>
    public Дробное близостьОтрисовки
    {
        get => оригинал.nearClipPlane;
        set
        {
            оригинал.nearClipPlane = Математика.ОграничитьСнизу(value, 0.0001);
            if (дальностьОтрисовки - value < 0.0001)
            {
                дальностьОтрисовки = value + 0.1;
            }
        }
    }

    /// <summary>
    /// Объекты дальше этого расстояния не видны
    /// </summary>
    public Дробное дальностьОтрисовки
    {
        get => оригинал.farClipPlane;
        set
        {
            оригинал.farClipPlane = Математика.ОграничитьСнизу(value, 0.0001);
            if (value - близостьОтрисовки < 0.0001)
            {
                близостьОтрисовки = value - 0.1;
            }
        }
    }

    /// <summary>
    /// Если "да", то фон будет одним цветом, а если "нет", то небом
    /// </summary>
    public Флажок залитьФонЦветом
    {
        get => оригинал.clearFlags == CameraClearFlags.SolidColor;
        set => оригинал.clearFlags = value ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox;
    }

    /// <summary>
    /// Цвет фона
    /// </summary>
    public Цвет фоновыйЦвет
    {
        get => оригинал.backgroundColor;
        set => оригинал.backgroundColor = value;
    }

    /// <summary>
    /// Камера, созданная последней
    /// </summary>
    public static Камера главная;
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Камера))]
internal class КамераEditor : RunityEditor<Камера>
{
    protected override void Отрисовать()
    {
        Текст("Через этот объект смотрит игрок");

        Пробел();

        Синхрополе("Близость отрисовки", x => x.близостьОтрисовки,
            (title, value) => EditorGUILayout.FloatField(title, value));
        Синхрополе("Дальность отрисовки", x => x.дальностьОтрисовки,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Пробел();

        Синхрополе("Перспектива", x => x.перспектива,
            (title, value) => EditorGUILayout.Toggle(title, value));
        if (!компонент.перспектива)
        {
            Синхрополе("Размер", x => x.размер,
                (title, value) => EditorGUILayout.FloatField(title, value));
        }

        Пробел();

        Синхрополе("Залить фон цветом", x => x.залитьФонЦветом,
            (title, value) => EditorGUILayout.Toggle(title, value));
        if (компонент.залитьФонЦветом)
        {
            Синхрополе("Фоновый цвет", x => x.фоновыйЦвет,
                (title, value) => EditorGUILayout.ColorField(title, value));
        }
    }
}
#endif