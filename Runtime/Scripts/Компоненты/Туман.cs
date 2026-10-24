using UnityEditor;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Окружение/Туман")]
public class Туман : RunityComponent
{
    protected override void OnEnable() => RenderSettings.fog = true;
    protected override void OnDisable() => RenderSettings.fog = false;

    public static Цвет цвет
    {
        get => RenderSettings.fogColor;
        set => RenderSettings.fogColor = value;
    }
    public static float сила
    {
        get => RenderSettings.fogDensity;
        set => RenderSettings.fogDensity = Математика.Ограничить(value, 0, 1);
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Туман))]
internal class ТуманEditor : RunityEditor<Туман>
{
    public override void OnInspectorGUI()
    {
        Текст("Этот объект добавляет туман на карту");

        Пробел();

        Поле("Цвет", x => Туман.цвет,
            (title, value) => EditorGUILayout.ColorField(title, value));

        Поле("Cила", x => Туман.сила,
            (title, value) => EditorGUILayout.Slider(title, value, 0, 1));
    }
}
#endif