#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Проигрывает анимации из Animator Controller и управляет его параметрами
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
[AddComponentMenu("  Runity/ Анимация/Аниматор")]
public class Аниматор : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<Animator>();
    void OnValidate() => Awake();
    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public Animator оригинал => (Animator)оригинальныйКомпонент;

    /// <summary>
    /// Схема анимаций: какие анимации есть и как они переключаются
    /// </summary>
    public RuntimeAnimatorController контроллер
    {
        get => оригинал.runtimeAnimatorController;
        set => оригинал.runtimeAnimatorController = value;
    }

    /// <summary>
    /// Скорость анимаций: 1 - обычная, 2 - в два раза быстрее, 0 - стоп-кадр
    /// </summary>
    public Дробное скорость
    {
        get => оригинал.speed;
        set => оригинал.speed = value;
    }

    /// <summary>
    /// Если "да", то анимация сможет двигать сам объект
    /// </summary>
    public Флажок движениеИзАнимации
    {
        get => оригинал.applyRootMotion;
        set => оригинал.applyRootMotion = value;
    }

    /// <summary>
    /// Сразу включает анимацию с таким названием
    /// </summary>
    public void Запустить(Строка названиеАнимации)
    {
        оригинал.Play(названиеАнимации.ToString());
    }

    /// <summary>
    /// Плавно переходит к анимации с таким названием за указанное время в секундах
    /// </summary>
    public void ЗапуститьПлавно(Строка названиеАнимации, Дробное времяПерехода)
    {
        оригинал.CrossFadeInFixedTime(названиеАнимации.ToString(), времяПерехода);
    }

    /// <summary>
    /// Даёт "да", если сейчас играет анимация с таким названием
    /// </summary>
    public Флажок Играет(Строка названиеАнимации)
    {
        return оригинал.GetCurrentAnimatorStateInfo(0).IsName(названиеАнимации);
    }

    /// <summary>
    /// Срабатывает параметр-триггер
    /// </summary>
    public void ВключитьТриггер(Строка названиеПараметра)
    {
        оригинал.SetTrigger(названиеПараметра.ToString());
    }

    /// <summary>
    /// Отменяет параметр-триггер, если он ещё не сработал
    /// </summary>
    public void СброситьТриггер(Строка названиеПараметра)
    {
        оригинал.ResetTrigger(названиеПараметра.ToString());
    }

    /// <summary>
    /// Устанавливает параметр типа Bool
    /// </summary>
    public void УстановитьФлажок(Строка названиеПараметра, Флажок значение)
    {
        оригинал.SetBool(названиеПараметра.ToString(), значение);
    }

    /// <summary>
    /// Устанавливает параметр типа Int
    /// </summary>
    public void УстановитьЧисло(Строка названиеПараметра, Число значение)
    {
        оригинал.SetInteger(названиеПараметра.ToString(), значение);
    }

    /// <summary>
    /// Устанавливает параметр типа Float
    /// </summary>
    public void УстановитьДробное(Строка названиеПараметра, Дробное значение)
    {
        оригинал.SetFloat(названиеПараметра.ToString(), значение);
    }

    /// <summary>
    /// Даёт значение параметра типа Bool
    /// </summary>
    public Флажок ПолучитьФлажок(Строка названиеПараметра) => оригинал.GetBool(названиеПараметра.ToString());

    /// <summary>
    /// Даёт значение параметра типа Int
    /// </summary>
    public Число ПолучитьЧисло(Строка названиеПараметра) => оригинал.GetInteger(названиеПараметра.ToString());

    /// <summary>
    /// Даёт значение параметра типа Float
    /// </summary>
    public Дробное ПолучитьДробное(Строка названиеПараметра) => оригинал.GetFloat(названиеПараметра.ToString());
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Аниматор))]
internal class АниматорEditor : RunityEditor<Аниматор>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект проигрывает анимации по схеме из Animator Controller. Для простой покадровой анимации удобнее Проигрыватель кадров");

        Пробел();

        Синхрополе("Контроллер", x => x.контроллер,
            (title, value) => ПолеОбъекта<RuntimeAnimatorController>(title, value));

        Синхрополе("Скорость", x => x.скорость,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Синхрополе("Движение из анимации", x => x.движениеИзАнимации,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        Кнопка("Открыть схему анимаций", () => EditorApplication.ExecuteMenuItem("Window/Animation/Animator"));
    }
}
#endif
