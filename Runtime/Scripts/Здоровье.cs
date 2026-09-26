using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public enum ДействиеПриПотереЗдоровья { УдалениеСебя, ПерезапускСцены, УказанноеДействие}

[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Здоровье")]
public class Здоровье : RunityComponent
{
    [SerializeField] int _текущее = 10;
    public int текущее
    {
        get => _текущее;
        set
        {
            _текущее = value;
            if (_текущее <= 0)
            {
                _текущее = 0;
                switch (действиеПриПотереЗдоровья)
                {
                    case ДействиеПриПотереЗдоровья.УдалениеСебя:
                        Удалить(объект);
                        break;
                    case ДействиеПриПотереЗдоровья.ПерезапускСцены:
                        Сцены.Перезапустить();
                        break;
                    case ДействиеПриПотереЗдоровья.УказанноеДействие:
                        _когдаЗакончилосьЗдоровье.Вызвать();
                        break;
                }
            }
        }
    }

    [SerializeField] int _максимальное = 10;
    public int максимальное { get => _максимальное; set => _максимальное = value; }

    [SerializeField] ДействиеПриПотереЗдоровья _действиеПриПотереЗдоровья;
    public ДействиеПриПотереЗдоровья действиеПриПотереЗдоровья { get => _действиеПриПотереЗдоровья; set => _действиеПриПотереЗдоровья = value; }

    [SerializeField] UnityEvent _когдаЗакончилосьЗдоровье;
    public UnityEvent когдаЗакончилосьЗдоровье => _когдаЗакончилосьЗдоровье;
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Здоровье))]
public class ЗдоровьеEditor : RunityEditor<Здоровье>
{
    public override void OnInspectorGUI()
    {
        Текст("Этот объект имеет определённое количество здоровья и что-то сделает при его потере");

        Пробел();

        Синхрополе("Текущее", x => x.текущее,
            (title, value) => EditorGUILayout.IntField(title, value));

        Синхрополе("Максимальное", x => x.максимальное,
            (title, value) => EditorGUILayout.IntField(title, value));

        Пробел();

        компонент.действиеПриПотереЗдоровья = EnumField("Действие при потере здоровья", x => x.действиеПриПотереЗдоровья,
            (component, value) => component.действиеПриПотереЗдоровья = value);

        Пробел();

        if (компонент.действиеПриПотереЗдоровья == ДействиеПриПотереЗдоровья.УказанноеДействие)
        {
            НачатьСобытия();
            Событие("Когда закончилось здоровье", "_когдаЗакончилосьЗдоровье");
            ЗакончитьСобытия();
        }
    }
}
#endif
