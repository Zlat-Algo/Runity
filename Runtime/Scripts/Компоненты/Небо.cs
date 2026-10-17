using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public enum РежимОкружающегоСвета { Небесный, Градиентный, Одноцветный }

public class Небо : RunityComponent
{
    public static Material материал
    {
        get => RenderSettings.skybox;
        set => RenderSettings.skybox = value;
    }

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
    public override void OnInspectorGUI()
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