#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Намертво скрепляет два физических объекта, но позволяет соединению порваться от сильного удара
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(FixedJoint))]
[AddComponentMenu("  Runity/ Физика 3D/ Соединения/Жёсткое соединение")]
public class ЖёсткоеСоединение : Соединение3D
{
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(ЖёсткоеСоединение))]
internal class ЖёсткоеСоединениеEditor : Соединение3DEditor<ЖёсткоеСоединение>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект скреплён с другим объектом. Укажите прочность, чтобы соединение могло порваться");

        Пробел();

        ОбщиеНастройки();

        Пробел();

        ОбщиеСобытия();
    }
}
#endif
