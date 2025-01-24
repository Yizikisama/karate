using System;
using Cysharp.Threading.Tasks;
using PunctualSolutionsTool.Tool;
using UnityEngine;

namespace PunctualSolutions.Boxing
{
    public class Point : MonoBehaviour
    {
        [SerializeField] MeshRenderer meshRenderer; // 用于设置材质颜色
        [SerializeField] private AudioSource audioSource;  // 用于播放音效
        [SerializeField] private AudioClip hitSound;       // 碰撞时播放的音效

        string Id { get; set; } // 用于标识当前点的ID

        // 初始化方法，设置点的唯一ID
        public void Init(string id) => Id = id;

        // 异步销毁方法
        public async UniTaskVoid Destroy()
        {
            meshRenderer.material.color = Color.green; // 将颜色设置为绿色
            await 3.Delay(); // 等待3秒
            Destroy(gameObject); // 销毁当前对象
        }

        // 碰撞触发事件
        void OnTriggerEnter(Collider other)
        {
            Debug.Log($"Triggered with: {other.gameObject.name}");

            // 检查当前物体是否为 "Player" 且不在 TargetAreaSphere 上
            if (other.CompareTag("Player") && other.gameObject.name == "Role")
            {
                Debug.Log("Player detected! Removing point.");

                // 播放音效
                if (audioSource != null && hitSound != null)
                {
                    audioSource.PlayOneShot(hitSound);
                }

                MainManger.Instance.RemovePoint(Id);
            }
        }

    }
}