using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Механики/Взаимодействие")]
public class Взаимодействие : RunityComponent
{
    [SerializeField] float _дальностьВзаимодействия = 5;
    public float дальностьВзаимодействия{ get => _дальностьВзаимодействия; set => _дальностьВзаимодействия = value;}

    [SerializeField] bool _включен = да;
    public bool включен { get => _включен; set => _включен = value;}

    [SerializeField] bool _многоразовый;
    public bool многоразовый { get => _многоразовый; set => _многоразовый = value; }

    [SerializeField] bool _ЛКМВместоЕ;
    public bool ЛКМВместоЕ { get => _ЛКМВместоЕ; set => _ЛКМВместоЕ = value; }

    bool камераБлизко => (transform.position - Camera.main.transform.position).magnitude <= дальностьВзаимодействия;
    bool взаимодействиеДоступно;

    [SerializeField] UnityEvent _когдаМожноВзаимодействовать;
    public UnityEvent когдаМожноВзаимодействовать => _когдаМожноВзаимодействовать;
    [SerializeField] UnityEvent _когдаНельзяВзаимодействовать;
    public UnityEvent когдаНельзяВзаимодействовать => _когдаНельзяВзаимодействовать;
    [SerializeField] UnityEvent _когдаВзаимодействует;
    public UnityEvent когдаВзаимодействует => _когдаВзаимодействует;

    void OnMouseOver()
    {
        if (включен && камераБлизко && !взаимодействиеДоступно)
        {
            взаимодействиеДоступно = да;
            _когдаМожноВзаимодействовать.Вызвать();
        }
        else if (взаимодействиеДоступно && !(включен && камераБлизко))
        {
            взаимодействиеДоступно = нет;
            _когдаНельзяВзаимодействовать.Вызвать();
        }
    }

    void OnMouseExit()
    {
        if (взаимодействиеДоступно)
        {
            взаимодействиеДоступно = нет;
            _когдаНельзяВзаимодействовать.Вызвать();
        }
    }

    void Update()
    {
        if (включен && взаимодействиеДоступно && (!ЛКМВместоЕ && Управление.ИспользоватьНажато) || (ЛКМВместоЕ && Управление.ЛКМНажата))
        {
            _когдаВзаимодействует.Вызвать();
            if (!многоразовый) включен = false; 
        }
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Взаимодействие))]
public class ВзаимодействиеEditor : RunityEditor<Взаимодействие>
{
    bool eventFoldout = false;

    public override void OnInspectorGUI()
    {
        Текст("С этим объектом можно взаимодействовать, чтобы он делал какое-то действие");

        Пробел();

        Поле("Дальность взаимодействия", x => x.дальностьВзаимодействия,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Поле("Включен", x => x.включен,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Поле("Многоразовый", x => x.многоразовый,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        компонент.ЛКМВместоЕ = GUILayout.Toolbar(компонент.ЛКМВместоЕ ? 1 : 0, new string[] { "E", "ЛКМ" }) == 1;

        Пробел();

        eventFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(eventFoldout, "События");
        if (eventFoldout)
        {
            НачатьСобытия();

            Событие("Когда можно взаимодействовать", "_когдаМожноВзаимодействовать");
            Событие("Когда нельзя взаимодействовать", "_когдаНельзяВзаимодействовать");
            Пробел();
            Событие("Когда взаимодействует", "_когдаВзаимодействует");

            ЗакончитьСобытия();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        Пробел();
    }
}
#endif