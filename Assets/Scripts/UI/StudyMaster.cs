using System;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;
using System.Net;
using System.Net.Sockets;

namespace PunctualSolutions.Boxing
{
    [ExecuteInEditMode]
    public class StudyMaster : MonoBehaviour
    {
        [Header("Network Settings")]
        [SerializeField] private UserRole userRole = UserRole.UserA;  // 在 Inspector 中选择 UserA 或 UserB
        [SerializeField] private string ipAddress = "xx.xxx.xx.xx";  // 输入连接的目标 IP 地址
        [SerializeField] private ushort port = 7777;  // 端口改为 ushort 类型

        [Header("Interaction Settings")]
        [SerializeField] private InteractionMode interactionMode;
        [SerializeField] private TargetSettingHandler targetSettingHandler;
        [SerializeField] private PointingAndTalkingHandler pointingAndTalkingHandler;
        [SerializeField] private VoiceCommandHandler voiceCommandHandler;

        public enum UserRole
        {
            UserA,
            UserB
        }

        public enum InteractionMode
        {
            TargetSetting,
            PointingAndTalking,
            VoiceCommand
        }

        void Start()
        {
#if UNITY_EDITOR
            StartCoroutine(WaitForNetworkManagerInitialization());
#endif
        }

        private System.Collections.IEnumerator WaitForNetworkManagerInitialization()
        {
            // 等待直到 NetworkManager.Singleton 已经初始化
            while (NetworkManager.Singleton == null)
            {
                yield return null;
            }
            InitializeNetworkConnection();
            ShowInteractionUI();
        }

        private void InitializeNetworkConnection()
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("NetworkManager.Singleton is null. Make sure NetworkManager is added to the scene.");
                return;
            }

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("UnityTransport component is missing from NetworkManager. Please add UnityTransport to NetworkManager.");
                return;
            }

            if (userRole == UserRole.UserA)
            {
                transport.SetConnectionData(ipAddress, port);
                NetworkManager.Singleton.StartClient();
                NetworkManager.Singleton.OnClientConnectedCallback += clientId =>
                {
                    Debug.Log("UserA connected to the server successfully.");
                    MainManger.Instance.GameMode = GameMode.SetPoint;
                    MainManger.Instance.HiedLine();
                };
                NetworkManager.Singleton.OnClientDisconnectCallback += clientId =>
                {
                    Debug.Log("UserA disconnected from the server.");
                };
            }
            else if (userRole == UserRole.UserB)
            {
                var hostName = Dns.GetHostName();
                var hostEntry = Dns.GetHostEntry(hostName);
                var localIp = "";
                foreach (var ip in hostEntry.AddressList)
                {
                    if (ip.AddressFamily != AddressFamily.InterNetwork) continue;
                    localIp = ip.ToString();
                    break;
                }
                Debug.Log("Local IP: " + localIp);

                NetworkManager.Singleton.StartHost();
                NetworkManager.Singleton.OnClientConnectedCallback += clientId =>
                {
                    Debug.Log("A client has connected to UserB's server.");
                    MainManger.Instance.GameMode = GameMode.TriggerPoint;
                    MainManger.Instance.HiedLine();
                    MainManger.Instance.HiedController();
                    MainManger.Instance.InitServer();
                };
                NetworkManager.Singleton.OnClientDisconnectCallback += clientId =>
                {
                    Debug.Log("A client has disconnected from UserB's server.");
                };
            }
        }

        private void ShowInteractionUI()
        {
            switch (interactionMode)
            {
                case InteractionMode.TargetSetting:
                    targetSettingHandler?.Activate();
                    break;
                case InteractionMode.PointingAndTalking:
                    pointingAndTalkingHandler?.Activate();
                    break;
                case InteractionMode.VoiceCommand:
                    voiceCommandHandler?.Activate();
                    break;
                default:
                    Debug.LogWarning("未知的交互方式");
                    break;
            }
        }
    }
}
