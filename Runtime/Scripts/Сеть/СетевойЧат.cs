using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Сообщение в чате
/// </summary>
[System.Serializable]
public class СообщениеЧата
{
    /// <summary>
    /// Кто написал. Пусто - сообщение от игры (кто-то вошёл или вышел)
    /// </summary>
    public string автор;
    /// <summary>
    /// Что написано
    /// </summary>
    public string текст;
    /// <summary>
    /// Когда пришло, по часам этого компьютера
    /// </summary>
    public string время;
}

/// <summary>
/// Чат сетевой игры и список игроков с именами. Работает вместе с компонентом Сеть: сообщения получают все подключённые игроки.
/// Сетевой объект для чата не нужен
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("  Runity/ Сеть/Сетевой Чат")]
public class СетевойЧат : RunityComponent
{
    protected override void OnEnable() { }
    protected override void OnDisable() { }

    const string канал = "Runity.Чат";
    const char разделитель = '\u001f';

    /// <summary>
    /// Имя этого игрока в чате и в списке игроков. Запоминается между запусками игры
    /// </summary>
    public static Строка имяИгрока
    {
        get
        {
            // Без имени у каждого игрока своё случайное, чтобы в чате их можно было различить
            if (!PlayerPrefs.HasKey("Runity.ИмяИгрока")) PlayerPrefs.SetString("Runity.ИмяИгрока", "Игрок " + Random.Range(100, 1000));
            return PlayerPrefs.GetString("Runity.ИмяИгрока");
        }
        set
        {
            PlayerPrefs.SetString("Runity.ИмяИгрока", string.IsNullOrWhiteSpace(value) ? "Игрок" : value.ToString().Trim());
            if (главный != null) главный.ОтправитьИмя();
        }
    }

    /// <summary>
    /// Чат на сцене
    /// </summary>
    public static СетевойЧат главный { get; private set; }

    [SerializeField] ВводТекста _полеВвода;
    /// <summary>
    /// Поле, в котором игрок пишет сообщение. Сообщение отправляется, когда игрок нажимает Enter
    /// </summary>
    public ВводТекста полеВвода { get => _полеВвода; set => _полеВвода = value; }

    [SerializeField] Текст _текстЧата;
    /// <summary>
    /// Текст, в котором показываются последние сообщения
    /// </summary>
    public Текст текстЧата { get => _текстЧата; set { _текстЧата = value; Показать(); } }

    [SerializeField] Текст _текстИгроков;
    /// <summary>
    /// Текст, в котором показывается список игроков
    /// </summary>
    public Текст текстИгроков { get => _текстИгроков; set { _текстИгроков = value; Показать(); } }

    [SerializeField, Минимум(1)] int _сколькоПоказывать = 8;
    /// <summary>
    /// Сколько последних сообщений видно в тексте чата
    /// </summary>
    public Число сколькоПоказывать { get => _сколькоПоказывать; set { _сколькоПоказывать = Mathf.Max(1, value); Показать(); } }

    [SerializeField] bool _показыватьВремя;
    /// <summary>
    /// Если "да", перед сообщением пишется время
    /// </summary>
    public Флажок показыватьВремя { get => _показыватьВремя; set { _показыватьВремя = value; Показать(); } }

    [SerializeField] bool _сообщатьОВходе = true;
    /// <summary>
    /// Если "да", в чате пишется, когда игрок вошёл или вышел
    /// </summary>
    public Флажок сообщатьОВходе { get => _сообщатьОВходе; set => _сообщатьОВходе = value; }

    [SerializeField, Минимум(1)] int _длинаСообщения = 200;
    /// <summary>
    /// Самое длинное сообщение в буквах. Остальное обрезается
    /// </summary>
    public Число длинаСообщения { get => _длинаСообщения; set => _длинаСообщения = Mathf.Max(1, value); }

    [SerializeField] События _приСообщении;
    /// <summary>
    /// Срабатывает, когда в чат пришло сообщение
    /// </summary>
    public События приСообщении => _приСообщении;

    [SerializeField] События _приСменеИгроков;
    /// <summary>
    /// Срабатывает, когда игрок вошёл, вышел или сменил имя
    /// </summary>
    public События приСменеИгроков => _приСменеИгроков;

    /// <summary>
    /// Все сообщения с начала игры
    /// </summary>
    public List<СообщениеЧата> сообщения { get; } = new List<СообщениеЧата>();

    /// <summary>
    /// Последнее сообщение
    /// </summary>
    public СообщениеЧата последнее => сообщения.Count > 0 ? сообщения[сообщения.Count - 1] : null;

    /// <summary>
    /// Имена всех игроков в игре
    /// </summary>
    public List<string> игроки { get; } = new List<string>();

    // Хозяин знает имена всех: номер подключения - имя
    readonly Dictionary<ulong, string> имена = new Dictionary<ulong, string>();
    NetworkManager сеть;

    void Awake()
    {
        if (главный == null) главный = this;
    }

    void Start()
    {
        сеть = Сеть.главная != null ? Сеть.главная.оригинал : NetworkManager.Singleton;
        if (сеть == null)
        {
            Консоль.НапечататьПредупреждение("Для чата на сцене нужен компонент Сеть", this);
            return;
        }
        сеть.OnServerStarted += ПриСтартеХозяина;
        сеть.OnClientStarted += Подписаться;
        сеть.OnClientConnectedCallback += ПриПодключении;
        сеть.OnClientDisconnectCallback += ПриОтключении;
        if (сеть.IsListening) Подписаться();

        if (_полеВвода != null && _полеВвода.оригинал != null)
            _полеВвода.оригинал.onEndEdit.AddListener(_ => ОтправитьИзПоля());
        Показать();
    }

    void OnDestroy()
    {
        if (главный == this) главный = null;
        if (сеть == null) return;
        сеть.OnServerStarted -= ПриСтартеХозяина;
        сеть.OnClientStarted -= Подписаться;
        сеть.OnClientConnectedCallback -= ПриПодключении;
        сеть.OnClientDisconnectCallback -= ПриОтключении;
        if (сеть.CustomMessagingManager != null) сеть.CustomMessagingManager.UnregisterNamedMessageHandler(канал);
    }

    void Подписаться()
    {
        if (сеть.CustomMessagingManager == null) return;
        сеть.CustomMessagingManager.UnregisterNamedMessageHandler(канал);
        сеть.CustomMessagingManager.RegisterNamedMessageHandler(канал, Получить);
    }

    void ПриСтартеХозяина()
    {
        Подписаться();
        имена.Clear();
        if (сеть.IsHost) имена[сеть.LocalClientId] = имяИгрока;
        РазослатьИгроков();
    }

    void ПриПодключении(ulong номер)
    {
        if (!сеть.IsServer && номер == сеть.LocalClientId) ОтправитьИмя();
    }

    void ПриОтключении(ulong номер)
    {
        if (!сеть.IsServer)
        {
            if (номер == сеть.LocalClientId) { игроки.Clear(); Показать(); }
            return;
        }
        if (!имена.TryGetValue(номер, out string имя)) return;
        имена.Remove(номер);
        if (_сообщатьОВходе) Разослать("", имя + " выходит из игры");
        РазослатьИгроков();
    }

    /// <summary>
    /// Отправляет сообщение всем игрокам
    /// </summary>
    public void Отправить(Строка текст)
    {
        string сообщение = текст.ToString().Trim();
        if (сообщение.Length == 0) return;
        if (сообщение.Length > _длинаСообщения) сообщение = сообщение.Substring(0, _длинаСообщения);

        if (сеть == null || !сеть.IsListening)
        {
            // Без сети сообщение видно только себе: удобно проверять чат без второго компьютера
            Добавить(имяИгрока, сообщение);
            return;
        }
        if (сеть.IsServer) Разослать(имяИгрока, сообщение);
        else Послать(NetworkManager.ServerClientId, "С" + сообщение);
    }

    /// <summary>
    /// Отправляет то, что написано в поле ввода, и очищает поле
    /// </summary>
    public void ОтправитьИзПоля()
    {
        if (_полеВвода == null) return;
        string текст = _полеВвода.содержимое;
        if (string.IsNullOrWhiteSpace(текст)) return;
        _полеВвода.содержимое = "";
        Отправить(текст);
    }

    /// <summary>
    /// Стирает все сообщения у этого игрока
    /// </summary>
    public void Очистить()
    {
        сообщения.Clear();
        Показать();
    }

    void ОтправитьИмя()
    {
        if (сеть == null || !сеть.IsListening) return;
        if (сеть.IsServer)
        {
            имена[сеть.LocalClientId] = имяИгрока;
            РазослатьИгроков();
        }
        else Послать(NetworkManager.ServerClientId, "И" + имяИгрока);
    }

    // Вид сообщения - первая буква: С - сообщение хозяину, И - имя хозяину, Ч - строка чата всем, Л - список игроков всем
    void Получить(ulong отКого, FastBufferReader данные)
    {
        данные.ReadValueSafe(out string строка);
        if (string.IsNullOrEmpty(строка)) return;
        string содержимое = строка.Substring(1);
        switch (строка[0])
        {
            case 'С':
                if (сеть.IsServer)
                {
                    if (содержимое.Length > _длинаСообщения) содержимое = содержимое.Substring(0, _длинаСообщения);
                    Разослать(имена.TryGetValue(отКого, out string имя) ? имя : "Игрок " + отКого, содержимое);
                }
                break;
            case 'И':
                if (сеть.IsServer)
                {
                    bool новый = !имена.ContainsKey(отКого);
                    имена[отКого] = содержимое;
                    if (новый && _сообщатьОВходе) Разослать("", содержимое + " входит в игру");
                    РазослатьИгроков();
                }
                break;
            case 'Ч':
                int граница = содержимое.IndexOf(разделитель);
                if (граница >= 0) Добавить(содержимое.Substring(0, граница), содержимое.Substring(граница + 1));
                break;
            case 'Л':
                ОбновитьИгроков(содержимое.Split(разделитель));
                break;
        }
    }

    void Разослать(string автор, string текст)
    {
        Всем("Ч" + автор + разделитель + текст);
        Добавить(автор, текст);
    }

    void РазослатьИгроков()
    {
        string[] список = имена.OrderBy(п => п.Key).Select(п => п.Value).ToArray();
        Всем("Л" + string.Join(разделитель.ToString(), список));
        ОбновитьИгроков(список);
    }

    void Всем(string строка)
    {
        if (сеть == null || !сеть.IsServer) return;
        foreach (ulong номер in сеть.ConnectedClientsIds)
        {
            if (номер != сеть.LocalClientId) Послать(номер, строка);
        }
    }

    void Послать(ulong кому, string строка)
    {
        if (сеть.CustomMessagingManager == null) return;
        using FastBufferWriter запись = new FastBufferWriter(64, Allocator.Temp, 64 * 1024);
        запись.WriteValueSafe(строка);
        сеть.CustomMessagingManager.SendNamedMessage(канал, кому, запись, NetworkDelivery.ReliableFragmentedSequenced);
    }

    void ОбновитьИгроков(IEnumerable<string> список)
    {
        игроки.Clear();
        игроки.AddRange(список.Where(и => !string.IsNullOrEmpty(и)));
        _приСменеИгроков.Вызвать();
        Показать();
    }

    void Добавить(string автор, string текст)
    {
        сообщения.Add(new СообщениеЧата { автор = автор, текст = текст, время = System.DateTime.Now.ToString("HH:mm") });
        _приСообщении.Вызвать();
        Показать();
    }

    void Показать()
    {
        if (_текстЧата != null)
        {
            _текстЧата.содержимое = string.Join("\n", сообщения.Skip(Mathf.Max(0, сообщения.Count - _сколькоПоказывать)).Select(с =>
                (_показыватьВремя ? $"[{с.время}] " : "") + (string.IsNullOrEmpty(с.автор) ? $"<i>{с.текст}</i>" : $"<b>{с.автор}:</b> {с.текст}")));
        }
        if (_текстИгроков != null)
        {
            _текстИгроков.содержимое = игроки.Count > 0 ? $"Игроки ({игроки.Count}):\n" + string.Join("\n", игроки) : "";
        }
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(СетевойЧат))]
internal class СетевойЧатEditor : RunityEditor<СетевойЧат>
{
    protected override void Отрисовать()
    {
        Текст("Чат сетевой игры. Нужен компонент Сеть на сцене. Имя игрока задаётся через СетевойЧат.имяИгрока - например, из поля ввода");

        Синхрополе("Поле ввода", x => x.полеВвода,
            (title, value) => (ВводТекста)EditorGUILayout.ObjectField(title, value, typeof(ВводТекста), true));
        Синхрополе("Текст чата", x => x.текстЧата,
            (title, value) => (Текст)EditorGUILayout.ObjectField(title, value, typeof(Текст), true));
        Синхрополе("Текст игроков", x => x.текстИгроков,
            (title, value) => (Текст)EditorGUILayout.ObjectField(title, value, typeof(Текст), true));
        Синхрополе("Сколько показывать", x => x.сколькоПоказывать,
            (title, value) => EditorGUILayout.IntField(title, value));
        Синхрополе("Сообщать о входе", x => x.сообщатьОВходе,
            (title, value) => EditorGUILayout.Toggle(title, value));

        if (Application.isPlaying)
        {
            Пробел();
            Текст("Имя игрока: " + СетевойЧат.имяИгрока + "\nСообщений: " + компонент.сообщения.Count + "\nИгроки: " + string.Join(", ", компонент.игроки));
        }
    }
}
#endif
