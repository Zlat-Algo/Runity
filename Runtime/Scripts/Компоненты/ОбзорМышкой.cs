#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Вид от первого лица: мышка поворачивает объект влево-вправо, а камеру вверх-вниз
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Передвижение/ Управление игроком/Обзор мышкой")]
public class ОбзорМышкой : RunityComponent
{
    protected override void OnEnable()
    {
        if (спрятатьКурсор) Мышка.режим = РежимМышки.Отключенная;
    }
    protected override void OnDisable()
    {
        if (спрятатьКурсор) Мышка.режим = РежимМышки.Свободная;
    }

    [SerializeField] Transform _камера;
    /// <summary>
    /// Что наклоняется вверх-вниз. Обычно это камера внутри игрока
    /// </summary>
    public Transform камера { get => _камера; set => _камера = value; }

    [SerializeField] Дробное _чувствительность = 0.1;
    /// <summary>
    /// Насколько быстро поворачивается взгляд
    /// </summary>
    public Дробное чувствительность { get => _чувствительность; set => _чувствительность = value; }

    [SerializeField] Дробное _предельныйНаклон = 85;
    /// <summary>
    /// На сколько градусов можно посмотреть вверх или вниз
    /// </summary>
    public Дробное предельныйНаклон { get => _предельныйНаклон; set => _предельныйНаклон = value; }

    [SerializeField] Флажок _спрятатьКурсор = да;
    /// <summary>
    /// Если "да", то курсор исчезнет и не будет выходить за окно игры
    /// </summary>
    public Флажок спрятатьКурсор { get => _спрятатьКурсор; set => _спрятатьКурсор = value; }

    float наклон;

    void Update()
    {
        Направление2D смещение = Мышка.смещение;

        transform.Rotate(Vector3.up, смещение.x * чувствительность);

        if (камера != null)
        {
            наклон = Математика.Ограничить(наклон - смещение.y * чувствительность, -предельныйНаклон, предельныйНаклон);
            камера.localEulerAngles = new Vector3(наклон, 0, 0);
        }
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(ОбзорМышкой))]
internal class ОбзорМышкойEditor : RunityEditor<ОбзорМышкой>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект поворачивается за мышкой, как в играх от первого лица");

        Пробел();

        Поле("Камера", x => x.камера,
            (title, value) => ПолеОбъекта<Transform>(title, value, true));

        Поле("Чувствительность", x => x.чувствительность,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Поле("Предельный наклон", x => x.предельныйНаклон,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Поле("Спрятать курсор", x => x.спрятатьКурсор,
            (title, value) => EditorGUILayout.Toggle(title, value));
    }
}
#endif
