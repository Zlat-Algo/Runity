using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// Игра, найденная в локальной сети
/// </summary>
[Serializable]
public class НайденнаяИгра
{
    /// <summary>
    /// Название игры, которое задал хозяин
    /// </summary>
    public string название;

    /// <summary>
    /// Адрес компьютера хозяина
    /// </summary>
    public string адрес;

    /// <summary>
    /// Порт игры
    /// </summary>
    public ushort порт;

    /// <summary>
    /// Сколько игроков уже в игре
    /// </summary>
    public int игроков;

    public override string ToString() => $"{название} ({адрес}, игроков: {игроков})";
}

// Хозяин раз в секунду кричит в локальную сеть о своей игре, а ищущие слушают.
// Radmin VPN и Hamachi выглядят для Windows как обычная локальная сеть, поэтому через них это тоже работает
internal static class ПоискИгр
{
    internal const int портПоиска = 47777;
    const string метка = "RUNITY1";

    static UdpClient слушатель;
    static readonly ConcurrentQueue<НайденнаяИгра> пришедшие = new ConcurrentQueue<НайденнаяИгра>();

    internal static void Объявить(string название, ushort порт, int игроков)
    {
        byte[] сообщение = Encoding.UTF8.GetBytes($"{метка}|{порт}|{игроков}|{название.Replace("|", "/")}");
        using (UdpClient отправитель = new UdpClient())
        {
            отправитель.EnableBroadcast = true;
            foreach (IPAddress адрес in АдресаРассылки())
            {
                try { отправитель.Send(сообщение, сообщение.Length, new IPEndPoint(адрес, портПоиска)); }
                catch (SocketException) { }
            }
        }
    }

    // У каждой сети свой адрес рассылки: 192.168.1.255 для домашней, 26.255.255.255 для Radmin и так далее
    static IEnumerable<IPAddress> АдресаРассылки()
    {
        HashSet<IPAddress> адреса = new HashSet<IPAddress> { IPAddress.Broadcast, IPAddress.Loopback };
        foreach ((IPAddress адрес, IPAddress маска) in СвоиАдреса())
        {
            byte[] а = адрес.GetAddressBytes(), м = маска.GetAddressBytes();
            for (int i = 0; i < 4; i++) а[i] = (byte)(а[i] | ~м[i]);
            адреса.Add(new IPAddress(а));
        }
        return адреса;
    }

    internal static IEnumerable<(IPAddress адрес, IPAddress маска)> СвоиАдреса()
    {
        NetworkInterface[] сети;
        try { сети = NetworkInterface.GetAllNetworkInterfaces(); }
        catch (Exception) { yield break; }

        foreach (NetworkInterface сеть in сети)
        {
            if (сеть.OperationalStatus != OperationalStatus.Up || сеть.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
            foreach (UnicastIPAddressInformation адрес in сеть.GetIPProperties().UnicastAddresses)
            {
                if (адрес.Address.AddressFamily == AddressFamily.InterNetwork && адрес.IPv4Mask != null)
                    yield return (адрес.Address, адрес.IPv4Mask);
            }
        }
    }

    internal static void НачатьСлушать()
    {
        if (слушатель != null) return;
        try
        {
            слушатель = new UdpClient();
            слушатель.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            слушатель.Client.Bind(new IPEndPoint(IPAddress.Any, портПоиска));
            слушатель.BeginReceive(Пришло, слушатель);
        }
        catch (SocketException ошибка)
        {
            UnityEngine.Debug.LogWarning("Не получилось начать поиск игр: " + ошибка.Message);
            слушатель = null;
        }
    }

    internal static void ПерестатьСлушать()
    {
        слушатель?.Close();
        слушатель = null;
    }

    static void Пришло(IAsyncResult результат)
    {
        UdpClient клиент = (UdpClient)результат.AsyncState;
        IPEndPoint откуда = new IPEndPoint(IPAddress.Any, 0);
        byte[] данные;
        try { данные = клиент.EndReceive(результат, ref откуда); }
        catch (Exception) { return; }

        string[] части = Encoding.UTF8.GetString(данные).Split(new[] { '|' }, 4);
        if (части.Length == 4 && части[0] == метка && ushort.TryParse(части[1], out ushort порт) && int.TryParse(части[2], out int игроков))
        {
            пришедшие.Enqueue(new НайденнаяИгра { адрес = откуда.Address.ToString(), порт = порт, игроков = игроков, название = части[3] });
        }

        try { клиент.BeginReceive(Пришло, клиент); }
        catch (Exception) { }
    }

    internal static bool ВзятьПришедшую(out НайденнаяИгра игра) => пришедшие.TryDequeue(out игра);
}
