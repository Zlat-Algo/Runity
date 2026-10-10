using UnityEditor;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("  Runity/Курсор")]
public class Курсор : RunityComponent
{
    [SerializeField] Флажок _следуетПоОсиX = да;
    public Флажок следуетПоОсиX { get => _следуетПоОсиX; set => _следуетПоОсиX = value; }

    [SerializeField] Флажок _следуетПоОсиY = да;
    public Флажок следуетПоОсиY { get => _следуетПоОсиY; set => _следуетПоОсиY = value; }

    /*[SerializeField] Дробное _скорость;
    public Дробное скорость { get => _скорость; set => _скорость = value; }*/

    private void Update()
    {
        if (следуетПоОсиX)
        {
            весьОбъект.трансформация.позицияX = Мышка.позицияВМире2D.x;
        }
        if (следуетПоОсиY)
        {
            весьОбъект.трансформация.позицияY = Мышка.позицияВМире2D.y;
        }
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Курсор))]
internal class КурсорEditor : RunityEditor<Курсор>
{
    public override void OnInspectorGUI()
    {
        Текст("Этот объект следует за курсором");

        Пробел();

        Поле("Следует по оси X", x => x.следуетПоОсиX,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Поле("Следует по оси Y", x => x.следуетПоОсиY,
            (title, value) => EditorGUILayout.Toggle(title, value));
    }
}
#endif