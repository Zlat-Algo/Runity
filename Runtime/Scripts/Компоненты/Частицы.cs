#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Готовые настройки частиц
/// </summary>
public enum ПресетЧастиц { Огонь, Дым, Искры, Взрыв, Снег, Дождь, Пыль, Волшебство }

/// <summary>
/// Много маленьких картинок, из которых получаются огонь, дым, искры, дождь и другие эффекты
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(ParticleSystem))]
[AddComponentMenu("  Runity/ Эффекты/Частицы")]
public class Частицы : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<ParticleSystem>();

    void OnValidate()
    {
        Awake();
#if UNITY_EDITOR
        ParticleSystemRenderer отрисовщик = GetComponent<ParticleSystemRenderer>();
        if (отрисовщик != null && отрисовщик.sharedMaterial == null)
        {
            отрисовщик.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat");
        }
#endif
    }

    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public ParticleSystem оригинал => (ParticleSystem)оригинальныйКомпонент;

    protected override void OnEnable() { }
    protected override void OnDisable() { }

    /// <summary>
    /// Цвет частиц при появлении
    /// </summary>
    public Цвет цвет
    {
        get => оригинал.main.startColor.color;
        set { ParticleSystem.MainModule основное = оригинал.main; основное.startColor = (Color)value; }
    }

    /// <summary>
    /// Размер частиц при появлении
    /// </summary>
    public Дробное размер
    {
        get => оригинал.main.startSize.constant;
        set { ParticleSystem.MainModule основное = оригинал.main; основное.startSize = (float)value; }
    }

    /// <summary>
    /// С какой скоростью частицы разлетаются
    /// </summary>
    public Дробное скорость
    {
        get => оригинал.main.startSpeed.constant;
        set { ParticleSystem.MainModule основное = оригинал.main; основное.startSpeed = (float)value; }
    }

    /// <summary>
    /// Сколько секунд живёт каждая частица
    /// </summary>
    public Дробное времяЖизни
    {
        get => оригинал.main.startLifetime.constant;
        set { ParticleSystem.MainModule основное = оригинал.main; основное.startLifetime = (float)value; }
    }

    /// <summary>
    /// Сколько частиц появляется за секунду
    /// </summary>
    public Дробное количествоВСекунду
    {
        get => оригинал.emission.rateOverTime.constant;
        set { ParticleSystem.EmissionModule появление = оригинал.emission; появление.rateOverTime = (float)value; }
    }

    /// <summary>
    /// Насколько сильно частицы падают вниз. Отрицательное число - взлетают вверх
    /// </summary>
    public Дробное гравитация
    {
        get => оригинал.main.gravityModifier.constant;
        set { ParticleSystem.MainModule основное = оригинал.main; основное.gravityModifier = (float)value; }
    }

    /// <summary>
    /// Если "да", то эффект повторяется бесконечно. Если "нет", то проигрывается один раз
    /// </summary>
    public Флажок повторять
    {
        get => оригинал.main.loop;
        set { ParticleSystem.MainModule основное = оригинал.main; основное.loop = value; }
    }

    /// <summary>
    /// Если "да", то эффект начнётся при появлении объекта
    /// </summary>
    public Флажок играетНаСтарте
    {
        get => оригинал.main.playOnAwake;
        set { ParticleSystem.MainModule основное = оригинал.main; основное.playOnAwake = value; }
    }

    /// <summary>
    /// Если "да", то частицы остаются на месте, когда объект движется, и за ним тянется шлейф
    /// </summary>
    public Флажок оставлятьШлейф
    {
        get => оригинал.main.simulationSpace == ParticleSystemSimulationSpace.World;
        set { ParticleSystem.MainModule основное = оригинал.main; основное.simulationSpace = value ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local; }
    }

    [SerializeField] Флажок _удалитьПослеОкончания = нет;
    /// <summary>
    /// Если "да", то объект удалится, когда эффект без повтора закончится. Удобно для взрывов
    /// </summary>
    public Флажок удалитьПослеОкончания { get => _удалитьПослеОкончания; set => _удалитьПослеОкончания = value; }

    [SerializeField] События _приОкончании;
    /// <summary>
    /// Срабатывает, когда эффект без повтора закончился и все частицы исчезли
    /// </summary>
    public События приОкончании => _приОкончании;

    /// <summary>
    /// Даёт "да", пока эффект проигрывается
    /// </summary>
    public Флажок играет => оригинал.isPlaying;

    /// <summary>
    /// Сколько частиц видно сейчас
    /// </summary>
    public Число количествоЧастиц => оригинал.particleCount;

    bool игралРаньше;

    void Update()
    {
        bool живой = оригинал.IsAlive(true);

        if (игралРаньше && !живой && !повторять)
        {
            _приОкончании.Вызвать();
            if (удалитьПослеОкончания) Удалить(gameObject);
        }

        игралРаньше = живой;
    }

    /// <summary>
    /// Запускает эффект
    /// </summary>
    public void Запустить() => оригинал.Play();

    /// <summary>
    /// Перестаёт выпускать частицы. Уже вылетевшие догорают сами
    /// </summary>
    public void Остановить() => оригинал.Stop();

    /// <summary>
    /// Останавливает эффект и сразу убирает все частицы
    /// </summary>
    public void Очистить() => оригинал.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

    /// <summary>
    /// Сразу выпускает указанное количество частиц. Удобно для попаданий и подборов
    /// </summary>
    public void Вспышка(Число количество) => оригинал.Emit(количество);

    /// <summary>
    /// Настраивает частицы так, чтобы получился готовый эффект
    /// </summary>
    public void ПрименитьПресет(ПресетЧастиц пресет)
    {
        ParticleSystem.MainModule основное = оригинал.main;
        ParticleSystem.EmissionModule появление = оригинал.emission;
        ParticleSystem.ShapeModule форма = оригинал.shape;
        ParticleSystem.ColorOverLifetimeModule цветСоВременем = оригинал.colorOverLifetime;
        ParticleSystem.SizeOverLifetimeModule размерСоВременем = оригинал.sizeOverLifetime;
        ParticleSystemRenderer отрисовщик = GetComponent<ParticleSystemRenderer>();

        // Длительность нельзя менять, пока эффект играет
        bool игралДо = оригинал.isPlaying;
        оригинал.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        основное.loop = true;
        основное.duration = 1;
        основное.startRotation = 0;
        основное.simulationSpace = ParticleSystemSimulationSpace.World;
        основное.gravityModifier = 0;
        появление.SetBursts(new ParticleSystem.Burst[0]);
        форма.enabled = true;
        форма.rotation = new Vector3(-90, 0, 0);
        форма.radius = 0.2f;
        форма.angle = 15;
        цветСоВременем.enabled = true;
        размерСоВременем.enabled = false;
        отрисовщик.renderMode = ParticleSystemRenderMode.Billboard;

        switch (пресет)
        {
            case ПресетЧастиц.Огонь:
                Настроить(0.8f, 2, 0.5f, 40, new Color(1, 0.6f, 0.1f), new Color(1, 0.1f, 0, 0));
                Уменьшение(размерСоВременем);
                break;

            case ПресетЧастиц.Дым:
                Настроить(3, 1, 1, 10, new Color(0.5f, 0.5f, 0.5f, 0.6f), new Color(0.3f, 0.3f, 0.3f, 0));
                форма.angle = 20;
                размерСоВременем.enabled = true;
                размерСоВременем.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 0.5f, 1, 2));
                break;

            case ПресетЧастиц.Искры:
                Настроить(0.5f, 5, 0.08f, 30, new Color(1, 0.9f, 0.4f), new Color(1, 0.4f, 0, 0));
                форма.shapeType = ParticleSystemShapeType.Sphere;
                основное.gravityModifier = 1;
                break;

            case ПресетЧастиц.Взрыв:
                Настроить(0.6f, 6, 0.4f, 0, new Color(1, 0.9f, 0.3f), new Color(0.6f, 0.1f, 0, 0));
                форма.shapeType = ParticleSystemShapeType.Sphere;
                основное.loop = false;
                появление.SetBursts(new[] { new ParticleSystem.Burst(0, 60) });
                Уменьшение(размерСоВременем);
                break;

            case ПресетЧастиц.Снег:
                Настроить(8, 1, 0.1f, 30, Color.white, new Color(1, 1, 1, 0));
                форма.shapeType = ParticleSystemShapeType.Cone;
                форма.rotation = new Vector3(90, 0, 0);
                форма.angle = 0;
                форма.radius = 8;
                break;

            case ПресетЧастиц.Дождь:
                Настроить(1.5f, 15, 0.05f, 200, new Color(0.7f, 0.8f, 1, 0.8f), new Color(0.7f, 0.8f, 1, 0.3f));
                форма.shapeType = ParticleSystemShapeType.Cone;
                форма.rotation = new Vector3(90, 0, 0);
                форма.angle = 0;
                форма.radius = 8;
                отрисовщик.renderMode = ParticleSystemRenderMode.Stretch;
                отрисовщик.lengthScale = 3;
                break;

            case ПресетЧастиц.Пыль:
                Настроить(4, 0.2f, 0.05f, 15, new Color(0.9f, 0.85f, 0.7f, 0.6f), new Color(0.9f, 0.85f, 0.7f, 0));
                форма.shapeType = ParticleSystemShapeType.Sphere;
                форма.radius = 3;
                break;

            case ПресетЧастиц.Волшебство:
                Настроить(1.5f, 0.5f, 0.15f, 30, new Color(0.7f, 0.3f, 1), new Color(0.3f, 0.9f, 1, 0));
                форма.shapeType = ParticleSystemShapeType.Sphere;
                форма.radius = 0.5f;
                основное.gravityModifier = -0.2f;
                break;
        }

        if (пресет != ПресетЧастиц.Искры && пресет != ПресетЧастиц.Взрыв && пресет != ПресетЧастиц.Пыль && пресет != ПресетЧастиц.Волшебство &&
            пресет != ПресетЧастиц.Снег && пресет != ПресетЧастиц.Дождь)
        {
            форма.shapeType = ParticleSystemShapeType.Cone;
        }

        void Настроить(float жизнь, float скоростьЧастиц, float размерЧастиц, float вСекунду, Color начало, Color конец)
        {
            основное.startLifetime = жизнь;
            основное.startSpeed = скоростьЧастиц;
            основное.startSize = размерЧастиц;
            основное.startColor = Color.white;
            появление.rateOverTime = вСекунду;

            Gradient градиент = new Gradient();
            градиент.SetKeys(
                new[] { new GradientColorKey(начало, 0), new GradientColorKey(конец, 1) },
                new[] { new GradientAlphaKey(начало.a, 0), new GradientAlphaKey(конец.a, 1) });
            цветСоВременем.color = градиент;
        }

        void Уменьшение(ParticleSystem.SizeOverLifetimeModule модуль)
        {
            модуль.enabled = true;
            модуль.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0));
        }

        if (игралДо || Application.isPlaying)
        {
            оригинал.Play();
        }
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Частицы))]
internal class ЧастицыEditor : RunityEditor<Частицы>
{
    ПресетЧастиц пресет;

    protected override void Отрисовать()
    {
        Текст("Этот объект выпускает частицы. Начните с готового пресета, а потом подправьте цифры");

        Пробел();

        НачатьГоризонтальнуюГруппу();
        пресет = (ПресетЧастиц)EditorGUILayout.EnumPopup(Подпись("Пресет", nameof(Частицы.ПрименитьПресет)), пресет);
        if (GUILayout.Button("Применить", GUILayout.Width(90)))
        {
            foreach (Частицы частицы in components)
            {
                Undo.RecordObjects(new Object[] { частицы.оригинал, частицы.GetComponent<ParticleSystemRenderer>() }, "Пресет частиц");
                частицы.ПрименитьПресет(пресет);
            }
        }
        ЗакончитьГоризонтальнуюГруппу();

        Пробел();

        Синхрополе("Цвет", x => x.цвет,
            (title, value) => EditorGUILayout.ColorField(title, value));

        Синхрополе("Размер", x => x.размер,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Скорость", x => x.скорость,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Время жизни", x => x.времяЖизни,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Количество в секунду", x => x.количествоВСекунду,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Гравитация", x => x.гравитация,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Пробел();

        Синхрополе("Повторять", x => x.повторять,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Играет на старте", x => x.играетНаСтарте,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Синхрополе("Оставлять шлейф", x => x.оставлятьШлейф,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Поле("Удалить после окончания", x => x.удалитьПослеОкончания,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При окончании", "_приОкончании");
        ЗакончитьСобытия();
    }
}
#endif
