using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Откуда и куда направлен луч
/// </summary>
public enum НаправлениеЛуча { КудаСмотрит, НаМышку, ИзЦентраЭкрана, ДоОбъекта }

/// <summary>
/// Как работает луч
/// </summary>
public enum РежимЛуча { Выстрел, Датчик }

/// <summary>
/// Пускает невидимый луч и проверяет, во что он попал. Подходит для мгновенной стрельбы, лазеров и датчиков
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Механики/ Бой/Луч")]
public class Луч : RunityComponent
{
    protected override void OnEnable() { }
    protected override void OnDisable() { }

    [SerializeField] РежимЛуча _режим;
    /// <summary>
    /// Выстрел - луч пускается по кнопке и наносит урон. Датчик - луч проверяется каждый кадр, например у лазерной ловушки
    /// </summary>
    public РежимЛуча режим { get => _режим; set => _режим = value; }

    [SerializeField] Флажок _режим2D = да;
    /// <summary>
    /// "Да" для 2D-игр
    /// </summary>
    public Флажок режим2D { get => _режим2D; set => _режим2D = value; }

    [SerializeField] НаправлениеЛуча _направление;
    /// <summary>
    /// Куда направлен луч. Из центра экрана - как прицел в играх от первого лица. До объекта - луч заканчивается на указанном объекте
    /// </summary>
    public НаправлениеЛуча направление { get => _направление; set => _направление = value; }

    [SerializeField] НаправлениеВзгляда2D _взгляд2D;
    /// <summary>
    /// Куда смотрит объект в 2D, если его не поворачивали
    /// </summary>
    public НаправлениеВзгляда2D взгляд2D { get => _взгляд2D; set => _взгляд2D = value; }

    [SerializeField] Transform _конец;
    /// <summary>
    /// Объект, на котором заканчивается луч в режиме "До объекта". Луч задевает всё, что окажется между ним и этим объектом
    /// </summary>
    public Transform конец { get => _конец; set => _конец = value; }

    [SerializeField] Transform _точкаВылета;
    /// <summary>
    /// Откуда начинается луч. Если не указать, то из центра объекта
    /// </summary>
    public Transform точкаВылета { get => _точкаВылета; set => _точкаВылета = value; }

    [SerializeField] Дробное _дальность = 50;
    /// <summary>
    /// Как далеко достаёт луч
    /// </summary>
    public Дробное дальность { get => _дальность; set => _дальность = value; }

    [SerializeField] LayerMask _слои = ~0;
    /// <summary>
    /// Объекты каких физических слоёв задевает луч
    /// </summary>
    public LayerMask слои { get => _слои; set => _слои = value; }

    [SerializeField] Теги _тегиЦелей = new Теги();
    /// <summary>
    /// Каким объектам луч наносит урон и для каких срабатывает "При попадании в цель". Если теги не выбраны, то любым
    /// </summary>
    public Теги тегиЦелей => _тегиЦелей;

    [SerializeField] Число _урон = 1;
    /// <summary>
    /// Сколько здоровья отнимает попадание в режиме Выстрел
    /// </summary>
    public Число урон { get => _урон; set => _урон = value; }

    [SerializeField] Дробное _перезарядка = 0.2;
    /// <summary>
    /// Сколько секунд нужно ждать между выстрелами
    /// </summary>
    public Дробное перезарядка { get => _перезарядка; set => _перезарядка = value; }

    [SerializeField] Флажок _стрелятьПоКлавише = да;
    /// <summary>
    /// Если "да", то в режиме Выстрел луч пускается по клавише. Если "нет", то только из кода и событий
    /// </summary>
    public Флажок стрелятьПоКлавише { get => _стрелятьПоКлавише; set => _стрелятьПоКлавише = value; }

    [SerializeField] Клавиша _клавиша = Клавиша.ЛКМ;
    /// <summary>
    /// По какой клавише или кнопке мыши стреляет луч
    /// </summary>
    public Клавиша клавиша { get => _клавиша; set => _клавиша = value; }

    [SerializeField] Флажок _стрелятьПриУдержании = нет;
    /// <summary>
    /// Если "да", то можно не нажимать кнопку каждый раз, а держать её. Подходит для автомата
    /// </summary>
    public Флажок стрелятьПриУдержании { get => _стрелятьПриУдержании; set => _стрелятьПриУдержании = value; }

    [SerializeField] Флажок _показыватьЛуч = да;
    /// <summary>
    /// Если "да", то луч ненадолго становится виден в момент выстрела, а у датчика виден всегда
    /// </summary>
    public Флажок показыватьЛуч { get => _показыватьЛуч; set => _показыватьЛуч = value; }

    [SerializeField] Цвет _цветЛуча = new Цвет(1, 0.9, 0.3);
    /// <summary>
    /// Цвет видимого луча
    /// </summary>
    public Цвет цветЛуча { get => _цветЛуча; set => _цветЛуча = value; }

    [SerializeField] События _приВыстреле;
    /// <summary>
    /// Срабатывает при каждом выстреле, даже если луч никуда не попал
    /// </summary>
    public События приВыстреле => _приВыстреле;

    [SerializeField] События _приПопаданииВЦель;
    /// <summary>
    /// Срабатывает, когда луч попал в объект с подходящим тегом
    /// </summary>
    public События приПопаданииВЦель => _приПопаданииВЦель;

    [SerializeField] События _приПопаданииВПрепятствие;
    /// <summary>
    /// Срабатывает, когда луч упёрся во что-то другое
    /// </summary>
    public События приПопаданииВПрепятствие => _приПопаданииВПрепятствие;

    [SerializeField] События _приПромахе;
    /// <summary>
    /// Срабатывает, когда луч ни во что не попал
    /// </summary>
    public События приПромахе => _приПромахе;

    [SerializeField] События _когдаЦельПоявилась;
    /// <summary>
    /// Для датчика: срабатывает, когда в луч вошла цель
    /// </summary>
    public События когдаЦельПоявилась => _когдаЦельПоявилась;

    [SerializeField] События _когдаЦельПропала;
    /// <summary>
    /// Для датчика: срабатывает, когда цель вышла из луча
    /// </summary>
    public События когдаЦельПропала => _когдаЦельПропала;

    /// <summary>
    /// Во что попал луч в последний раз. Если ни во что, то пусто
    /// </summary>
    public GameObject последнийОбъект { get; private set; }

    /// <summary>
    /// Точка, куда попал луч в последний раз. Если промах, то конец луча
    /// </summary>
    public Направление3D точкаПопадания { get; private set; }

    /// <summary>
    /// Даёт "да", если в последний раз луч попал в цель
    /// </summary>
    public Флажок попалВЦель { get; private set; }

    /// <summary>
    /// Даёт "да", если перезарядка закончилась
    /// </summary>
    public Флажок готов => перезарядкаОсталось <= 0;

    float перезарядкаОсталось;
    LineRenderer линия;
    bool цельВЛуче;

    void Update()
    {
        if (перезарядкаОсталось > 0) перезарядкаОсталось -= Время.времяСПрошлогоКадра;

        if (режим == РежимЛуча.Датчик)
        {
            bool сейчас = Проверить();
            if (сейчас && !цельВЛуче) _когдаЦельПоявилась.Вызвать();
            if (!сейчас && цельВЛуче) _когдаЦельПропала.Вызвать();
            цельВЛуче = сейчас;
            if (показыватьЛуч) Показать(1);
            return;
        }

        if (стрелятьПоКлавише && (стрелятьПриУдержании ? Управление.КлавишаУдерживается(клавиша) : Управление.КлавишаНажата(клавиша)))
        {
            Выстрелить();
        }
    }

    /// <summary>
    /// Стреляет лучом, если закончилась перезарядка
    /// </summary>
    public void Выстрелить()
    {
        if (!готов) return;

        перезарядкаОсталось = перезарядка;
        _приВыстреле.Вызвать();

        bool попал = Проверить();

        if (последнийОбъект == null)
        {
            _приПромахе.Вызвать();
        }
        else if (попал)
        {
            Здоровье здоровье = последнийОбъект.GetComponentInParent<Здоровье>();
            if (здоровье != null) здоровье.ПолучитьУрон(урон);
            _приПопаданииВЦель.Вызвать();
        }
        else
        {
            _приПопаданииВПрепятствие.Вызвать();
        }

        if (показыватьЛуч)
        {
            StopAllCoroutines();
            StartCoroutine(ПоказатьНенадолго());
        }
    }

    /// <summary>
    /// Пускает луч без урона и перезарядки. Даёт "да", если луч упёрся в цель
    /// </summary>
    public Флажок Проверить()
    {
        Vector3 откуда = Начало();
        Vector3 куда = Направление(откуда);
        float длина = Длина(откуда);

        последнийОбъект = null;
        точкаПопадания = откуда + куда * длина;

        if (режим2D)
        {
            foreach (RaycastHit2D попадание in Physics2D.RaycastAll(откуда, куда, длина, слои))
            {
                if (попадание.collider.isTrigger || ПоискОбъектов.Свой(попадание.transform, transform)) continue;
                Запомнить(попадание.collider.gameObject, попадание.point);
                break;
            }
        }
        else
        {
            RaycastHit[] попадания = Physics.RaycastAll(откуда, куда, длина, слои, QueryTriggerInteraction.Ignore);
            System.Array.Sort(попадания, (первое, второе) => первое.distance.CompareTo(второе.distance));
            foreach (RaycastHit попадание in попадания)
            {
                if (ПоискОбъектов.Свой(попадание.transform, transform)) continue;
                Запомнить(попадание.collider.gameObject, попадание.point);
                break;
            }
        }

        попалВЦель = последнийОбъект != null && _тегиЦелей.Подходит(последнийОбъект);
        return попалВЦель;
    }

    void Запомнить(GameObject объект, Vector3 точка)
    {
        GameObject владелец = объект.GetComponentInParent<Rigidbody2D>() != null ? объект.GetComponentInParent<Rigidbody2D>().gameObject :
                              объект.GetComponentInParent<Rigidbody>() != null ? объект.GetComponentInParent<Rigidbody>().gameObject : объект;
        последнийОбъект = владелец;
        Vector3 итог = точка;
        if (режим2D) итог.z = transform.position.z;
        точкаПопадания = итог;
    }

    Vector3 Начало()
    {
        if (направление == НаправлениеЛуча.ИзЦентраЭкрана && Camera.main != null && !режим2D) return Camera.main.transform.position;
        return точкаВылета != null ? точкаВылета.position : transform.position;
    }

    // До объекта луч идёт чуть дальше его центра, чтобы точно задеть его коллайдер
    float Длина(Vector3 откуда)
    {
        if (направление != НаправлениеЛуча.ДоОбъекта || конец == null) return дальность;
        Vector3 до = конец.position - откуда;
        if (режим2D) до.z = 0;
        return до.magnitude + 0.01f;
    }

    Vector3 Направление(Vector3 откуда)
    {
        switch (направление)
        {
            case НаправлениеЛуча.ДоОбъекта:
                if (конец != null)
                {
                    Vector3 до = конец.position - откуда;
                    if (режим2D) до.z = 0;
                    if (до != Vector3.zero) return до.normalized;
                }
                break;

            case НаправлениеЛуча.НаМышку:
                if (режим2D)
                {
                    Vector3 мышка = Мышка.позицияВМире2D;
                    мышка.z = откуда.z;
                    return (мышка - откуда).normalized;
                }
                if (Camera.main != null) return Camera.main.ScreenPointToRay(Мышка.позицияНаЭкране).direction;
                break;

            case НаправлениеЛуча.ИзЦентраЭкрана:
                if (Camera.main != null) return Camera.main.transform.forward;
                break;
        }

        return ПоискОбъектов.Взгляд(transform, режим2D, взгляд2D, да);
    }

    IEnumerator ПоказатьНенадолго()
    {
        for (float прозрачность = 1; прозрачность > 0; прозрачность -= Время.времяСПрошлогоКадра * 10)
        {
            Показать(прозрачность);
            yield return null;
        }
        if (линия != null) линия.enabled = нет;
    }

    void Показать(float прозрачность)
    {
        if (линия == null)
        {
            GameObject объект = new GameObject("Видимый луч");
            объект.transform.SetParent(transform, false);
            линия = объект.AddComponent<LineRenderer>();
            линия.widthMultiplier = 0.05f;
            линия.positionCount = 2;
            линия.useWorldSpace = true;
            линия.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        Color цвет = цветЛуча;
        цвет.a *= прозрачность;
        линия.enabled = да;
        линия.startColor = цвет;
        линия.endColor = цвет;
        линия.SetPosition(0, Начало());
        линия.SetPosition(1, точкаПопадания);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 откуда = Начало();
        Gizmos.color = Color.red;
        Gizmos.DrawLine(откуда, откуда + Направление(откуда) * Длина(откуда));
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Луч))]
internal class ЛучEditor : RunityEditor<Луч>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект пускает невидимый луч и узнаёт, во что тот попал. Луч мгновенный, поэтому снаряд не нужен");

        Пробел();

        Поле("Режим", x => x.режим,
            (title, value) => (РежимЛуча)EditorGUILayout.EnumPopup(title, value));

        Поле("Режим 2D", x => x.режим2D,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Поле("Направление", x => x.направление,
            (title, value) => (НаправлениеЛуча)EditorGUILayout.EnumPopup(title, value));

        if (компонент.направление == НаправлениеЛуча.ДоОбъекта)
        {
            Поле("Конец луча", x => x.конец,
                (title, value) => ПолеОбъекта<Transform>(title, value, true));
        }

        if (компонент.режим2D && компонент.направление == НаправлениеЛуча.КудаСмотрит)
        {
            Поле("Куда смотрит", x => x.взгляд2D,
                (title, value) => (НаправлениеВзгляда2D)EditorGUILayout.EnumPopup(title, value));
        }

        Поле("Точка вылета", x => x.точкаВылета,
            (title, value) => ПолеОбъекта<Transform>(title, value, true));

        if (компонент.направление != НаправлениеЛуча.ДоОбъекта)
        {
            Поле("Дальность", x => x.дальность,
                (title, value) => EditorGUILayout.FloatField(title, value));
        }

        Поле("Слои", x => x.слои, ПолеСлоёв);

        ОтобразитьТеги("Теги целей", "_тегиЦелей");

        Пробел();

        if (компонент.режим == РежимЛуча.Выстрел)
        {
            Поле("Урон", x => x.урон,
                (title, value) => EditorGUILayout.IntField(title, value));

            Поле("Перезарядка", x => x.перезарядка,
                (title, value) => EditorGUILayout.FloatField(title, value));

            Поле("Стрелять по клавише", x => x.стрелятьПоКлавише,
                (title, value) => EditorGUILayout.Toggle(title, value));

            if (компонент.стрелятьПоКлавише)
            {
                Поле("Клавиша", x => x.клавиша,
                    (title, value) => ВыборКлавиши.Поле(title, value, $"Луч{компонент.GetHashCode()}"));

                Поле("Стрелять при удержании", x => x.стрелятьПриУдержании,
                    (title, value) => EditorGUILayout.Toggle(title, value));
            }

            Пробел();
        }

        Поле("Показывать луч", x => x.показыватьЛуч,
            (title, value) => EditorGUILayout.Toggle(title, value));

        if (компонент.показыватьЛуч)
        {
            Поле("Цвет луча", x => x.цветЛуча,
                (title, value) => EditorGUILayout.ColorField(title, value));
        }

        Пробел();

        НачатьСобытия();
        if (компонент.режим == РежимЛуча.Выстрел)
        {
            ОтобразитьСобытие("При выстреле", "_приВыстреле");
            ОтобразитьСобытие("При попадании в цель", "_приПопаданииВЦель");
            ОтобразитьСобытие("При попадании в препятствие", "_приПопаданииВПрепятствие");
            ОтобразитьСобытие("При промахе", "_приПромахе");
        }
        else
        {
            ОтобразитьСобытие("Когда цель появилась", "_когдаЦельПоявилась");
            ОтобразитьСобытие("Когда цель пропала", "_когдаЦельПропала");
        }
        ЗакончитьСобытия();
    }
}
#endif
