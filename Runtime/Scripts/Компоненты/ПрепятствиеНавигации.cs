#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Форма препятствия для навигации
/// </summary>
public enum ФормаПрепятствия { Коробка, Капсула }

/// <summary>
/// Движущееся препятствие, которое Навигационные агенты обходят: дверь, ящик, другой персонаж
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshObstacle))]
[AddComponentMenu("  Runity/ Передвижение/ Навигация/Препятствие навигации")]
public class ПрепятствиеНавигации : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<NavMeshObstacle>();
    void OnValidate() => Awake();
    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public NavMeshObstacle оригинал => (NavMeshObstacle)оригинальныйКомпонент;

    /// <summary>
    /// Форма препятствия
    /// </summary>
    public ФормаПрепятствия форма
    {
        get => оригинал.shape == NavMeshObstacleShape.Capsule ? ФормаПрепятствия.Капсула : ФормаПрепятствия.Коробка;
        set => оригинал.shape = value == ФормаПрепятствия.Капсула ? NavMeshObstacleShape.Capsule : NavMeshObstacleShape.Box;
    }

    /// <summary>
    /// Размер коробки по трём осям
    /// </summary>
    public Направление3D размер { get => оригинал.size; set => оригинал.size = value; }

    /// <summary>
    /// Радиус капсулы
    /// </summary>
    public Дробное радиус { get => оригинал.radius; set => оригинал.radius = value; }

    /// <summary>
    /// Высота капсулы
    /// </summary>
    public Дробное высота { get => оригинал.height; set => оригинал.height = value; }

    /// <summary>
    /// Смещение препятствия относительно объекта
    /// </summary>
    public Направление3D центр { get => оригинал.center; set => оригинал.center = value; }

    /// <summary>
    /// Если "да", то препятствие вырезает дыру в навигационной карте, и агенты строят путь в обход. Если "нет", агенты просто стараются не столкнуться
    /// </summary>
    public Флажок вырезатьВКарте { get => оригинал.carving; set => оригинал.carving = value; }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(ПрепятствиеНавигации))]
internal class ПрепятствиеНавигацииEditor : RunityEditor<ПрепятствиеНавигации>
{
    protected override void Отрисовать()
    {
        Текст("Навигационные агенты обходят этот объект, даже если он двигается");

        Пробел();

        EnumField("Форма", x => x.форма, (component, value) => component.форма = value);

        if (компонент.форма == ФормаПрепятствия.Коробка)
        {
            Синхрополе("Размер", x => x.размер,
                (title, value) => EditorGUILayout.Vector3Field(title, value));
        }
        else
        {
            Синхрополе("Радиус", x => x.радиус,
                (title, value) => EditorGUILayout.FloatField(title, value));

            Синхрополе("Высота", x => x.высота,
                (title, value) => EditorGUILayout.FloatField(title, value));
        }

        Синхрополе("Центр", x => x.центр,
            (title, value) => EditorGUILayout.Vector3Field(title, value));

        Синхрополе("Вырезать в карте", x => x.вырезатьВКарте,
            (title, value) => EditorGUILayout.Toggle(title, value));
    }
}
#endif
