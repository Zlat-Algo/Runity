using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Готовые действия чит-кодов. Здоровье и его действия применяются к объекту с Читером
/// </summary>
public enum ДействиеЧита { ТолькоСобытия, ПолноеЗдоровье, ПереключитьБессмертие, ПерезапуститьСцену, СледующаяСцена, УскоритьИгру, ЗамедлитьИгру, ОбычнаяСкоростьИгры }

/// <summary>
/// Один чит-код: что нужно ввести и что от этого произойдёт
/// </summary>
[Serializable]
public class ЧитКод
{
    /// <summary>
    /// Что нужно набрать на клавиатуре. Регистр и язык учитываются как есть: "iddqd" и "вжух" - разные коды
    /// </summary>
    public string код = "iddqd";
    /// <summary>
    /// Готовое действие
    /// </summary>
    public ДействиеЧита действие;
    /// <summary>
    /// Что ещё случится при вводе кода
    /// </summary>
    public События события;
}

/// <summary>
/// Следит за тем, что набирает игрок, и выполняет чит-коды
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Ввод/Читер")]
public class Читер : RunityComponent
{
    protected override void OnEnable()
    {
        if (Keyboard.current != null) Keyboard.current.onTextInput += ПриВводеСимвола;
    }
    protected override void OnDisable()
    {
        if (Keyboard.current != null) Keyboard.current.onTextInput -= ПриВводеСимвола;
    }

    [SerializeField] List<ЧитКод> _коды = new List<ЧитКод> { new ЧитКод() };
    /// <summary>
    /// Все чит-коды этого объекта
    /// </summary>
    public List<ЧитКод> коды { get => _коды; set => _коды = value; }

    [SerializeField] Дробное _паузаСброса = 3;
    /// <summary>
    /// Если игрок не нажимает клавиши столько секунд, то набранный текст забывается
    /// </summary>
    public Дробное паузаСброса { get => _паузаСброса; set => _паузаСброса = value; }

    [SerializeField] Дробное _ускорениеИгры = 2;
    /// <summary>
    /// Во сколько раз быстрее идёт игра с читом "Ускорить игру"
    /// </summary>
    public Дробное ускорениеИгры { get => _ускорениеИгры; set => _ускорениеИгры = value; }

    [SerializeField] Дробное _замедлениеИгры = 0.5;
    /// <summary>
    /// Скорость игры с читом "Замедлить игру"
    /// </summary>
    public Дробное замедлениеИгры { get => _замедлениеИгры; set => _замедлениеИгры = value; }

    [SerializeField] События _приЛюбомЧите;
    /// <summary>
    /// Срабатывает при вводе любого чит-кода
    /// </summary>
    public События приЛюбомЧите => _приЛюбомЧите;

    /// <summary>
    /// Последний введённый чит-код
    /// </summary>
    public Строка последнийКод { get; private set; }

    string набранное = "";
    float времяПоследнегоСимвола;

    /// <summary>
    /// Что игрок успел набрать
    /// </summary>
    public Строка набранныйТекст => набранное;

    void ПриВводеСимвола(char символ)
    {
        if (char.IsControl(символ)) return;

        // onTextInput приходит между кадрами, поэтому берём время без учёта скорости игры
        if (Time.unscaledTime - времяПоследнегоСимвола > паузаСброса)
        {
            набранное = "";
        }
        времяПоследнегоСимвола = Time.unscaledTime;

        ВвестиТекст(символ.ToString());
    }

    /// <summary>
    /// Добавляет текст так, будто его набрал игрок. Удобно, если код вводится через Поле ввода
    /// </summary>
    public void ВвестиТекст(Строка текст)
    {
        набранное += текст;

        int самыйДлинный = 0;

        foreach (ЧитКод код in коды)
        {
            if (string.IsNullOrEmpty(код.код)) continue;

            самыйДлинный = Mathf.Max(самыйДлинный, код.код.Length);

            if (набранное.EndsWith(код.код, StringComparison.Ordinal))
            {
                Выполнить(код);
                набранное = "";
                return;
            }
        }

        if (набранное.Length > самыйДлинный)
        {
            набранное = набранное.Substring(набранное.Length - самыйДлинный);
        }
    }

    void Выполнить(ЧитКод код)
    {
        последнийКод = код.код;

        switch (код.действие)
        {
            case ДействиеЧита.ПолноеЗдоровье:
                if (TryGetComponent(out Здоровье здоровье)) здоровье.ВылечитьПолностью();
                break;
            case ДействиеЧита.ПереключитьБессмертие:
                if (TryGetComponent(out Здоровье бессмертный)) бессмертный.бессмертие = !бессмертный.бессмертие;
                break;
            case ДействиеЧита.ПерезапуститьСцену:
                Сцены.Перезапустить();
                break;
            case ДействиеЧита.СледующаяСцена:
                Сцены.ЗапуститьСледующую();
                break;
            case ДействиеЧита.УскоритьИгру:
                Время.скоростьИгры = ускорениеИгры;
                break;
            case ДействиеЧита.ЗамедлитьИгру:
                Время.скоростьИгры = замедлениеИгры;
                break;
            case ДействиеЧита.ОбычнаяСкоростьИгры:
                Время.скоростьИгры = 1;
                break;
        }

        код.события.Вызвать();
        _приЛюбомЧите.Вызвать();
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Читер))]
internal class ЧитерEditor : RunityEditor<Читер>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект следит за тем, что набирает игрок. Когда игрок набирает чит-код, срабатывает готовое действие и указанные события");

        Пробел();

        Поле("Пауза сброса", x => x.паузаСброса,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Поле("Ускорение игры", x => x.ускорениеИгры,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Поле("Замедление игры", x => x.замедлениеИгры,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Пробел();

        НачатьСобытия();
        ОтобразитьСвойство("Чит-коды", "_коды");
        Пробел();
        ОтобразитьСобытие("При любом чите", "_приЛюбомЧите");
        ЗакончитьСобытия();
    }
}
#endif
