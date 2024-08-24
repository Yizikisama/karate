using System;
using System.Text;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace PunctualSolutions.Boxing
{
    public class LinkWindow : MonoSingleton<LinkWindow>
    {
        [SerializeField] TMP_InputField _linkInputField;

        public void LinkClick()
        {
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
                _linkInputField.text,
                7777
            );
            NetworkManager.Singleton.StartClient();
            NetworkManager.Singleton.OnClientConnectedCallback += _ =>
            {
                MainManger.Instance.GameMode = GameMode.SetPoint;
                MainManger.Instance.HiedLine();
                Destroy(gameObject);
            };
        }

        void Start()
        {
            gameObject.SetActive(false);
        }
    }
}