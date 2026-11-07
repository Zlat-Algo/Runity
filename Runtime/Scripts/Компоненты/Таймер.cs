#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Выполняет действия через указанное время
/// </summary>
[AddComponentMenu("  Runity/ Работа с объектами/Таймер")]
public class Таймер : RunityComponent
{
    /// <summary>
    /// Пока "да", таймер стоит на месте
    /// </summary>
    [SerializeField] bool _пауза;
    public bool пауза
    {
        get => _пауза;
        set => _пауза = value;
    }

    /// <summary>
    /// Сколько секунд осталось до срабатывания
    /// </summary>
    [SerializeField] float _таймер = 1;
    public float таймер
    {
        get => _таймер;
        set => _таймер = value;
    }

    /// <summary>
    /// Если "да", то таймер запустится снова после срабатывания
    /// </summary>
    [SerializeField] bool _повторять;
    public bool повторять
    {
        get => _повторять;
        set => _повторять = value;
    }

    /// <summary>
    /// Сколько секунд проходит между срабатываниями при повторе
    /// </summary>
    [SerializeField] Дробное _междуКаждымСрабатыванием;
    public Дробное междуКаждымСрабатыванием
    {
        get => _междуКаждымСрабатыванием;
        set => _междуКаждымСрабатыванием = value;
    }

    /// <summary>
    /// Что произойдёт при срабатывании
    /// </summary>
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
            таймер = междуКаждымСрабатыванием;
        }
        else
        {
            таймер = 0;
        }
    }

    /// <summary>
    /// Запускает действия прямо сейчас
    /// </summary>
    public void СрочноеСрабатывание()
    {
        Срабатывание();
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Таймер))]
internal class ТаймерEditor : RunityEditor<Таймер>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект сделает какие-то действия через указанное в таймере время, а потом будет повторять эти действия, если включен повтор");

        Пробел();

        Поле("Пауза", x => x.пауза, (title, value) => EditorGUILayout.Toggle(title, value));
        Поле("Таймер", x => x.таймер, (title, value) => EditorGUILayout.FloatField(title, value));
        Поле("Повторять", x => x.повторять, (title, value) => EditorGUILayout.Toggle(title, value));
        Поле("Между каждым срабатыванием", x => x.междуКаждымСрабатыванием, (title, value) => EditorGUILayout.FloatField(title, value));

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("Действия", "_действия");
        ЗакончитьСобытия();

        Пробел();

        Кнопка("Срочное срабатывание", компонент.СрочноеСрабатывание);

    }
}
#endif