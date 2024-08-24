using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PunctualSolutions.Boxing;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class MainManger : MonoSingleton<MainManger>
{
    public           GameMode                  GameMode { get; set; }
    [SerializeField] Point                     pointPrefab;
    readonly         Dictionary<string, Point> Points = new();
    public           NetManager                NetManager { get; set; }
    [SerializeField] Transform                 SpawnPoint;
    [SerializeField] GameObject                Role;
    public           void                      InitServer() => Role.SetActive(true);
    [SerializeField] XRInteractorLineVisual    _leftLine;
    [SerializeField] XRInteractorLineVisual    _rightLine;


    public void HiedLine()
    {
        _leftLine.enabled  = false;
        _rightLine.enabled = false;
    }

    public void HiedController()
    {
        GameObject.Find("XR Origin (XR Rig)/Camera Offset/Left Controller/[Left Controller] Model Parent/XR Controller Left(Clone)").SetActive(false);
        GameObject.Find("XR Origin (XR Rig)/Camera Offset/Right Controller/[Right Controller] Model Parent/XR Controller Right(Clone)").SetActive(false);
    }

    public async UniTaskVoid AddPoint(string playerId, Vector3 position)
    {
        if (Points.ContainsKey(playerId)) return;
        var async = InstantiateAsync(pointPrefab, position, Quaternion.identity);
        await async;
        async.Result[0].Init(playerId);
        Points.Add(playerId, async.Result[0]);
        if (GameMode == GameMode.SetPoint)
            NetManager.CreatePointRpc(playerId, position);
    }

    public void RemovePoint(string playerId)
    {
        if (!Points.TryGetValue(playerId, out var point)) return;
        point.Destroy().Forget();
        Points.Remove(playerId);
        if (GameMode == GameMode.TriggerPoint)
            NetManager.DestroyPointRpc(playerId);
    }

    [SerializeField] bool Random;

    public void AddPoint(InputAction.CallbackContext context)
    {
        if (!context.started ||
            GameMode != GameMode.SetPoint)
            return;
        var position = Random ? SpawnPoint.position + UnityEngine.Random.insideUnitSphere * 1 : SpawnPoint.position;
        AddPoint(Guid.NewGuid().ToString(), position).Forget();
    }
}