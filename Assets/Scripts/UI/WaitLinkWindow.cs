using System.Net;
using System.Net.Sockets;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace PunctualSolutions.Boxing
{
    public class WaitLinkWindow : MonoSingleton<WaitLinkWindow>
    {
        [SerializeField] TextMeshProUGUI _text;

        void Start()
        {
            gameObject.SetActive(false);
        }

        public void Init()
        {
            var hostName  = Dns.GetHostName();
            var hostEntry = Dns.GetHostEntry(hostName);
            var localIp   = "";
            foreach (var ip in hostEntry.AddressList)
            {
                if (ip.AddressFamily != AddressFamily.InterNetwork) continue;
                localIp = ip.ToString();
                break;
            }
            _text.text = localIp;
            NetworkManager.Singleton.StartHost();
            NetworkManager.Singleton.OnClientConnectedCallback += _ =>
            {
                MainManger.Instance.GameMode = GameMode.TriggerPoint;
                MainManger.Instance.HiedLine();
                MainManger.Instance.HiedController();
                MainManger.Instance.InitServer();
                Destroy(gameObject);
            };
        }
    }
}