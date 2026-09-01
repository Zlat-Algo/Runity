using UnityEditor;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Работа с объектами/Преследователь")]
public class Преследователь : RunityComponent
{
    public Transform цель { get => _цель; set => _цель = value; }
    [SerializeField] Transform _цель;
    public bool режим2D { get => _режим2D; set => _режим2D = value; }
    [SerializeField] bool _режим2D;
    public float скорость { get => _скорость; set => _скорость = value; }
    [SerializeField] float _скорость;
    public bool относительнаяСкорость { get => _относительнаяСкорость; set => _относительнаяСкорость = value; }
    [SerializeField] bool _относительнаяСкорость;
    //public float скоростьПоворота;
    //public bool плавныйПоворот;

    void FixedUpdate()
    {
        if (цель == null) return;

        if (режим2D)
        {
            if (относительнаяСкорость)
            {
                transform.position += (Vector3)(Vector2)(цель.position - transform.position) * скорость;
            }
            else
            {
                transform.position += (Vector3)(Vector2)(цель.position - transform.position).normalized * скорость;
            }
        }
        else
        {
            if (относительнаяСкорость)
            {
                transform.position += (цель.position - transform.position) * скорость;
            }
            else
            {
                transform.position += (цель.position - transform.position).normalized * скорость;
            }
        }
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Преследователь))]
public class ПреследовательEditor : RunityEditor<Преследователь>
{
    public override void OnInspectorGUI()
    {
        Текст("Этот объект движется в направлении другого объекта");

        Пробел();

        Поле("Цель", x => x.цель,
            (title, value) => Объект<Transform>(title, value));

        Пробел();

        Поле("Режим 2D", x => x.режим2D,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        Поле("Скорость", x => x.скорость,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Поле("Относительная скорость", x => x.относительнаяСкорость,
            (title, value) => EditorGUILayout.Toggle(title, value));
    }
}
#endif