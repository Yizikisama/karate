using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    public float moveSpeed = 5f;

    private void Update()
    {
        if (IsLocalPlayer)
        {
            HandleMovement();
        }
    }

    private void HandleMovement()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");
        Vector3 movement = new Vector3(moveHorizontal, 0, moveVertical) * moveSpeed * Time.deltaTime;

        // 移动玩家（本地）
        transform.Translate(movement);

        // 将位置和旋转发送到服务器
        SendPositionAndRotationServerRpc(transform.position, transform.rotation);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendPositionAndRotationServerRpc(Vector3 position, Quaternion rotation)
    {
        // 服务器接收并广播位置和旋转到所有客户端
        BroadcastPositionAndRotationClientRpc(position, rotation);
    }

    [ClientRpc]
    private void BroadcastPositionAndRotationClientRpc(Vector3 position, Quaternion rotation)
    {
        // 仅更新非本地玩家的模型
        if (!IsLocalPlayer)
        {
            transform.position = position;
            transform.rotation = rotation;
        }
    }
}
