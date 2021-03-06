﻿using Game.Common;
using Scripts.Containers;
using Scripts.Services;
using UnityEngine;

namespace Assets.Scripts.Graphics
{
    public class BubbleMessageListener : MonoBehaviour
    {
        private void Awake()
        {
            var gameScenePeerLogic = ServiceContainer.GameService
                .GetPeerLogic<IGameScenePeerLogicAPI>();
            gameScenePeerLogic.BubbleMessageReceived.AddListener(
                OnBubbleMessageReceived);
        }

        private void OnDestroy()
        {
            var gameScenePeerLogic = ServiceContainer.GameService
                .GetPeerLogic<IGameScenePeerLogicAPI>();
            gameScenePeerLogic.BubbleMessageReceived.RemoveListener(
                OnBubbleMessageReceived);
        }

        private void OnBubbleMessageReceived(
            BubbleMessageEventParameters parameters)
        {
            var id = parameters.RequesterId;
            var sceneObject = SceneObjectsContainer.GetInstance()
                .GetRemoteSceneObject(id);
            if (sceneObject != null)
            {
                var owner = sceneObject.GameObject.transform;
                var message = parameters.Message;
                var time = parameters.Time;
                BubbleMessageCreator.GetInstance().Create(owner, message, time);
            }
        }
    }
}