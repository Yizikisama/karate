using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;

namespace PunctualSolutions.Boxing
{
    public class VoiceCommandHandler : NetworkBehaviour
    {
        [SerializeField] private Button voiceCommandButton;  // 录音按钮
        [SerializeField] private GameObject playIconPrefab;  // 播放图标

        private bool isRecording = false;
        private AudioClip recordedClip;

        // 分片传输参数
        private const int MaxChunkSize = 6000; // 每个数据块的最大大小（字节）
        private Dictionary<string, List<byte[]>> receivedChunks = new(); // 存储接收到的分片

        private void Start()
        {
            Debug.Log("VoiceCommandHandler: Start called.");
            Debug.Log($"Start - IsClient: {NetworkManager.Singleton.IsClient}, IsServer: {NetworkManager.Singleton.IsServer}, IsConnectedClient: {NetworkManager.Singleton.IsConnectedClient}");
            Activate();
            if (StudyMaster.Instance != null)
            {
                Debug.Log($"VoiceCommandHandler: 当前用户角色是: {StudyMaster.Instance.CurrentUserRole}");
                StudyMaster.Instance.OnVoiceCountChanged += OnVoiceCountChanged;
            }
            else
            {
                Debug.LogError("VoiceCommandHandler: StudyMaster.Instance is null.");
            }
        }

        private void OnDestroy()
        {
            if (StudyMaster.Instance != null)
            {
                StudyMaster.Instance.OnVoiceCountChanged -= OnVoiceCountChanged;
            }
        }

        private void OnVoiceCountChanged(int remainingCount)
        {
            Debug.Log($"VoiceCommandHandler: Remaining voice count: {remainingCount}");
            if (remainingCount <= 0)
            {
                // 更新UI显示或禁用相关功能
                UpdateVoiceButtonState(false);
            }
        }

        private void UpdateVoiceButtonState(bool enabled)
        {
            if (voiceCommandButton != null)
            {
                voiceCommandButton.interactable = enabled;
                // 可以更新按钮文本显示剩余次数
                TextMeshProUGUI buttonText = voiceCommandButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = enabled ? $"Press to Speak ({StudyMaster.Instance.RemainingVoiceCount} left)" : "No more attempts";
                }
            }
        }


        public void Activate()
        {
            Debug.Log("VoiceCommandHandler: Activate called.");
            Debug.Log($"Activate - IsClient: {NetworkManager.Singleton.IsClient}, IsServer: {NetworkManager.Singleton.IsServer}, IsConnectedClient: {NetworkManager.Singleton.IsConnectedClient}");

            if (StudyMaster.Instance != null)
            {
                if (StudyMaster.Instance.CurrentUserRole == StudyMaster.UserRole.UserA)
                {
                    if (voiceCommandButton != null)
                    {
                        voiceCommandButton.gameObject.SetActive(true);
                        voiceCommandButton.onClick.RemoveAllListeners();
                        voiceCommandButton.onClick.AddListener(OnVoiceCommandButtonClick);
                        Debug.Log("VoiceCommandHandler: Button listener added and button activated.");
                    }
                    else
                    {
                        Debug.LogError("VoiceCommandHandler: Voice Command Button is null. Check the Inspector bindings.");
                    }
                }
                else
                {
                    if (voiceCommandButton != null)
                    {
                        voiceCommandButton.gameObject.SetActive(false);
                        Debug.Log("VoiceCommandHandler: User is not UserA. Voice Command Button hidden.");
                    }
                }
            }
            else
            {
                Debug.LogError("VoiceCommandHandler: StudyMaster.Instance is null.");
            }
        }

        private void OnVoiceCommandButtonClick()
        {
            Debug.Log("VoiceCommandHandler: Button clicked.");
            if (!isRecording)
            {
                StartRecording();
            }
            else
            {
                StopRecordingAndSend().Forget();
            }
        }

        private void StartRecording()
        {
            isRecording = true;
            voiceCommandButton.GetComponentInChildren<TextMeshProUGUI>().text = "Recording...";
            recordedClip = Microphone.Start(null, false, 10, 44100);
            Debug.Log("VoiceCommandHandler: Recording started.");
        }

        private async UniTaskVoid StopRecordingAndSend()
        {
            Debug.Log($"StopRecordingAndSend - IsClient: {NetworkManager.Singleton.IsClient}, IsServer: {NetworkManager.Singleton.IsServer}, IsConnectedClient: {NetworkManager.Singleton.IsConnectedClient}");

            // 在开始录音时检查并减少次数
            if (!StudyMaster.Instance.UseVoiceCount())
            {
                Debug.Log("VoiceCommandHandler: No more voice attempts remaining.");
                UpdateVoiceButtonState(false);
                return;
            }

            isRecording = false;
            UpdateVoiceButtonState(true); // 更新按钮显示剩余次数
            //voiceCommandButton.GetComponentInChildren<TextMeshProUGUI>().text = "Press to Speak";
            Microphone.End(null);
            Debug.Log("VoiceCommandHandler: Recording stopped.");

            if (recordedClip != null)
            {
                Debug.Log("VoiceCommandHandler: Preparing to send audio to Host.");
                await SendAudioToHostAsync(recordedClip);
            }
            else
            {
                Debug.LogError("VoiceCommandHandler: Recorded clip is null.");
            }
        }

        public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"VoiceCommandHandler spawned. IsServer: {IsServer}, IsClient: {IsClient}, IsOwner: {IsOwner}");
    }

        private async UniTask SendAudioToHostAsync(AudioClip clip)
        {
            Debug.Log($"SendAudioToHostAsync - IsClient: {NetworkManager.Singleton.IsClient}, IsServer: {NetworkManager.Singleton.IsServer}, IsConnectedClient: {NetworkManager.Singleton.IsConnectedClient}");
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);
            byte[] byteArray = ConvertFloatArrayToByteArray(samples);

            // 生成唯一的消息ID
            string messageId = Guid.NewGuid().ToString();

            // 分割音频数据
            int totalChunks = Mathf.CeilToInt((float)byteArray.Length / MaxChunkSize);
            for (int i = 0; i < totalChunks; i++)
            {
                int chunkSize = Mathf.Min(MaxChunkSize, byteArray.Length - i * MaxChunkSize);
                byte[] chunk = new byte[chunkSize];
                Array.Copy(byteArray, i * MaxChunkSize, chunk, 0, chunkSize);

                // 创建分片消息
                AudioDataMessage chunkMessage = new AudioDataMessage
                {
                    MessageId = messageId,
                    SampleRate = clip.frequency,
                    Channels = clip.channels,
                    ChunkIndex = i,
                    TotalChunks = totalChunks,
                    AudioData = chunk
                };

                // 添加额外的检查
        if (IsSpawned && NetworkManager.Singleton.IsConnectedClient)
        {
//            Debug.Log($"Attempting to send chunk {chunkMessage.ChunkIndex} via ServerRpc");
            SendAudioChunkServerRpc(chunkMessage);
        }
        else
        {
            Debug.LogError($"Network not ready for ServerRpc call. IsSpawned: {IsSpawned}, IsConnectedClient: {NetworkManager.Singleton.IsConnectedClient}");
        }
                await UniTask.Yield();
            }

            Debug.Log($"VoiceCommandHandler: All {totalChunks} chunks sent.");
        }

        [ServerRpc(RequireOwnership = false)]
        public void SendAudioChunkServerRpc(AudioDataMessage audioMessage, ServerRpcParams serverRpcParams = default)
        {
            Debug.Log($"SendAudioChunkServerRpc - IsClient: {NetworkManager.Singleton.IsClient}, IsServer: {NetworkManager.Singleton.IsServer}, IsConnectedClient: {NetworkManager.Singleton.IsConnectedClient}");
            Debug.Log($"VoiceCommandHandler: ServerRpc received on Host. MessageId: {audioMessage.MessageId}, ChunkIndex: {audioMessage.ChunkIndex}.");

            // 转发给所有客户端（包括Host自己）
            ReceiveAudioClientRpc(audioMessage);
        }

        [ClientRpc]
        private void ReceiveAudioClientRpc(AudioDataMessage audioMessage)
        {
            Debug.Log($"ReceiveAudioClientRpc - IsClient: {NetworkManager.Singleton.IsClient}, IsServer: {NetworkManager.Singleton.IsServer}, IsConnectedClient: {NetworkManager.Singleton.IsConnectedClient}");
            if (StudyMaster.Instance != null && StudyMaster.Instance.CurrentUserRole == StudyMaster.UserRole.UserB)
            {
                Debug.Log($"VoiceCommandHandler: ClientRpc received on UserB. MessageId: {audioMessage.MessageId}, ChunkIndex: {audioMessage.ChunkIndex}.");
                AssembleAudio(audioMessage);
            }
        }

        private void AssembleAudio(AudioDataMessage audioMessage)
        {
            if (!receivedChunks.ContainsKey(audioMessage.MessageId))
            {
                receivedChunks[audioMessage.MessageId] = new List<byte[]>();
            }

            receivedChunks[audioMessage.MessageId].Add(audioMessage.AudioData);
            Debug.Log($"VoiceCommandHandler: Received chunk {audioMessage.ChunkIndex + 1}/{audioMessage.TotalChunks} for MessageId: {audioMessage.MessageId}.");

            // 检查是否所有分片都已接收
            if (receivedChunks[audioMessage.MessageId].Count == audioMessage.TotalChunks)
            {
                Debug.Log($"VoiceCommandHandler: All chunks received for MessageId: {audioMessage.MessageId}. Assembling audio.");
                // 组装完整的音频数据
                List<byte> completeAudioData = new List<byte>();
                for (int i = 0; i < audioMessage.TotalChunks; i++)
                {
                    completeAudioData.AddRange(receivedChunks[audioMessage.MessageId][i]);
                }

                // 转换为 float 数组
                float[] samples = ConvertByteArrayToFloatArray(completeAudioData.ToArray());
                AudioClip receivedClip = AudioClip.Create("ReceivedAudio", samples.Length / audioMessage.Channels, audioMessage.Channels, audioMessage.SampleRate, false);
                receivedClip.SetData(samples, 0);

                // 播放音频
                AudioSource audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.clip = receivedClip;
                audioSource.Play();

                // 显示播放图标
                GameObject playIcon = Instantiate(playIconPrefab);
                playIcon.SetActive(true);
                StartCoroutine(PlayAudioAndHideIcon(audioSource, playIcon));

                // 清理已接收的分片
                receivedChunks.Remove(audioMessage.MessageId);

                Debug.Log("VoiceCommandHandler: Audio playback started on UserB.");
            }
        }

        private IEnumerator PlayAudioAndHideIcon(AudioSource audioSource, GameObject playIcon)
        {
            yield return new WaitForSeconds(audioSource.clip.length);
            Destroy(playIcon);
        }

        private byte[] ConvertFloatArrayToByteArray(float[] floatArray)
        {
            byte[] byteArray = new byte[floatArray.Length * sizeof(float)];
            Buffer.BlockCopy(floatArray, 0, byteArray, 0, byteArray.Length);
            return byteArray;
        }

        private float[] ConvertByteArrayToFloatArray(byte[] byteArray)
        {
            float[] floatArray = new float[byteArray.Length / sizeof(float)];
            Buffer.BlockCopy(byteArray, 0, floatArray, 0, byteArray.Length);
            return floatArray;
        }

        public struct AudioDataMessage : INetworkSerializable
        {
            public string MessageId;
            public int SampleRate;
            public int Channels;
            public int ChunkIndex;
            public int TotalChunks;
            public byte[] AudioData;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref MessageId);
                serializer.SerializeValue(ref SampleRate);
                serializer.SerializeValue(ref Channels);
                serializer.SerializeValue(ref ChunkIndex);
                serializer.SerializeValue(ref TotalChunks);
                serializer.SerializeValue(ref AudioData);
            }
        }
    }
}