using UnityEditor;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ФизическоеТело2D))]
[AddComponentMenu("  Runity/ Передвижение/Передвижение 2D сверху")]
public class Передвижение2DСверху : Передвижение
{
    void Awake() => оригинальныйКомпонент = GetComponent<ФизическоеТело2D>();
    void OnValidate() => Awake();
    public ФизическоеТело2D оригинал => (ФизическоеТело2D)оригинальныйКомпонент;

    void Update()
    {
        оригинал.движение = скорость * Направление2D(
            (Управление.вправоУдерживается ? 1 : 0) +
            (Управление.влевоУдерживается ? -1 : 0),
            (Управление.вперёдУдерживается ? 1 : 0) +
            (Управление.назадУдерживается ? -1 : 0)).normalized;
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Передвижение2DСверху))]
public class Передвижение2DСверхуEditor : RunityEditor<Передвижение2DСверху>
{
    public override void OnInspectorGUI()
    {
        Текст("Этим объектом может управлять игрок, чтобы перемещаться в одной плоскости");

        Пробел();

        Поле("Скорость", x => x.скорость,
            (title, value) => EditorGUILayout.FloatField(title, value));
    }
}
#endif