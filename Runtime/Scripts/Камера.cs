using System;
using UnityEditor;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("  Runity/Камера")]
public class Камера : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<Camera>();
    void OnValidate() => Awake();
    public Camera оригинал => (Camera)оригинальныйКомпонент;

    public Color фоновыйЦвет
    {
        get => оригинал.backgroundColor;
        set => оригинал.backgroundColor = value;
    }

    public bool перспектива
    {
        get => !оригинал.orthographic;
        set => оригинал.orthographic = !value;
    }
}

[CanEditMultipleObjects]
[CustomEditor(typeof(Камера))]
public class КамераEditor : RunityEditor<Камера>
{
    public override void OnInspectorGUI()
    {
        Текст("Через этот объект смотрит игрок");

        Пробел();

        Синхрополе("Фоновый цвет", x => x.фоновыйЦвет,
            (title, value) => EditorGUILayout.ColorField(title, value));
        Синхрополе("Перспектива", x => x.перспектива,
            (title, value) => EditorGUILayout.Toggle(title, value));
    }
}