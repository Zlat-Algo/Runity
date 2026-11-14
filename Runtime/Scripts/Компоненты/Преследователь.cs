#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Объект сам движется к другому объекту
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Передвижение/ Самостоятельное/Преследователь")]
public class Преследователь : RunityComponent
{
    /// <summary>
    /// За кем движется объект
    /// </summary>
    [Обязательное]
    public Transform цель { get => _цель; set => _цель = value; }
    /// <summary>
    /// Если "да", то объект не двигается по оси Z
    /// </summary>
    [SerializeField] Transform _цель;
    public bool режим2D { get => _режим2D; set => _режим2D = value; }
    /// <summary>
    /// На сколько объект сдвигается за физический кадр
    /// </summary>
    [SerializeField] bool _режим2D;
    public float скорость { get => _скорость; set => _скорость = value; }
    /// <summary>
    /// Если "да", то чем дальше цель, тем быстрее движется объект
    /// </summary>
    [SerializeField] float _скорость;
    public bool относительнаяСкорость { get => _относительнаяСкорость; set => _относительнаяСкорость = value; }
    [SerializeField] bool _относительнаяСкорость;
    //public float скоростьПоворота;
    //public bool плавныйПоворот;

    [SerializeField] float _зонаДостиженияЦели = .5f;
    /// <summary>
    /// На каком расстоянии от цели считается, что объект её догнал
    /// </summary>
    public float зонаДостиженияЦели { get => _зонаДостиженияЦели; set => _зонаДостиженияЦели = value; }

    [SerializeField] События _приДостиженииЦели;
    /// <summary>
    /// Срабатывает один раз, когда объект догнал цель
    /// </summary>
    public События приДостиженииЦели => _приДостиженииЦели;

    bool цельДостигнута;

    /// <summary>
    /// Начинает преследовать того, кого последним заметило Поле зрения на этом объекте.
    /// Удобно подключать к событию "Когда заметил"
    /// </summary>
    public void ПреследоватьЗамеченного()
    {
        if (TryGetComponent(out ПолеЗрения полеЗрения) && полеЗрения.последнийЗамеченный != null)
        {
            цель = полеЗрения.последнийЗамеченный.transform;
        }
    }

    /// <summary>
    /// Убирает цель, и объект останавливается
    /// </summary>
    public void ПерестатьПреследовать()
    {
        цель = null;
    }

    void FixedUpdate()
    {
        if (цель == null)
        {
            цельДостигнута = false;
            return;
        }

        Vector3 доЦели = цель.position - transform.position;
        if (режим2D) доЦели.z = 0;
        if (доЦели.magnitude <= зонаДостиженияЦели)
        {
            if (!цельДостигнута)
            {
                цельДостигнута = true;
                _приДостиженииЦели.Вызвать();
            }
            return;
        }
        цельДостигнута = false;

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
internal class ПреследовательEditor : RunityEditor<Преследователь>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект движется в направлении другого объекта");

        Пробел();

        Поле("Цель", x => x.цель,
            (title, value) => ПолеОбъекта<Transform>(title, value, true));

        Пробел();

        Поле("Режим 2D", x => x.режим2D,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        Поле("Скорость", x => x.скорость,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Поле("Относительная скорость", x => x.относительнаяСкорость,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Поле("Зона достижения цели", x => x.зонаДостиженияЦели,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При достижении цели", "_приДостиженииЦели");
        ЗакончитьСобытия();
    }
}
#endif