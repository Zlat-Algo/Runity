using UnityEditor;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ФизическоеТело2D))]
[AddComponentMenu("  Runity/ Передвижение/Передвижение 2D")]
public class Передвижение2D : Передвижение
{
    void Awake() => оригинальныйКомпонент = GetComponent<ФизическоеТело2D>();
    void OnValidate() => Awake();
    public ФизическоеТело2D оригинал => (ФизическоеТело2D)оригинальныйКомпонент;

    [SerializeField] Флажок _вверх = да;
    public Флажок вверх { get => _вверх; set => _вверх = value; }

    [SerializeField] Флажок _вниз = да;
    public Флажок вниз { get => _вниз; set => _вниз = value; }

    [SerializeField] Флажок _влево = да;
    public Флажок влево { get => _влево; set => _влево = value; }

    [SerializeField] Флажок _вправо = да;
    public Флажок вправо { get => _вправо; set => _вправо = value; }

    void Update()
    {
        Направление2D движение = скорость * new Направление2D(
            (Управление.вправоУдерживается && вправо ? 1 : 0) +
            (Управление.влевоУдерживается && влево ? -1 : 0),
            (Управление.вперёдУдерживается && вверх ? 1 : 0) +
            (Управление.назадУдерживается && вниз ? -1 : 0)).единичное;

        if (влево || вправо)
        {
            оригинал.движениеX = движение.x;
        }
        if (вверх || вниз)
        {
            оригинал.движениеY = движение.y;
        }
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Передвижение2D))]
internal class Передвижение2DСверхуEditor : RunityEditor<Передвижение2D>
{
    public override void OnInspectorGUI()
    {
        Текст("Этим объектом может управлять игрок, чтобы перемещаться по двум осям");

        Пробел();

        Поле("Скорость", x => x.скорость,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Пробел();

        GUILayout.Label("Объект сможет перемещаться");

        Поле("Вверх", x => x.вверх,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Поле("Вниз", x => x.вниз,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Поле("Влево", x => x.влево,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Поле("Вправо", x => x.вправо,
            (title, value) => EditorGUILayout.Toggle(title, value));
    }
}
#endif