using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Объект, который есть у всех игроков сетевой игры: его положение одинаковое у всех, а управлять им может только владелец
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject), typeof(СетевойМост))]
[AddComponentMenu("  Runity/ Сеть/Сетевой объект")]
public class СетевойОбъект : RunityComponent
{
    void Awake() => оригинальныйКомпонент = GetComponent<NetworkObject>();

    protected override void OnEnable() { }
    protected override void OnDisable() { }

    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public NetworkObject оригинал => (NetworkObject)оригинальныйКомпонент;

    [SerializeField] Флажок _синхронизироватьПоложение = да;
    /// <summary>
    /// Если "да", то положение, поворот и размер объекта у всех игроков одинаковые. Двигает объект его владелец
    /// </summary>
    public Флажок синхронизироватьПоложение
    {
        get => _синхронизироватьПоложение;
        set { _синхронизироватьПоложение = value; ОбновитьКомпоненты(); }
    }

    [SerializeField] List<Behaviour> _толькоУВладельца = new List<Behaviour>();
    /// <summary>
    /// Компоненты, которые работают только у владельца: управление, камера, слушатель звука. У чужих копий они выключаются, иначе клавиши одного игрока двигали бы всех
    /// </summary>
    public List<Behaviour> толькоУВладельца => _толькоУВладельца;

    [SerializeField] List<События> _событияДляВсех = new List<События>();
    /// <summary>
    /// События, которые срабатывают сразу у всех игроков. Запускаются методом «Вызвать у всех» с номером события
    /// </summary>
    public List<События> событияДляВсех => _событияДляВсех;

    [SerializeField] События _приПоявлении;
    /// <summary>
    /// Срабатывает, когда объект появился в сетевой игре
    /// </summary>
    public События приПоявлении => _приПоявлении;

    [SerializeField] События _приПоявленииСвоего;
    /// <summary>
    /// Срабатывает, когда появился объект, которым управляет этот игрок. Например, его персонаж
    /// </summary>
    public События приПоявленииСвоего => _приПоявленииСвоего;

    /// <summary>
    /// Даёт "да", если этим объектом управляет этот игрок
    /// </summary>
    public Флажок свой => оригинал != null && оригинал.IsSpawned && оригинал.IsOwner;

    /// <summary>
    /// Даёт "да", если это персонаж какого-то игрока
    /// </summary>
    public Флажок этоИгрок => оригинал != null && оригинал.IsPlayerObject;

    /// <summary>
    /// Номер игрока, который управляет объектом. У хозяина номер 0
    /// </summary>
    public Число номерВладельца => оригинал != null ? (int)оригинал.OwnerClientId : 0;

    internal void ПриПоявленииВСети()
    {
        bool мой = оригинал.IsOwner;
        foreach (Behaviour поведение in _толькоУВладельца)
        {
            if (поведение != null) поведение.enabled = мой;
        }

        _приПоявлении.Вызвать();
        if (мой) _приПоявленииСвоего.Вызвать();
    }

    /// <summary>
    /// Вызывает событие из списка «События для всех» у всех игроков сразу. События считаются с нуля
    /// </summary>
    public void ВызватьУВсех(Число номерСобытия)
    {
        if (оригинал == null || !оригинал.IsSpawned)
        {
            ВызватьЗдесь(номерСобытия);
            return;
        }
        GetComponent<СетевойМост>().ВызватьУВсехRpc(номерСобытия);
    }

    internal void ВызватьЗдесь(int номер)
    {
        if (номер >= 0 && номер < _событияДляВсех.Count) _событияДляВсех[номер].Вызвать();
    }

    /// <summary>
    /// Убирает объект у всех игроков. Работает у хозяина
    /// </summary>
    public void УдалитьУВсех()
    {
        if (оригинал.IsSpawned && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) оригинал.Despawn(true);
        else if (!оригинал.IsSpawned) Destroy(gameObject);
    }

    /// <summary>
    /// Находит компоненты, которыми игрок управляет с клавиатуры, и добавляет их в «Только у владельца»
    /// </summary>
    public void НайтиУправление()
    {
        foreach (Behaviour поведение in GetComponentsInChildren<Behaviour>(true))
        {
            if (_толькоУВладельца.Contains(поведение)) continue;
            bool управление = поведение is Передвижение || поведение is Прыжок || поведение is Бег || поведение is ОбзорМышкой
                || поведение is РеакцияНаКлавишу || поведение is РасширениеПерсонажа || поведение is Камера || поведение is Camera
                || поведение is AudioListener || поведение is СлушательЗвука || поведение is Атакер || поведение is Выбор;
            if (управление) _толькоУВладельца.Add(поведение);
        }
    }

    // Компоненты Unity для синхронизации ставятся заранее, в редакторе: в сети у всех копий они должны быть одинаковыми
    void ОбновитьКомпоненты()
    {
#if UNITY_EDITOR
        if (Application.isPlaying) return;
        EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            ВладельческаяСинхронизация синхронизация = GetComponent<ВладельческаяСинхронизация>();
            if (синхронизироватьПоложение && синхронизация == null)
            {
                Undo.AddComponent<ВладельческаяСинхронизация>(gameObject);
                if (GetComponent<Rigidbody>() != null && GetComponent<NetworkRigidbody>() == null) Undo.AddComponent<NetworkRigidbody>(gameObject);
                if (GetComponent<Rigidbody2D>() != null && GetComponent<NetworkRigidbody2D>() == null) Undo.AddComponent<NetworkRigidbody2D>(gameObject);
            }
            else if (!синхронизироватьПоложение && синхронизация != null)
            {
                if (GetComponent<NetworkRigidbody>() != null) Undo.DestroyObjectImmediate(GetComponent<NetworkRigidbody>());
                if (GetComponent<NetworkRigidbody2D>() != null) Undo.DestroyObjectImmediate(GetComponent<NetworkRigidbody2D>());
                Undo.DestroyObjectImmediate(синхронизация);
            }
        };
#endif
    }

    void Reset()
    {
        НайтиУправление();
        ОбновитьКомпоненты();
    }
}

/// <summary>
/// Связь Сетевого объекта с сетью Unity. Добавляется сама, настраивать её не нужно
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("")]
public class СетевойМост : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (TryGetComponent(out СетевойОбъект объект)) объект.ПриПоявленииВСети();
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    internal void ВызватьУВсехRpc(int номер)
    {
        if (TryGetComponent(out СетевойОбъект объект)) объект.ВызватьЗдесь(номер);
    }
}

/// <summary>
/// Синхронизация положения, при которой объект двигает его владелец, а не хозяин игры. Так управление отзывается без задержки
/// </summary>
[AddComponentMenu("")]
public class ВладельческаяСинхронизация : NetworkTransform
{
    protected override bool OnIsServerAuthoritative() => false;
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(СетевойОбъект))]
internal class СетевойОбъектEditor : RunityEditor<СетевойОбъект>
{
    protected override void Отрисовать()
    {
        Текст("Этот объект есть у всех игроков сетевой игры. Создаётся он Сетью (как игрок) или методом Сеть.Заспавнить, а не обычным Спавнером");

        Пробел();

        Поле("Синхронизировать положение", x => x.синхронизироватьПоложение, (title, value) => EditorGUILayout.Toggle(title, value));

        НачатьСобытия();
        ОтобразитьСвойство("Только у владельца", "_толькоУВладельца");
        ЗакончитьСобытия();
        Кнопка("Найти управление", x => x.НайтиУправление());

        Пробел();

        НачатьСобытия();
        ОтобразитьСвойство("События для всех", "_событияДляВсех");
        ОтобразитьСобытие("При появлении", "_приПоявлении");
        ОтобразитьСобытие("При появлении своего", "_приПоявленииСвоего");
        ЗакончитьСобытия();
    }
}
#endif
