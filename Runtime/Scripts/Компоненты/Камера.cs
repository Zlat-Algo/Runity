using System;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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

    public Camera оригинал => (Camera)оригинальныйКомпонент;

    public Флажок перспектива
    {
        get => !оригинал.orthographic;
        set => оригинал.orthographic = !value;
    }

    public Дробное размер
    {
        get => оригинал.orthographicSize;
        set => оригинал.orthographicSize = Математика.ОграничитьСнизу(value, 0.0001);
    }

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

    public Флажок залитьФонЦветом
    {
        get => оригинал.clearFlags == CameraClearFlags.SolidColor;
        set => оригинал.clearFlags = value ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox;
    }

    public Цвет фоновыйЦвет
    {
        get => оригинал.backgroundColor;
        set => оригинал.backgroundColor = value;
    }

    public static Камера главная;
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Камера))]
internal class КамераEditor : RunityEditor<Камера>
{
    public override void OnInspectorGUI()
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