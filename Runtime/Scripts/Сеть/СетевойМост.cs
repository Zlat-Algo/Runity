using Unity.Netcode;
using UnityEngine;

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
