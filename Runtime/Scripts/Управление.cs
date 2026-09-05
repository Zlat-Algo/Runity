using System;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

/*public enum РежимНажатияКлавиши { Нажата, Отпущена, Удерживается }

public struct ДанныеОНажатии
{
    public KeyCode клавиша;
    public РежимНажатияКлавиши режимНажатия;
}*/

[RequireComponent(typeof(PlayerInput)/*, typeof(EventSystem), typeof(InputSystemUIInputModule)*/)]
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/Управление")]
public class Управление : RunityComponent
{
    void Awake()
    {
        оригинальныйКомпонент = GetComponent<PlayerInput>();
        //главное = this;
    }
    void OnValidate() => Awake();
    public PlayerInput оригинал => (PlayerInput)оригинальныйКомпонент;

    //public static Управление главное;

    public InputActionAsset наборНастроек
    {
        get => оригинал.actions;
        set => оригинал.actions = value;
    }

    public static bool вперёдНажато { get; private set; }
    public static bool вперёдУдерживается { get; private set; }
    public static bool вперёдОтжато { get; private set; }
    static bool вперёдИзменено;
    void OnВперёд(InputValue данные)
    {
        вперёдНажато = данные.isPressed;
        вперёдУдерживается = данные.isPressed;
        вперёдОтжато = !данные.isPressed;
        вперёдИзменено = да;
    }


    public static bool влевоНажато { get; private set; }
    public static bool влевоУдерживается { get; private set; }
    public static bool влевоОтжато { get; private set; }
    static bool влевоИзменено;
    void OnВлево(InputValue данные)
    {
        влевоНажато = данные.isPressed;
        влевоУдерживается = данные.isPressed;
        влевоОтжато = !данные.isPressed;
        влевоИзменено = да;
    }

    public static bool назадНажато { get; private set; }
    public static bool назадУдерживается { get; private set; }
    public static bool назадОтжато { get; private set; }
    static bool назадИзменено;
    void OnНазад(InputValue данные)
    {
        назадНажато = данные.isPressed;
        назадУдерживается = данные.isPressed;
        назадОтжато = !данные.isPressed;
        назадИзменено = да;
    }

    public static bool вправоНажато { get; private set; }
    public static bool вправоУдерживается { get; private set; }
    public static bool вправоОтжато { get; private set; }
    static bool вправоИзменено;
    void OnВправо(InputValue данные)
    {
        вправоНажато = данные.isPressed;
        вправоУдерживается = данные.isPressed;
        вправоОтжато = !данные.isPressed;
        вправоИзменено = да;
    }

    public static bool движениеНачато { get; private set; }
    public static bool движениеУдерживается { get; private set; }
    public static bool движениеЗакончено { get; private set; }
    static bool движениеИзменено;
    /*void OnДвижение(InputValue данные)
    {
        движениеНажато = данные.isPressed;
        движениеУдерживается = данные.isPressed;
        движениеОтжато = !данные.isPressed;
        движениеИзменено = да;
    }*/

    public static bool пробелНажат { get; private set; }
    public static bool пробелУдерживается { get; private set; }
    public static bool пробелОтжат { get; private set; }
    static bool пробелИзменено;
    void OnПробел(InputValue данные)
    {
        пробелНажат = данные.isPressed;
        пробелУдерживается = данные.isPressed;
        пробелОтжат = !данные.isPressed;
        пробелИзменено = да;
    }

    public static bool выходНажат { get; private set; }
    public static bool выходУдерживается { get; private set; }
    public static bool выходОтжат { get; private set; }
    static bool выходИзменено;
    void OnВыход(InputValue данные)
    {
        выходНажат = данные.isPressed;
        выходУдерживается = данные.isPressed;
        выходОтжат = !данные.isPressed;
        выходИзменено = да;
    }

    public static bool ЛКМНажата { get; private set; }
    public static bool ЛКМУдерживается { get; private set; }
    public static bool ЛКМОтжата { get; private set; }
    static bool ЛКМИзменено;
    void OnЛКМ(InputValue данные)
    {
        ЛКМНажата = данные.isPressed;
        ЛКМУдерживается = данные.isPressed;
        ЛКМОтжата = !данные.isPressed;
        ЛКМИзменено = да;
    }

    public static bool ПКМНажата { get; private set; }
    public static bool ПКМУдерживается { get; private set; }
    public static bool ПКМОтжата { get; private set; }
    static bool ПКМИзменено;
    void OnПКМ(InputValue данные)
    {
        ПКМНажата = данные.isPressed;
        ПКМУдерживается = данные.isPressed;
        ПКМОтжата = !данные.isPressed;
        ПКМИзменено = да;
    }

    void Update()
    {
        if (вперёдИзменено) вперёдИзменено = false; else
        {
            вперёдНажато = false;
            вперёдОтжато = false;
        }
        if (влевоИзменено) влевоИзменено = false;
        else
        {
            влевоНажато = false;
            влевоОтжато = false;
        }
        if (назадИзменено) назадИзменено = false;
        else
        {
            назадНажато = false;
            назадОтжато = false;
        }
        if (вправоИзменено) вправоИзменено = false;
        else
        {
            вправоНажато = false;
            вправоОтжато = false;
        }
        if (!движениеУдерживается && (вперёдУдерживается || влевоУдерживается || назадУдерживается || вправоУдерживается))
        {
            движениеНачато = true;
            движениеУдерживается = true;
            движениеЗакончено = false;
            движениеИзменено = true;
        }
        if (движениеУдерживается && !(вперёдУдерживается || влевоУдерживается || назадУдерживается || вправоУдерживается))
        {
            движениеНачато = false;
            движениеУдерживается = false;
            движениеЗакончено = true;
            движениеИзменено = true;
        }
        if (движениеИзменено) движениеИзменено = false;
        else
        {
            движениеНачато = false;
            движениеЗакончено = false;
        }
        
        if (пробелИзменено) пробелИзменено = false;
        else
        {
            пробелНажат = false;
            пробелОтжат = false;
        }
        if (выходИзменено) выходИзменено = false;
        else
        {
            выходНажат = false;
            выходОтжат = false;
        }
        if (ЛКМИзменено) ЛКМИзменено = false;
        else
        {
            ЛКМНажата = false;
            ЛКМОтжата = false;
        }
        if (ПКМИзменено) ПКМИзменено = false;
        else
        {
            ПКМНажата = false;
            ПКМОтжата = false;
        }
    }

    [SerializeField] UnityEvent _приДвижении;
    public UnityEvent приДвижении => _приДвижении;
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Управление))]
public class УправлениеEditor : RunityEditor<Управление>
{
    public override void OnInspectorGUI()
    {
        Текст("Этот объект реагирует на нажатия кнопок и движения мышкой");

        Пробел();

        Синхрополе("Набор настроек", x => x.наборНастроек,
            (title, value) => Объект<InputActionAsset>(title, value));

        Кнопка("Стандартные настройки", () =>
        {
            компонент.наборНастроек = НайтиАссет<InputActionAsset>("198cc02dd732403458fa293529a9203c");
            компонент.оригинал.defaultActionMap = "Стандартная";
        });

        Пробел();

        НачатьСобытия();
        //Событие("При движении", "_приДвижении");
        ЗакончитьСобытия();
    }
}
#endif