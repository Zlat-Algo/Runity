using UnityEditor;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Работа с объектами/Вращатель 2D")]
public class Вращатель2D : RunityComponent
{
    [SerializeField] float _скорость = 45;
    public float скорость { get => _скорость; set => _скорость = value; }

    void FixedUpdate()
    {
        gameObject.Повернуть2D(скорость * Время.времяМеждуFixedUpdate);
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Вращатель2D))]
public class Вращатель2DEditor : RunityEditor<Вращатель2D>
{
    public override void OnInspectorGUI()
    {
        Текст("Этот объект будет вращаться с указанной скоростью");

        Пробел();

        Поле("Скорость", x => x.скорость,
            (title, value) => EditorGUILayout.FloatField(title, value));
    }
}
#endif

