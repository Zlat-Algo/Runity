using UnityEditor;
using UnityEngine;

public class Туман : RunityComponent
{
    void OnEnable() => RenderSettings.fog = true;
    void OnDisable() => RenderSettings.fog = false;

    public static Цвет цвет
    {
        get => RenderSettings.fogColor;
        set => RenderSettings.fogColor = value;
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
    }
}
#endif