using System;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;
using System.Net;
using System.Net.Sockets;
using System.Collections;

namespace PunctualSolutions.Boxing
{

    public class StudyMaster : MonoSingleton<StudyMaster>
    {
        [Header("Network Settings")]
        [SerializeField] private UserRole userRole = UserRole.UserA;
        [SerializeField] private string ipAddress = "xx.xxx.xx.xx";
        [SerializeField] private ushort port = 7777;

        [Header("Voice Settings")]
        [SerializeField] private int maxVoiceCount = 5;
        private int _remainingVoiceCount;

        // 添加语音命令激活状态
        private bool _isVoiceCommandActive = false;
        public bool IsVoiceCommandActive
        {
            get => _isVoiceCommandActive;
            set
            {
                _isVoiceCommandActive = value;
                OnVoiceCommandStateChanged?.Invoke(_isVoiceCommandActive);
            }
        }
        //开场词
        public AudioSource audioSource;  // 用于播放语音
        public AudioClip welcomePointing;      // 对应目标指引模式
        public AudioClip welcomeTalking;       // 对应语音交互模式
        public AudioClip welcomePointingTalking;  // 对应组合模式

        public delegate void VoiceCommandStateChangedHandler(bool isActive);
        public event VoiceCommandStateChangedHandler OnVoiceCommandStateChanged;

        public int RemainingVoiceCount
        {
            get => _remainingVoiceCount;
            private set
            {
                _remainingVoiceCount = value;
                OnVoiceCountChanged?.Invoke(_remainingVoiceCount);

                // 当次数用完时，禁用语音功能
                if (_remainingVoiceCount <= 0)
                {
                    IsVoiceCommandActive = false;
                }
            }
        }

        public delegate void VoiceCountChangedHandler(int remainingCount);
        public event VoiceCountChangedHandler OnVoiceCountChanged;

        [Header("Interaction Settings")]
        [SerializeField] private InteractionMode interactionMode;
        [SerializeField] private TargetSettingHandler targetSettingHandler;
        [SerializeField] private PointingAndTalkingHandler pointingAndTalkingHandler;
        [SerializeField] private GameObject voiceCommandHandlerPrefab; // 新增：VoiceCommandHandler 预制体
        private VoiceCommandHandler voiceCommandHandler;

        public enum UserRole { UserA, UserB }
        public enum InteractionMode { TargetSetting, PointingAndTalking, VoiceCommand }
        public UserRole CurrentUserRole => userRole;

        public CameraPathController cameraPathController;  // 摄像机路径控制器
        public Camera openingCamera;                      // 用于开场动画的摄像机
        public Camera mainCamera;                         // XR Rig 中的 Main Camera

        [SerializeField] private GameObject recordButton; // 录制按钮

        void Start()
        {
            // 默认隐藏按钮
            if (recordButton != null)
            {
                recordButton.SetActive(false);
            }
            //订阅摄像机动画完成事件
            if (cameraPathController != null)
            {
                cameraPathController.OnAnimationComplete += HandleCameraAnimationComplete;
            }

            //只针对UserA
            if (userRole == UserRole.UserA)
            {
                PlayInteractionAudio();  // 在启动时播放开场词
                PlayOpeningSequence();  //视角围绕Role旋转
            }
            UpdateRecordButtonVisibility(); // 初始化时更新按钮状态
            InitializeVoiceCount();
#if UNITY_EDITOR
            StartCoroutine(WaitForNetworkManagerInitialization());
#endif
        }

        public void PlayInteractionAudio()
        {
            AudioClip selectedClip = null;

            // 根据交互方式选择语音文件
            switch (interactionMode)
            {
                case InteractionMode.TargetSetting:
                    selectedClip = welcomePointing;
                    break;
                case InteractionMode.VoiceCommand:
                    selectedClip = welcomeTalking;
                    break;
                case InteractionMode.PointingAndTalking:
                    selectedClip = welcomePointingTalking;
                    break;
            }

            // 播放语音
            if (selectedClip != null && audioSource != null)
            {
                audioSource.clip = selectedClip;
                audioSource.Play();
                Debug.Log("播放语音：" + selectedClip.name);
            }
            else
            {
                Debug.LogWarning("AudioSource或AudioClip未设置！");
            }
        }

        public void SetInteractionMode(int mode)
        {
            interactionMode = (InteractionMode)mode;
            Debug.Log("选择的交互方式：" + interactionMode);

            PlayInteractionAudio();  // 播放对应语音

            UpdateRecordButtonVisibility(); // 切换模式时更新按钮状态
        }

        private void InitializeVoiceCount()
        {
            _remainingVoiceCount = maxVoiceCount;
            IsVoiceCommandActive = true; // 初始化时启用语音命令
        }

        public bool UseVoiceCount()
        {
            if (RemainingVoiceCount <= 0)
                return false;

            RemainingVoiceCount--;
            return true;
        }

        private System.Collections.IEnumerator WaitForNetworkManagerInitialization()
        {
            while (NetworkManager.Singleton == null)
            {
                yield return null;
            }
            InitializeNetworkConnection();
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
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            }
            else if (userRole == UserRole.UserB)
            {
                var hostName = Dns.GetHostName();
                var hostEntry = Dns.GetHostEntry(hostName);
                var localIp = "";
                foreach (var ip in hostEntry.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        localIp = ip.ToString();
                        break;
                    }
                }
                Debug.Log("Local IP: " + localIp);

                NetworkManager.Singleton.StartHost();
                NetworkManager.Singleton.OnServerStarted += OnServerStarted;
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            }

            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        private void OnServerStarted()
        {
            Debug.Log("Server started. Spawning VoiceCommandHandler.");
            SpawnVoiceCommandHandler();
        }

        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"Client connected: {clientId}");

            InitializeVoiceCount();


            if (userRole == UserRole.UserA)
            {
                MainManger.Instance.GameMode = GameMode.SetPoint;
                MainManger.Instance.HiedLine();
            }
            else if (userRole == UserRole.UserB)
            {
                MainManger.Instance.GameMode = GameMode.TriggerPoint;
                MainManger.Instance.HiedLine();
                MainManger.Instance.HiedController();
                MainManger.Instance.InitServer();
            }

            ShowInteractionUI();
        }

        private void OnClientDisconnected(ulong clientId)
        {
            Debug.Log($"Client disconnected: {clientId}");
        }

        private void SpawnVoiceCommandHandler()
        {
            if (NetworkManager.Singleton.IsServer && voiceCommandHandlerPrefab != null)
            {
                GameObject voiceHandlerObject = Instantiate(voiceCommandHandlerPrefab);
                NetworkObject networkObject = voiceHandlerObject.GetComponent<NetworkObject>();
                networkObject.Spawn();
                voiceCommandHandler = voiceHandlerObject.GetComponent<VoiceCommandHandler>();
                Debug.Log("VoiceCommandHandler spawned on server.");
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
                    if (voiceCommandHandler == null)
                    {
                        voiceCommandHandler = FindObjectOfType<VoiceCommandHandler>();
                    }
                    voiceCommandHandler?.Activate();
                    break;
                default:
                    Debug.LogWarning("未知的交互方式");
                    break;
            }
        }

        void PlayOpeningSequence()
        {
            if (audioSource != null && cameraPathController != null)
            {
                // 启动摄像机旋转逻辑
                cameraPathController.enabled = true;
                openingCamera.enabled = true;

                // 暂时禁用XR Rig下的主摄像机
                SetMainCameraActive(false);

                // 播放开场语音
                audioSource.Play();
                Debug.Log("开场语音播放中...");

                // 等待语音播放结束后恢复XR视角
                StartCoroutine(WaitForAudioToEnd());
            }
        }
        IEnumerator WaitForAudioToEnd()
        {
            // 等待语音播放完成
            yield return new WaitForSeconds(audioSource.clip.length);

            // 停止摄像机旋转逻辑
            cameraPathController.enabled = false;
            openingCamera.enabled = false;

            // 恢复XR Rig下的主摄像机
            SetMainCameraActive(true);
            // 更新按钮可见性
            UpdateRecordButtonVisibility();

            Debug.Log("开场动画和语音结束，切换到头显视角。");
        }

        void SetMainCameraActive(bool isActive)
        {
            if (mainCamera != null)
            {
                mainCamera.enabled = isActive;
                Debug.Log(isActive ? "启用Main Camera (头显视角)" : "禁用Main Camera (头显视角)");
            }
            else
            {
                Debug.LogWarning("Main Camera未设置！");
            }
        }

        private void UpdateRecordButtonVisibility()
        {
            if (recordButton == null)
                return;
            if (recordButton != null && mainCamera.enabled && !openingCamera.enabled)
            {
                // 当交互模式为 Talking 或 PointingAndTalking 时显示按钮
                recordButton.SetActive(interactionMode == InteractionMode.VoiceCommand || interactionMode == InteractionMode.PointingAndTalking);
            }
            else
            {
                // 主摄像头未激活或 recordButton 未设置时，隐藏按钮
                recordButton?.SetActive(false);
            }
        }

        private void HandleCameraAnimationComplete()
        {
            Debug.Log("Opening camera animation completed. Checking interaction mode...");

            // 更新按钮状态
            UpdateRecordButtonVisibility();

            // 切换到 MainCamera
            if (mainCamera != null && openingCamera != null)
            {
                openingCamera.gameObject.SetActive(false);
                mainCamera.gameObject.SetActive(true);
            }
        }



    }
}