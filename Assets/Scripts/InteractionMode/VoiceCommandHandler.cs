using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;

namespace PunctualSolutions.Boxing
{
    public class VoiceCommandHandler : MonoBehaviour
    {
        [SerializeField] private Button voiceCommandButton;  // UserA 的录音按钮
        [SerializeField] private GameObject playIconPrefab;  // UserB 的播放图标

        private bool isRecording = false;
        private AudioClip recordedClip;

        public void Activate()
        {
            Debug.Log("VoiceCommandHandler: Activated voice command mode.");
            if (voiceCommandButton != null && NetworkManager.Singleton.IsHost)
            {
                // 只为 UserA 显示录音按钮
                voiceCommandButton.gameObject.SetActive(true);
                voiceCommandButton.onClick.AddListener(OnVoiceCommandButtonClick);
            }
        }

        private void OnVoiceCommandButtonClick()
        {
            if (!isRecording)
            {
                // 开始录音
                isRecording = true;
                voiceCommandButton.interactable = false;
                voiceCommandButton.GetComponentInChildren<TextMeshProUGUI>().text = "Recording...";
                recordedClip = Microphone.Start(null, false, 10, 44100);
            }
            else
            {
                // 结束录音并发送音频数据
                isRecording = false;
                voiceCommandButton.interactable = true;
                voiceCommandButton.GetComponentInChildren<TextMeshProUGUI>().text = "Press to Speak";
                Microphone.End(null);

                // 将录音的数据传输给 UserB
                SendAudioToUserB(recordedClip);
            }
        }

        private void SendAudioToUserB(AudioClip clip)
        {
            // 获取音频样本
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            // 将样本转换为字节数组
            byte[] byteArray = ConvertFloatArrayToByteArray(samples);

            // 使用 Netcode 发送音频数据
            AudioDataMessage audioMessage = new AudioDataMessage
            {
                SampleRate = clip.frequency,
                Channels = clip.channels,
                AudioData = byteArray
            };

            // 发送音频数据到所有客户端
            SendAudioServerRpc(audioMessage);
        }

        [ServerRpc]
        private void SendAudioServerRpc(AudioDataMessage audioMessage, ServerRpcParams serverRpcParams = default)
        {
            // 将音频数据发送给所有客户端（除了发送者本身）
            ReceiveAudioClientRpc(audioMessage);
        }

        [ClientRpc]
        private void ReceiveAudioClientRpc(AudioDataMessage audioMessage, ClientRpcParams clientRpcParams = default)
        {
            // 只处理 UserB 的音频接收
            if (!NetworkManager.Singleton.IsHost)
            {
                PlayReceivedAudio(audioMessage);
            }
        }

        private void PlayReceivedAudio(AudioDataMessage audioMessage)
        {
            // 将字节数组转换回浮点数数组
            float[] samples = ConvertByteArrayToFloatArray(audioMessage.AudioData);

            // 创建 AudioClip 并设置样本
            AudioClip receivedClip = AudioClip.Create("ReceivedAudio", samples.Length / audioMessage.Channels, audioMessage.Channels, audioMessage.SampleRate, false);
            receivedClip.SetData(samples, 0);

            // 播放音频并显示播放图标
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = receivedClip;

            GameObject playIcon = Instantiate(playIconPrefab);
            playIcon.SetActive(true);
            StartCoroutine(PlayAudioAndHideIcon(audioSource, playIcon));
        }

        private IEnumerator PlayAudioAndHideIcon(AudioSource audioSource, GameObject playIcon)
        {
            audioSource.Play();
            yield return new WaitForSeconds(audioSource.clip.length);
            Destroy(playIcon);
        }

        private byte[] ConvertFloatArrayToByteArray(float[] floatArray)
        {
            byte[] byteArray = new byte[floatArray.Length * sizeof(float)];
            for (int i = 0; i < floatArray.Length; i++)
            {
                byte[] floatBytes = System.BitConverter.GetBytes(floatArray[i]);
                System.Buffer.BlockCopy(floatBytes, 0, byteArray, i * sizeof(float), sizeof(float));
            }
            return byteArray;
        }

        private float[] ConvertByteArrayToFloatArray(byte[] byteArray)
        {
            float[] floatArray = new float[byteArray.Length / sizeof(float)];
            for (int i = 0; i < floatArray.Length; i++)
            {
                floatArray[i] = System.BitConverter.ToSingle(byteArray, i * sizeof(float));
            }
            return floatArray;
        }

        // 音频数据传输消息结构
        public struct AudioDataMessage : INetworkSerializable
        {
            public int SampleRate;
            public int Channels;
            public byte[] AudioData;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref SampleRate);
                serializer.SerializeValue(ref Channels);
                serializer.SerializeValue(ref AudioData);
            }
        }
    }
}
