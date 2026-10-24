using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Механики/Взаимодействие")]
public class Взаимодействие : RunityComponent
{
    protected override void OnEnable() { }
    protected override void OnDisable() { }

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

    [SerializeField] События _когдаМожноВзаимодействовать;
    public События когдаМожноВзаимодействовать => _когдаМожноВзаимодействовать;
    [SerializeField] События _когдаНельзяВзаимодействовать;
    public События когдаНельзяВзаимодействовать => _когдаНельзяВзаимодействовать;
    [SerializeField] События _когдаВзаимодействует;
    public События когдаВзаимодействует => _когдаВзаимодействует;

    void OnMouseOver()
    {
        //if (вРедакторе) return;
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
        //if (вРедакторе) return;
        if (взаимодействиеДоступно)
        {
            взаимодействиеДоступно = нет;
            _когдаНельзяВзаимодействовать.Вызвать();
        }
    }

    void Update()
    {
        //if (вРедакторе) return;
        if (включен && взаимодействиеДоступно && ((!ЛКМВместоЕ && Управление.использоватьНажато) || (ЛКМВместоЕ && Управление.ЛКМНажата)))
        {
            _когдаВзаимодействует.Вызвать();
            if (!многоразовый) включен = false; 
        }
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Взаимодействие))]
internal class ВзаимодействиеEditor : RunityEditor<Взаимодействие>
{
    //bool eventFoldout = true;

    public override void OnInspectorGUI()
    {
        Текст("С этим объектом можно взаимодействовать, чтобы он делал какое-то действие");

        Пробел();

        ПредупреждениеПроУправление();

        Поле("Дальность взаимодействия", x => x.дальностьВзаимодействия,
            (title, value) => EditorGUILayout.FloatField(title, value));

        Поле("Включен", x => x.включен,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Поле("Многоразовый", x => x.многоразовый,
            (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();

        компонент.ЛКМВместоЕ = GUILayout.Toolbar(компонент.ЛКМВместоЕ ? 1 : 0, new string[] { "E", "ЛКМ" }) == 1;

        Пробел();

        //eventFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(eventFoldout, "События");
        //if (eventFoldout)
        {
            НачатьСобытия();

            ОтобразитьСобытие("Когда можно взаимодействовать", "_когдаМожноВзаимодействовать");
            ОтобразитьСобытие("Когда нельзя взаимодействовать", "_когдаНельзяВзаимодействовать");
            Пробел();
            ОтобразитьСобытие("Когда взаимодействует", "_когдаВзаимодействует");

            ЗакончитьСобытия();
        }
        //EditorGUILayout.EndFoldoutHeaderGroup();

        Пробел();
    }
}
#endif