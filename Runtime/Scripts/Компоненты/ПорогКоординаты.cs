using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// По какой оси проверяется координата
/// </summary>
public enum ОсьКоординаты { X, Y, Z }

/// <summary>
/// Когда срабатывает порог: когда объект стал выше (больше) порога, ниже (меньше) или в любую сторону
/// </summary>
public enum ПереходПорога { СталВыше, СталНиже, ВЛюбуюСторону }

/// <summary>
/// Срабатывает, когда объект переходит заданную координату по одной оси: упал ниже -10 по Y, пересёк финиш по X, поднялся на вершину.
/// Когда объект выбран, плоскость порога видна в сцене
/// </summary>
[AddComponentMenu("  Runity/ Механики/Порог Координаты")]
public class ПорогКоординаты : RunityComponent
{
    protected override void OnEnable() { }
    protected override void OnDisable() { }

    [SerializeField] ОсьКоординаты _ось = ОсьКоординаты.Y;
    /// <summary>
    /// По какой оси проверяется координата
    /// </summary>
    public ОсьКоординаты ось { get => _ось; set => _ось = value; }

    [SerializeField] Дробное _значение = -10;
    /// <summary>
    /// Координата порога в мире
    /// </summary>
    public Дробное значение { get => _значение; set => _значение = value; }

    [SerializeField] ПереходПорога _когда = ПереходПорога.СталНиже;
    /// <summary>
    /// Когда срабатывает: объект стал выше порога, ниже или в любую сторону
    /// </summary>
    public ПереходПорога когда { get => _когда; set => _когда = value; }

    [SerializeField] Transform _цель;
    /// <summary>
    /// Чью координату проверять. Если пусто и теги не выбраны - этого объекта
    /// </summary>
    public Transform цель { get => _цель; set => _цель = value; }

    [SerializeField] Теги _теги = new Теги();
    /// <summary>
    /// Если выбраны теги, проверяются все объекты с этими тегами, например все игроки или все враги
    /// </summary>
    public Теги теги { get => _теги; set => _теги = value ?? new Теги(); }

    [SerializeField] Флажок _толькоОдинРаз = нет;
    /// <summary>
    /// Если "да", порог срабатывает только в первый раз
    /// </summary>
    public Флажок толькоОдинРаз { get => _толькоОдинРаз; set => _толькоОдинРаз = value; }

    [SerializeField] Дробное _размерВСцене = 20;
    /// <summary>
    /// Насколько большой рисуется плоскость порога в сцене. На игру не влияет
    /// </summary>
    public Дробное размерВСцене { get => _размерВСцене; set => _размерВСцене = value; }

    [SerializeField] События _приПересечении;
    /// <summary>
    /// Срабатывает, когда объект перешёл порог в нужную сторону
    /// </summary>
    public События приПересечении => _приПересечении;

    [SerializeField] События _приВозврате;
    /// <summary>
    /// Срабатывает, когда объект вернулся обратно за порог
    /// </summary>
    public События приВозврате => _приВозврате;

    /// <summary>
    /// Кто последним перешёл порог
    /// </summary>
    public GameObject последний { get; private set; }

    /// <summary>
    /// Сколько раз порог срабатывал
    /// </summary>
    public Число срабатываний { get; private set; }

    // Для каждого объекта запоминается, с какой стороны порога он был в прошлый раз
    readonly Dictionary<Transform, bool> выше = new Dictionary<Transform, bool>();

    float Координата(Transform объект) => ось switch
    {
        ОсьКоординаты.X => объект.position.x,
        ОсьКоординаты.Z => объект.position.z,
        _ => объект.position.y
    };

    IEnumerable<Transform> Проверяемые()
    {
        if (теги.любые) { yield return цель != null ? цель : transform; yield break; }
        foreach (string тег in теги.список)
        {
            if (string.IsNullOrEmpty(тег)) continue;
            foreach (GameObject найденный in GameObject.FindGameObjectsWithTag(тег)) yield return найденный.transform;
        }
    }

    void Update()
    {
        if (толькоОдинРаз && (int)срабатываний > 0) return;

        foreach (Transform объект in Проверяемые())
        {
            bool сейчасВыше = Координата(объект) > (float)значение;
            if (!выше.TryGetValue(объект, out bool былВыше))
            {
                выше[объект] = сейчасВыше;
                continue;
            }
            if (сейчасВыше == былВыше) continue;
            выше[объект] = сейчасВыше;
            последний = объект.gameObject;

            bool нужныйПереход = когда == ПереходПорога.ВЛюбуюСторону || (когда == ПереходПорога.СталВыше) == сейчасВыше;
            if (нужныйПереход)
            {
                срабатываний = (int)срабатываний + 1;
                _приПересечении.Вызвать();
                if (толькоОдинРаз) return;
            }
            else _приВозврате.Вызвать();
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector3 центр = цель != null ? цель.position : transform.position;
        float р = Mathf.Max(0.1f, (float)размерВСцене);
        Vector3 размер;
        switch (ось)
        {
            case ОсьКоординаты.X: центр.x = значение; размер = new Vector3(0.01f, р, р); break;
            case ОсьКоординаты.Z: центр.z = значение; размер = new Vector3(р, р, 0.01f); break;
            default: центр.y = значение; размер = new Vector3(р, 0.01f, р); break;
        }
        Gizmos.color = new Color(1f, 0.3f, 0.6f, 0.2f);
        Gizmos.DrawCube(центр, размер);
        Gizmos.color = new Color(1f, 0.3f, 0.6f, 0.9f);
        Gizmos.DrawWireCube(центр, размер);
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(ПорогКоординаты))]
internal class ПорогКоординатыEditor : RunityEditor<ПорогКоординаты>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект срабатывает, когда кто-то переходит координату по одной оси. Розовая плоскость в сцене показывает порог");

        Пробел();

        Поле("Ось", x => x.ось, (title, value) => (ОсьКоординаты)EditorGUILayout.EnumPopup(title, value));
        Поле("Значение", x => x.значение, (title, value) => EditorGUILayout.FloatField(title, value));
        Поле("Когда", x => x.когда, (title, value) => (ПереходПорога)EditorGUILayout.EnumPopup(title, value));

        Пробел();

        Поле("Цель", x => x.цель, (title, value) => ПолеОбъекта<Transform>(title, value, true));
        ОтобразитьТеги("Теги", "_теги");
        Поле("Только один раз", x => x.толькоОдинРаз, (title, value) => EditorGUILayout.Toggle(title, value));
        Поле("Размер в сцене", x => x.размерВСцене, (title, value) => EditorGUILayout.FloatField(title, value));

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При пересечении", "_приПересечении");
        ОтобразитьСобытие("При возврате", "_приВозврате");
        ЗакончитьСобытия();
    }
}
#endif
