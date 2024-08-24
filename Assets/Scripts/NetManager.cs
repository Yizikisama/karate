using Unity.Netcode;
using UnityEngine;

public class NetManager : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        MainManger.Instance.NetManager = this;
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void CreatePointRpc(string id, Vector3 position)
    {
        MainManger.Instance.AddPoint(id, position).Forget();
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void DestroyPointRpc(string id)
    {
        MainManger.Instance.RemovePoint(id);
    }
}