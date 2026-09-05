using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

[AddComponentMenu("  Runity/ Работа с объектами/Таймер")]
public class Таймер : RunityComponent
{
    [SerializeField] bool _пауза;
    public bool пауза
    {
        get => _пауза;
        set => _пауза = value;
    }

    [SerializeField] float _таймер = 1;
    public float таймер
    {
        get => _таймер;
        set => _таймер = value;
    }

    [SerializeField] bool _повторять;
    public bool повторять
    {
        get => _повторять;
        set => _повторять = value;
    }

    [SerializeField] Диапазон _междуКаждымСрабатыванием;
    public Диапазон междуКаждымСрабатыванием
    {
        get => _междуКаждымСрабатыванием;
        set => _междуКаждымСрабатыванием = value;
    }

    [SerializeField] UnityEvent _действия;
    public UnityEvent действия
    {
        get => _действия;
        set => _действия = value;
    }

    void FixedUpdate()
    {
        if (пауза) return;

        if (таймер > 0)
        {
            таймер -= Время.времяМеждуFixedUpdate;
            if (таймер <= 0)
            {
                Срабатывание();
            }
        }
    }

    void Срабатывание()
    {
        действия.Invoke();
        if (повторять)
        {
            таймер = междуКаждымСрабатыванием.ПолучитьЗначение();
        }
        else
        {
            таймер = 0;
        }
    }

    public void СрочноеСрабатывание()
    {
        Срабатывание();
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Таймер))]
public class ТаймерEditor : RunityEditor<Таймер>
{
    public override void OnInspectorGUI()
    {
        Текст("Этот объект сделает какие-то действия через указанное в таймере время, а потом будет повторять эти действия, если включен повтор");

        Пробел();

        Поле("Пауза", x => x.пауза, (title, value) => EditorGUILayout.Toggle(title, value));
        Поле("Таймер", x => x.таймер, (title, value) => EditorGUILayout.FloatField(title, value));
        Поле("Повторять", x => x.повторять, (title, value) => EditorGUILayout.Toggle(title, value));
        ПолеСДиапазоном("Между каждым срабатыванием", x => x.междуКаждымСрабатыванием);

        Пробел();

        НачатьСобытия();
        Событие("Действия", "_действия");
        ЗакончитьСобытия();

        Пробел();

        Кнопка("Срочное срабатывание", компонент.СрочноеСрабатывание);

    }
}
#endif