#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;

public enum РежимОкружающегоСвета { Небесный, Градиентный, Одноцветный }

/// <summary>
/// Настраивает небо и солнце на сцене
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Визуал/ Окружение/Небо")]
public class Небо : RunityComponent
{
    protected override void OnEnable() { }
    protected override void OnDisable() { }

    /// <summary>
    /// Материал неба на всей сцене
    /// </summary>
    public static Material материал
    {
        get => RenderSettings.skybox;
        set => RenderSettings.skybox = value;
    }

    /// <summary>
    /// Откуда берётся мягкий свет, который освещает тени: от неба или одним цветом
    /// </summary>
    public static РежимОкружающегоСвета режимОкружающегоСвета
    {
        get => RenderSettings.ambientMode switch
        {
            AmbientMode.Skybox => РежимОкружающегоСвета.Небесный,
            AmbientMode.Trilight => РежимОкружающегоСвета.Градиентный,
            AmbientMode.Flat => РежимОкружающегоСвета.Одноцветный,
            _ => РежимОкружающегоСвета.Одноцветный
        };
        set => RenderSettings.ambientMode = value switch
        {
            РежимОкружающегоСвета.Небесный => AmbientMode.Skybox,
            РежимОкружающегоСвета.Градиентный => AmbientMode.Trilight,
            РежимОкружающегоСвета.Одноцветный => AmbientMode.Flat,
            _ => AmbientMode.Custom
        };
    }

    /// <summary>
    /// Насколько ярок окружающий свет
    /// </summary>
    public static Дробное силаОкружающегоСвета
    {
        get => RenderSettings.ambientIntensity;
        set => RenderSettings.ambientIntensity = Mathf.Clamp(value, 0, 8);
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Небо))]
internal class НебоEditor : RunityEditor<Небо>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект настраивает небо на сцене");

        Пробел();

        Поле("Материал", x => Небо.материал,
            (title, value) => ПолеОбъекта<Material>(title, value));

        Пробел();

        Небо.режимОкружающегоСвета = EnumField("Режим окружающего света", x => Небо.режимОкружающегоСвета,
            (component, value) => Небо.режимОкружающегоСвета = value);

        Поле("Сила окружающего света", x => Небо.силаОкружающегоСвета,
            (title, value) => EditorGUILayout.FloatField(title, value));
    }
}
#endif