using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace PunctualSolutions.Boxing
{
    public class CameraPathController : MonoBehaviour
    {
        public Transform target;       // 旋转的中心对象（Role 模型）
        public float rotationSpeed = 30f;  // 旋转速度（度/秒）
        public float radius = 5f;      // 摄像机与目标的距离
        public float height = 2f;      // 摄像机的高度
        private float angle = 0f;      // 当前旋转角度
        public Action OnAnimationComplete;
        private bool isRotating = true;

        void Update()
        {
            if (target == null) return;

            // 更新摄像机的位置
            angle += rotationSpeed * Time.deltaTime; // 根据时间计算角度
            float radians = angle * Mathf.Deg2Rad;

            // 计算摄像机的新位置
            float x = target.position.x + radius * Mathf.Cos(radians);
            float z = target.position.z + radius * Mathf.Sin(radians);
            float y = target.position.y + height;

            transform.position = new Vector3(x, y, z);

            // 始终让摄像机看向目标
            transform.LookAt(target);
        }

        public void CompleteAnimation()
        {
            Debug.Log("Opening camera animation completed.");
            OnAnimationComplete?.Invoke();  // 通知订阅者
        }

    }
}