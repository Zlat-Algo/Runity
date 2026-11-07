#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Запускает события при нажатии любой клавиши или кнопки мыши. Код писать не нужно
/// </summary>
[AddComponentMenu("  Runity/ Ввод/Реакция на клавишу")]
public class РеакцияНаКлавишу : RunityComponent
{
    protected override void OnEnable() { }
    protected override void OnDisable() { }

    [SerializeField] Клавиша _клавиша = Клавиша.E;
    /// <summary>
    /// На какую клавишу или кнопку мыши реагирует объект
    /// </summary>
    public Клавиша клавиша { get => _клавиша; set => _клавиша = value; }

    [SerializeField] События _приНажатии;
    /// <summary>
    /// Срабатывает один раз, когда клавишу нажали
    /// </summary>
    public События приНажатии => _приНажатии;

    [SerializeField] События _покаУдерживается;
    /// <summary>
    /// Срабатывает каждый кадр, пока клавиша зажата
    /// </summary>
    public События покаУдерживается => _покаУдерживается;

    [SerializeField] События _приОтпускании;
    /// <summary>
    /// Срабатывает один раз, когда клавишу отпустили
    /// </summary>
    public События приОтпускании => _приОтпускании;

    void Update()
    {
        if (Управление.КлавишаНажата(клавиша)) _приНажатии.Вызвать();
        if (Управление.КлавишаУдерживается(клавиша)) _покаУдерживается.Вызвать();
        if (Управление.КлавишаОтжата(клавиша)) _приОтпускании.Вызвать();
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(РеакцияНаКлавишу))]
internal class РеакцияНаКлавишуEditor : RunityEditor<РеакцияНаКлавишу>
{
    // Во время игры выбранная клавиша загорается зелёным, когда её нажимают, поэтому инспектор перерисовывается постоянно
    public override bool RequiresConstantRepaint() => Application.isPlaying;

    protected override void Отрисовать()
    {
        Текст("Этот объект что-то делает, когда игрок нажимает указанную клавишу или кнопку мыши. Нажмите на нужную клавишу ниже");

        Пробел();

        Поле("Клавиша", x => x.клавиша, (title, value) =>
        {
            EditorGUILayout.LabelField(title, new GUIContent(value.ToString()), EditorStyles.boldLabel);
            return ВыборКлавиши.Клавиатура(value);
        });

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При нажатии", "_приНажатии");
        ОтобразитьСобытие("Пока удерживается", "_покаУдерживается");
        ОтобразитьСобытие("При отпускании", "_приОтпускании");
        ЗакончитьСобытия();
    }
}
#endif
