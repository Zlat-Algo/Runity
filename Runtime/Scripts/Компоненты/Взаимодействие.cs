using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Игрок может навестись на этот объект и нажать кнопку, чтобы что-то произошло
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Механики/Взаимодействие")]
public class Взаимодействие : RunityComponent
{
    protected override void OnEnable() { }
    protected override void OnDisable()
    {
        if (!взаимодействиеДоступно) return;
        взаимодействиеДоступно = нет;
        _когдаНельзяВзаимодействовать.Вызвать();
    }

    /// <summary>
    /// С какого расстояния от камеры можно взаимодействовать
    /// </summary>
    [SerializeField] float _дальностьВзаимодействия = 5;
    public float дальностьВзаимодействия{ get => _дальностьВзаимодействия; set => _дальностьВзаимодействия = value;}

    /// <summary>
    /// Если "нет", то с объектом нельзя взаимодействовать
    /// </summary>
    [SerializeField] bool _включен = да;
    public bool включен { get => _включен; set => _включен = value;}

    /// <summary>
    /// Если "нет", то взаимодействовать можно только один раз
    /// </summary>
    [SerializeField] bool _многоразовый;
    public bool многоразовый { get => _многоразовый; set => _многоразовый = value; }

    /// <summary>
    /// Если "да", то взаимодействие идёт по левой кнопке мыши, а не по клавише E
    /// </summary>
    [SerializeField] bool _ЛКМВместоЕ;
    public bool ЛКМВместоЕ { get => _ЛКМВместоЕ; set => _ЛКМВместоЕ = value; }

    bool взаимодействиеДоступно;

    /// <summary>
    /// Срабатывает, когда игрок навёлся на объект и находится достаточно близко
    /// </summary>
    [SerializeField] События _когдаМожноВзаимодействовать;
    public События когдаМожноВзаимодействовать => _когдаМожноВзаимодействовать;
    /// <summary>
    /// Срабатывает, когда взаимодействие снова стало недоступно
    /// </summary>
    [SerializeField] События _когдаНельзяВзаимодействовать;
    public События когдаНельзяВзаимодействовать => _когдаНельзяВзаимодействовать;
    /// <summary>
    /// Срабатывает, когда игрок нажал кнопку взаимодействия
    /// </summary>
    [SerializeField] События _когдаВзаимодействует;
    public События когдаВзаимодействует => _когдаВзаимодействует;

    /// <summary>
    /// Даёт "да", пока игрок смотрит на объект и может с ним взаимодействовать
    /// </summary>
    public Флажок доступно => взаимодействиеДоступно;

    /// <summary>
    /// Срабатывает так, будто игрок нажал кнопку взаимодействия
    /// </summary>
    public void Взаимодействовать()
    {
        if (!включен) return;
        _когдаВзаимодействует.Вызвать();
        if (!многоразовый) включен = false;
    }

    void Update()
    {
        bool можно = включен && НаведеноНаМеня();
        if (можно != взаимодействиеДоступно)
        {
            взаимодействиеДоступно = можно;
            if (можно) _когдаМожноВзаимодействовать.Вызвать();
            else _когдаНельзяВзаимодействовать.Вызвать();
        }

        // Пока игра стоит (пауза, разговор), нажатия не считаются
        if (взаимодействиеДоступно && Time.timeScale > 0 && ((!ЛКМВместоЕ && Управление.использоватьНажато) || (ЛКМВместоЕ && Управление.ЛКМНажата)))
        {
            Взаимодействовать();
        }
    }

    bool НаведеноНаМеня()
    {
        Transform цель = ЧтоПодПрицелом(out float расстояние);
        return цель != null && расстояние <= дальностьВзаимодействия && (цель == transform || цель.IsChildOf(transform));
    }

    // Луч из камеры ищется один раз за кадр для всех объектов сразу.
    // Если курсор спрятан (вид от первого лица) или мыши нет, луч идёт в центр экрана, иначе - под курсор
    static int кадрЛуча = -1;
    static Transform подПрицелом;
    static float расстояниеДоЦели;
    static readonly RaycastHit[] попадания = new RaycastHit[16];

    static Transform ЧтоПодПрицелом(out float расстояние)
    {
        if (кадрЛуча != Time.frameCount)
        {
            кадрЛуча = Time.frameCount;
            подПрицелом = null;
            расстояниеДоЦели = float.MaxValue;
            Camera камера = Camera.main;
            if (камера != null)
            {
                Vector2 точка = Мышка.режим == РежимМышки.Отключенная || UnityEngine.InputSystem.Mouse.current == null ? new Vector2(Screen.width / 2f, Screen.height / 2f) : (Vector2)Мышка.позицияНаЭкране;
                НайтиВ3D(камера.ScreenPointToRay(точка));
                if (подПрицелом == null) НайтиВ2D(камера, точка);
            }
        }
        расстояние = расстояниеДоЦели;
        return подПрицелом;
    }

    static void НайтиВ3D(Ray луч)
    {
        int сколько = Physics.RaycastNonAlloc(луч, попадания, 1000, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        for (int i = 0; i < сколько; i++)
        {
            RaycastHit попадание = попадания[i];
            if (попадание.distance >= расстояниеДоЦели) continue;
            // Невидимые зоны-триггеры не загораживают объекты, если с ними самими нельзя взаимодействовать
            if (попадание.collider.isTrigger && попадание.collider.GetComponentInParent<Взаимодействие>() == null) continue;
            // Свой персонаж тоже не загораживает: камера часто стоит внутри него
            if (попадание.collider is CharacterController) continue;
            подПрицелом = попадание.collider.transform;
            расстояниеДоЦели = попадание.distance;
        }
    }

    static void НайтиВ2D(Camera камера, Vector2 точка)
    {
        Vector3 вМире = камера.ScreenToWorldPoint(new Vector3(точка.x, точка.y, -камера.transform.position.z));
        Collider2D найденный = Physics2D.OverlapPoint(вМире);
        if (найденный == null) return;
        подПрицелом = найденный.transform;
        расстояниеДоЦели = Vector2.Distance(камера.transform.position, найденный.transform.position);
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Взаимодействие))]
internal class ВзаимодействиеEditor : RunityEditor<Взаимодействие>
{
    //bool eventFoldout = true;

    protected override void Отрисовать()
    {
        Текст("С этим объектом можно взаимодействовать, чтобы он делал какое-то действие");

        Пробел();

        ПредупреждениеПроУправление();

        Поле("Дальность взаимодействия", x => x.дальностьВзаимодействия,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Поле("Включен", x => x.включен,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Поле("Многоразовый", x => x.многоразовый,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        Поле("Кнопка", x => x.ЛКМВместоЕ,
            (title, value) => GUILayout.Toolbar(value ? 1 : 0, new[] { new GUIContent("E", title.tooltip), new GUIContent("ЛКМ", title.tooltip) }) == 1);

        Пробел();

        //eventFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(eventFoldout, "События");
        //if (eventFoldout)
        {
            НачатьСобытия();

            ОтобразитьСобытие("Когда можно взаимодействовать", "_когдаМожноВзаимодействовать");
            ОтобразитьСобытие("Когда нельзя взаимодействовать", "_когдаНельзяВзаимодействовать");
            Пробел();
            ОтобразитьСобытие("Когда взаимодействует", "_когдаВзаимодействует");

            ЗакончитьСобытия();
        }
        //EditorGUILayout.EndFoldoutHeaderGroup();

        Пробел();
    }
}
#endif