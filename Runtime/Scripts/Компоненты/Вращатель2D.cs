#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Постоянно вращает объект
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Работа с объектами/ Движение/Вращатель 2D")]
public class Вращатель2D : RunityComponent
{
    /// <summary>
    /// Сколько градусов в секунду проходит объект
    /// </summary>
    [SerializeField] float _скорость = 45;
    public float скорость { get => _скорость; set => _скорость = value; }

    void FixedUpdate()
    {
        весьОбъект.трансформация.поворотZ += скорость * Время.времяМеждуFixedUpdate;
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Вращатель2D))]
internal class Вращатель2DEditor : RunityEditor<Вращатель2D>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект будет вращаться с указанной скоростью");

        Пробел();

        Поле("Скорость", x => x.скорость,
            (title, value) => EditorGUILayout.FloatField(title, value));
    }
}
#endif

