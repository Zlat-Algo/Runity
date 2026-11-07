#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// "Уши" игрока: объёмные звуки слышны так, как их слышал бы этот объект. Обычно висит на камере
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioListener))]
[AddComponentMenu("  Runity/ Звук/Слушатель звука")]
public class СлушательЗвука : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<AudioListener>();
    void OnValidate() => Awake();
    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public AudioListener оригинал => (AudioListener)оригинальныйКомпонент;

    /// <summary>
    /// Громкость всей игры: от 0 до 1
    /// </summary>
    public static Дробное громкостьИгры
    {
        get => AudioListener.volume;
        set => AudioListener.volume = Mathf.Clamp01(value);
    }

    /// <summary>
    /// Пока "да", все звуки игры стоят на паузе
    /// </summary>
    public static Флажок звукНаПаузе
    {
        get => AudioListener.pause;
        set => AudioListener.pause = value;
    }

    /// <summary>
    /// Меняет громкость всей игры. Удобно подключать к Слайдеру
    /// </summary>
    public void УстановитьГромкость(Дробное громкость) => громкостьИгры = громкость;

    /// <summary>
    /// Выключает звук, если он был, и включает, если его не было
    /// </summary>
    public void ПереключитьЗвук() => громкостьИгры = громкостьИгры > 0 ? 0 : 1;
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(СлушательЗвука))]
internal class СлушательЗвукаEditor : RunityEditor<СлушательЗвука>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект слышит звуки игры. На сцене должен быть ровно один слушатель, обычно на камере");

        if (Object.FindObjectsByType<AudioListener>().Length > 1)
        {
            Текст("На сцене несколько слушателей звука. Оставьте одного", MessageType.Warning);
        }

        Пробел();

        Поле("Громкость игры", x => СлушательЗвука.громкостьИгры,
            (title, value) => EditorGUILayout.Slider(title, value, 0, 1));
    }
}
#endif
