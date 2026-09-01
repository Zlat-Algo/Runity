using System;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public enum РежимНажатияКлавиши { Нажата, Отпущена, Удерживается }

public struct ДанныеОНажатии
{
    public KeyCode клавиша;
    public РежимНажатияКлавиши режимНажатия;
}

[RequireComponent(typeof(PlayerInput), typeof(EventSystem), typeof(InputSystemUIInputModule))]
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/Управление")]
public class Управление : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<PlayerInput>();
    void OnValidate() => Awake();
    public PlayerInput оригинал => (PlayerInput)оригинальныйКомпонент;

    public InputActionAsset наборНастроек
    {
        get => оригинал.actions;
        set => оригинал.actions = value;
    }

    void OnMove(InputValue value)
    {
        Консоль.Напечатать(value.ИзвлечьV2());
        _приДвижении.Invoke();
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
            component.наборНастроек = НайтиАссет<InputActionAsset>("198cc02dd732403458fa293529a9203c");
            component.оригинал.defaultActionMap = "Player";
        });

        Пробел();

        НачатьСобытия();
        Событие("При движении", "_приДвижении");
        ЗакончитьСобытия();
    }
}
#endif