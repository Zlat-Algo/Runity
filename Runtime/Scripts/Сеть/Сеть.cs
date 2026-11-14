using System.Collections.Generic;
using System.Linq;
using System.Net;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Сетевая игра по локальной сети, в том числе через Radmin VPN или Hamachi: один игрок создаёт игру, остальные подключаются по адресу или находят её сами
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkManager), typeof(UnityTransport))]
[AddComponentMenu("  Runity/ Сеть/Сеть")]
public class Сеть : RunityComponent
{
    void Awake()
    {
        оригинальныйКомпонент = GetComponent<NetworkManager>();
        if (главная == null) главная = this;
        Настроить();
    }

    void OnValidate()
    {
        оригинальныйКомпонент = GetComponent<NetworkManager>();
        if (оригинал != null && транспорт != null) оригинал.NetworkConfig.NetworkTransport = транспорт;
    }

    protected override void OnEnable() { }
    protected override void OnDisable() { }

    /// <summary>
    /// Оригинальный компонент Unity
    /// </summary>
    public NetworkManager оригинал => (NetworkManager)оригинальныйКомпонент;

    /// <summary>
    /// Как сеть передаёт данные. Это тоже оригинальный компонент Unity
    /// </summary>
    public UnityTransport транспорт => GetComponent<UnityTransport>();

    [SerializeField] Строка _адрес = "127.0.0.1";
    /// <summary>
    /// Адрес компьютера, к которому подключаться. 127.0.0.1 - этот же компьютер. Адрес хозяина видно у него в «Моём адресе»
    /// </summary>
    public Строка адрес { get => _адрес; set => _адрес = value; }

    [SerializeField] Число _порт = 7777;
    /// <summary>
    /// Номер порта игры. У хозяина и у подключающихся он должен совпадать
    /// </summary>
    public Число порт { get => _порт; set => _порт = value; }

    [SerializeField] GameObject _игрок;
    /// <summary>
    /// Образец игрока (префаб). Каждому подключившемуся создаётся своя копия. На образце должен быть Сетевой объект
    /// </summary>
    [Обязательное]
    public GameObject игрок { get => _игрок; set => _игрок = value; }

    [SerializeField] List<Transform> _точкиПоявления = new List<Transform>();
    /// <summary>
    /// Где появляются игроки, по очереди. Если пусто, игроки появляются там, где стоит этот объект
    /// </summary>
    public List<Transform> точкиПоявления => _точкиПоявления;

    [SerializeField] List<GameObject> _сетевыеОбразцы = new List<GameObject>();
    /// <summary>
    /// Образцы (префабы), которые можно создавать во время сетевой игры, например снаряды. На каждом должен быть Сетевой объект
    /// </summary>
    public List<GameObject> сетевыеОбразцы => _сетевыеОбразцы;

    [SerializeField] ВводТекста _полеАдреса;
    /// <summary>
    /// Поле ввода, из которого берётся адрес при подключении. Можно не указывать
    /// </summary>
    public ВводТекста полеАдреса { get => _полеАдреса; set => _полеАдреса = value; }

    [SerializeField] Строка _названиеИгры = "Моя игра";
    /// <summary>
    /// Как игра называется в поиске игр
    /// </summary>
    public Строка названиеИгры { get => _названиеИгры; set => _названиеИгры = value; }

    [SerializeField] Флажок _видноВПоиске = да;
    /// <summary>
    /// Если "да", то созданную игру могут найти другие игроки локальной сети без ввода адреса
    /// </summary>
    public Флажок видноВПоиске { get => _видноВПоиске; set => _видноВПоиске = value; }

    [SerializeField] События _приСозданииИгры;
    /// <summary>
    /// Срабатывает, когда игра создана и ждёт игроков
    /// </summary>
    public События приСозданииИгры => _приСозданииИгры;

    [SerializeField] События _приПодключении;
    /// <summary>
    /// Срабатывает, когда этот игрок подключился к чужой игре
    /// </summary>
    public События приПодключении => _приПодключении;

    [SerializeField] События _приОтключении;
    /// <summary>
    /// Срабатывает, когда этот игрок вышел из игры или связь пропала
    /// </summary>
    public События приОтключении => _приОтключении;

    [SerializeField] События _приВходеИгрока;
    /// <summary>
    /// Срабатывает у хозяина, когда в игру вошёл новый игрок
    /// </summary>
    public События приВходеИгрока => _приВходеИгрока;

    [SerializeField] События _приВыходеИгрока;
    /// <summary>
    /// Срабатывает у хозяина, когда игрок вышел
    /// </summary>
    public События приВыходеИгрока => _приВыходеИгрока;

    [SerializeField] События _приНахожденииИгры;
    /// <summary>
    /// Срабатывает, когда поиск нашёл новую игру
    /// </summary>
    public События приНахожденииИгры => _приНахожденииИгры;

    /// <summary>
    /// Сеть, которая появилась на сцене первой
    /// </summary>
    public static Сеть главная { get; private set; }

    /// <summary>
    /// Даёт "да", если этот игрок создал игру и все к нему подключаются
    /// </summary>
    public Флажок этоХозяин => оригинал != null && оригинал.IsHost;

    /// <summary>
    /// Даёт "да", пока идёт сетевая игра
    /// </summary>
    public Флажок вИгре => оригинал != null && (оригинал.IsServer || оригинал.IsConnectedClient);

    /// <summary>
    /// Сколько игроков в игре. Точно знает только хозяин
    /// </summary>
    public Число количествоИгроков => оригинал != null && оригинал.IsServer ? оригинал.ConnectedClientsIds.Count : вИгре ? (int)игроковПоСлухам : 0;

    /// <summary>
    /// Адреса этого компьютера, которые нужно сообщить друзьям. Адрес Radmin VPN начинается с 26
    /// </summary>
    public static Строка мойАдрес => string.Join(", ", ПоискИгр.СвоиАдреса().Select(а => а.адрес.ToString()).OrderByDescending(а => а.StartsWith("26.")));

    /// <summary>
    /// Что сейчас происходит с сетью, обычными словами. Удобно выводить через Вывод значения
    /// </summary>
    public Строка состояние
    {
        get
        {
            if (оригинал == null || !оригинал.IsListening) return "Не в сети. Ваш адрес: " + мойАдрес;
            if (оригинал.IsHost) return $"Вы хозяин игры. Игроков: {(int)количествоИгроков}. Ваш адрес: {мойАдрес}";
            return оригинал.IsConnectedClient ? $"Подключено к {(string)адрес}" : $"Подключаемся к {(string)адрес}...";
        }
    }

    /// <summary>
    /// Игры, которые нашёл поиск
    /// </summary>
    public List<НайденнаяИгра> найденныеИгры { get; } = new List<НайденнаяИгра>();

    /// <summary>
    /// Даёт "да", пока идёт поиск игр
    /// </summary>
    public Флажок идётПоиск { get; private set; }

    int номерПоявления;
    float доОбъявления;
    int игроковПоСлухам;
    bool подписано;

    void Настроить()
    {
        if (оригинал == null) return;

        оригинал.NetworkConfig.NetworkTransport = транспорт;
        оригинал.NetworkConfig.ConnectionApproval = true;
        оригинал.NetworkConfig.PlayerPrefab = null;

        foreach (GameObject образец in _сетевыеОбразцы.Append(_игрок))
        {
            if (образец == null || Зарегистрирован(образец)) continue;
            if (образец.GetComponent<NetworkObject>() == null)
            {
                Консоль.НапечататьОшибку($"На образце {образец.name} нет Сетевого объекта, поэтому его нельзя создавать в сети", образец);
                continue;
            }
            оригинал.AddNetworkPrefab(образец);
        }

        if (подписано) return;
        подписано = true;
        оригинал.ConnectionApprovalCallback += Одобрить;
        оригинал.OnClientConnectedCallback += ПриПодключенииКлиента;
        оригинал.OnClientDisconnectCallback += ПриОтключенииКлиента;
    }

    // Netcode сам складывает сетевые образцы в общий список, и второй раз добавлять их нельзя
    bool Зарегистрирован(GameObject образец)
    {
        NetworkPrefabs образцы = оригинал.NetworkConfig.Prefabs;
        if (образцы.Contains(образец)) return true;
        foreach (NetworkPrefabsList список in образцы.NetworkPrefabsLists)
        {
            if (список != null && список.PrefabList.Any(з => з.Prefab == образец)) return true;
        }
        return false;
    }

    void OnDestroy()
    {
        if (главная == this) главная = null;
        if (идётПоиск) ПоискИгр.ПерестатьСлушать();
        if (оригинал != null && подписано)
        {
            оригинал.ConnectionApprovalCallback -= Одобрить;
            оригинал.OnClientConnectedCallback -= ПриПодключенииКлиента;
            оригинал.OnClientDisconnectCallback -= ПриОтключенииКлиента;
        }
    }

    // Хозяин пускает каждого и решает, где ему появиться
    void Одобрить(NetworkManager.ConnectionApprovalRequest запрос, NetworkManager.ConnectionApprovalResponse ответ)
    {
        ответ.Approved = true;
        ответ.CreatePlayerObject = _игрок != null;
        if (_игрок != null)
        {
            ответ.PlayerPrefabHash = _игрок.GetComponent<NetworkObject>().PrefabIdHash;
            Transform точка = _точкиПоявления.Count > 0 ? _точкиПоявления[номерПоявления++ % _точкиПоявления.Count] : transform;
            ответ.Position = точка != null ? точка.position : transform.position;
            ответ.Rotation = точка != null ? точка.rotation : Quaternion.identity;
        }
    }

    void ПриПодключенииКлиента(ulong номер)
    {
        if (оригинал.IsServer)
        {
            if (номер != оригинал.LocalClientId) _приВходеИгрока.Вызвать();
        }
        else if (номер == оригинал.LocalClientId)
        {
            _приПодключении.Вызвать();
        }
    }

    void ПриОтключенииКлиента(ulong номер)
    {
        if (оригинал.IsServer && номер != оригинал.LocalClientId) _приВыходеИгрока.Вызвать();
        else if (!оригинал.IsServer) _приОтключении.Вызвать();
    }

    /// <summary>
    /// Создаёт игру на этом компьютере. Этот игрок становится хозяином, а другие могут к нему подключаться
    /// </summary>
    public void СоздатьИгру()
    {
        if (оригинал.IsListening) return;
        Настроить();
        транспорт.SetConnectionData("127.0.0.1", (ushort)(int)порт, "0.0.0.0");
        if (оригинал.StartHost())
        {
            доОбъявления = 0;
            _приСозданииИгры.Вызвать();
        }
        else Консоль.НапечататьОшибку($"Не получилось создать игру. Возможно, порт {(int)порт} занят другой программой", this);
    }

    /// <summary>
    /// Подключается к игре по адресу из поля «Адрес» или из поля ввода адреса
    /// </summary>
    public void Подключиться()
    {
        if (полеАдреса != null && !string.IsNullOrWhiteSpace(полеАдреса.содержимое)) адрес = ((string)полеАдреса.содержимое).Trim();
        ПодключитьсяК(адрес);
    }

    /// <summary>
    /// Подключается к игре на компьютере с указанным адресом
    /// </summary>
    public void ПодключитьсяК(Строка адресХозяина)
    {
        if (оригинал.IsListening) return;
        if (!IPAddress.TryParse(адресХозяина, out _))
        {
            Консоль.НапечататьОшибку($"«{(string)адресХозяина}» не похоже на адрес. Адрес выглядит так: 192.168.1.5 или 26.12.34.56", this);
            return;
        }

        адрес = адресХозяина;
        Настроить();
        транспорт.SetConnectionData(адресХозяина, (ushort)(int)порт);
        if (!оригинал.StartClient()) Консоль.НапечататьОшибку("Не получилось начать подключение", this);
    }

    /// <summary>
    /// Выходит из сетевой игры. Если это хозяин, игра заканчивается для всех
    /// </summary>
    public void Отключиться()
    {
        if (!оригинал.IsListening) return;
        bool былКлиент = !оригинал.IsServer;
        оригинал.Shutdown();
        if (!былКлиент) _приОтключении.Вызвать();
    }

    /// <summary>
    /// Начинает искать игры в локальной сети. Найденные появляются в списке «Найденные игры»
    /// </summary>
    public void НайтиИгры()
    {
        найденныеИгры.Clear();
        идётПоиск = да;
        ПоискИгр.НачатьСлушать();
    }

    /// <summary>
    /// Перестаёт искать игры
    /// </summary>
    public void ПрекратитьПоиск()
    {
        идётПоиск = нет;
        ПоискИгр.ПерестатьСлушать();
    }

    /// <summary>
    /// Подключается к найденной игре по её номеру в списке. Игры считаются с нуля
    /// </summary>
    public void ПодключитьсяКНайденной(Число номер)
    {
        if (номер < 0 || номер >= найденныеИгры.Count) return;
        НайденнаяИгра игра = найденныеИгры[номер];
        порт = игра.порт;
        ПрекратитьПоиск();
        ПодключитьсяК(игра.адрес);
    }

    /// <summary>
    /// Подключается к первой найденной игре
    /// </summary>
    public void ПодключитьсяКПервойНайденной() => ПодключитьсяКНайденной(0);

    /// <summary>
    /// Создаёт копию сетевого образца у всех игроков. Работает только у хозяина
    /// </summary>
    public static GameObject Заспавнить(GameObject образец, Направление3D где)
    {
        NetworkManager сеть = NetworkManager.Singleton;
        if (сеть == null || !сеть.IsServer)
        {
            Консоль.НапечататьПредупреждение("Создавать сетевые объекты может только хозяин игры");
            return null;
        }

        GameObject копия = Instantiate(образец, где, образец.transform.rotation);
        копия.GetComponent<NetworkObject>().Spawn(true);
        return копия;
    }

    void Update()
    {
        if (оригинал != null && оригинал.IsHost && видноВПоиске)
        {
            доОбъявления -= Time.unscaledDeltaTime;
            if (доОбъявления <= 0)
            {
                доОбъявления = 1;
                ПоискИгр.Объявить(названиеИгры, (ushort)(int)порт, оригинал.ConnectedClientsIds.Count);
            }
        }

        while (ПоискИгр.ВзятьПришедшую(out НайденнаяИгра игра))
        {
            if (!идётПоиск) continue;
            НайденнаяИгра была = найденныеИгры.Find(и => и.адрес == игра.адрес && и.порт == игра.порт);
            if (была != null)
            {
                была.игроков = игра.игроков;
                continue;
            }
            найденныеИгры.Add(игра);
            _приНахожденииИгры.Вызвать();
        }
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(Сеть))]
internal class СетьEditor : RunityEditor<Сеть>
{
    public override bool RequiresConstantRepaint() => Application.isPlaying;

    protected override void Отрисовать()
    {
        Текст("Этот объект делает игру сетевой. Один игрок нажимает «Создать игру», остальные - «Подключиться» с его адресом или находят игру поиском. Для игры через интернет установите Radmin VPN или Hamachi и подключайтесь по адресу из них");

        Пробел();

        Поле("Игрок", x => x.игрок, (title, value) => ПолеОбъекта<GameObject>(title, value));
        if (компонент.игрок != null && компонент.игрок.GetComponent<СетевойОбъект>() == null)
        {
            Текст("На образце игрока нет Сетевого объекта", MessageType.Error);
            Кнопка("Добавить Сетевой объект на образец", () =>
            {
                string путь = AssetDatabase.GetAssetPath(компонент.игрок);
                GameObject содержимое = PrefabUtility.LoadPrefabContents(путь);
                содержимое.AddComponent<СетевойОбъект>().НайтиУправление();
                PrefabUtility.SaveAsPrefabAsset(содержимое, путь);
                PrefabUtility.UnloadPrefabContents(содержимое);
            });
        }

        НачатьСобытия();
        ОтобразитьСвойство("Точки появления", "_точкиПоявления");
        ОтобразитьСвойство("Сетевые образцы", "_сетевыеОбразцы");
        ЗакончитьСобытия();

        Пробел();

        Поле("Адрес", x => x.адрес, (title, value) => EditorGUILayout.TextField(title, value));
        Поле("Порт", x => x.порт, (title, value) => Mathf.Clamp(EditorGUILayout.IntField(title, value), 1, 65535));
        Поле("Поле адреса", x => x.полеАдреса, (title, value) => ПолеОбъекта<ВводТекста>(title, value, true));
        Поле("Название игры", x => x.названиеИгры, (title, value) => EditorGUILayout.TextField(title, value));
        Поле("Видно в поиске", x => x.видноВПоиске, (title, value) => EditorGUILayout.Toggle(title, value));

        Пробел();
        Текст(компонент.состояние);

        if (Application.isPlaying)
        {
            НачатьГоризонтальнуюГруппу();
            if (!компонент.вИгре)
            {
                Кнопка("Создать игру", x => x.СоздатьИгру());
                Кнопка("Подключиться", x => x.Подключиться());
                Кнопка("Найти игры", x => x.НайтиИгры());
            }
            else
            {
                Кнопка("Отключиться", x => x.Отключиться());
            }
            ЗакончитьГоризонтальнуюГруппу();

            foreach (НайденнаяИгра игра in компонент.найденныеИгры) Текст("Найдена: " + игра);
        }

        Пробел();

        НачатьСобытия();
        ОтобразитьСобытие("При создании игры", "_приСозданииИгры");
        ОтобразитьСобытие("При подключении", "_приПодключении");
        ОтобразитьСобытие("При отключении", "_приОтключении");
        ОтобразитьСобытие("При входе игрока", "_приВходеИгрока");
        ОтобразитьСобытие("При выходе игрока", "_приВыходеИгрока");
        ОтобразитьСобытие("При нахождении игры", "_приНахожденииИгры");
        ЗакончитьСобытия();
    }
}
#endif
