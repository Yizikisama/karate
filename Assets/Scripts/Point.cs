using System;
using Cysharp.Threading.Tasks;
using PunctualSolutionsTool.Tool;
using UnityEngine;

namespace PunctualSolutions.Boxing
{
    public class Point : MonoBehaviour
    {
        [SerializeField] MeshRenderer meshRenderer;
        string                        Id              { get; set; }
        public void                   Init(string id) => Id = id;

        public async UniTaskVoid Destroy()
        {
            meshRenderer.material.color = Color.green;
            await 3.Delay();
            Destroy(gameObject);
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player")) MainManger.Instance.RemovePoint(Id);
        }
    }
}