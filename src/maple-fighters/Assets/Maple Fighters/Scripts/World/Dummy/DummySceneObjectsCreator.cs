﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Common;
using Scripts.Containers;
using Scripts.Gameplay.Actors;
using Scripts.Services;
using Scripts.Utils;
using UnityEngine;

namespace Assets.Scripts
{
    [RequireComponent(typeof(DummyCharacterDetailsProvider))]
    public class DummySceneObjectsCreator : MonoBehaviour
    {
        private IDummySceneObjectsProvider dummySceneObjectsProvider;

        private void Awake()
        {
            dummySceneObjectsProvider =
                GetComponent<IDummySceneObjectsProvider>();
        }

        private void Start()
        {
            StartCoroutine(WaitFrameAndStart());
        }

        private void OnDestroy()
        {
            SavedGameObjects.GetInstance().DestroyAll();
        }

        private IEnumerator WaitFrameAndStart()
        {
            yield return null;

            CreateDummyPlayer();
            CreateDummySceneObjects();
        }

        private void CreateDummyPlayer()
        {
            int id;
            CreateDummyPlayerSceneObject(out id);
        }

        private void CreateDummyPlayerSceneObject(out int id)
        {
            var dummyCharacterDetailsProvider =
                GetComponent<DummyCharacterDetailsProvider>();
            var parameters = 
                dummyCharacterDetailsProvider.GetDummyCharacterParameters();
            var gameScenePeerLogic = ServiceContainer.GameService
                .GetPeerLogic<IGameScenePeerLogicAPI>();
            gameScenePeerLogic.SceneEntered?.Invoke(parameters);

            id = parameters.SceneObject.Id;
        }

        private void CreateDummySceneObjects()
        {
            foreach (var dummyParameters in GetDummySceneObjectsParameters())
            {
                var gameScenePeerLogic = ServiceContainer.GameService
                    .GetPeerLogic<IGameScenePeerLogicAPI>();
                gameScenePeerLogic.SceneObjectAdded?.Invoke(dummyParameters);
            }

            InitializeDummySceneObjects();
        }

        private void InitializeDummySceneObjects()
        {
            foreach (var dummySceneObject in dummySceneObjectsProvider
                .GetSceneObjects())
            {
                var id = dummySceneObject.Id;
                CreateCommonComponentsToSceneObject(id);

                var sceneObject = SceneObjectsContainer.GetInstance()
                    .GetRemoteSceneObject(id);
                if (sceneObject != null)
                {
                    dummySceneObject.AddComponentsAction?.Invoke(sceneObject.GameObject);
                }
            }
        }

        private void CreateCommonComponentsToSceneObject(
            int id,
            params Type[] components)
        {
            var sceneObject = 
                SceneObjectsContainer.GetInstance().GetRemoteSceneObject(id)
                    ?.GameObject;
            if (sceneObject == null)
            {
                Debug.LogWarning($"Could not find a scene object with id {id}");
                return;
            }

            sceneObject.gameObject.name =
                $"{sceneObject.gameObject.name} (Id: {id})";

            foreach (var component in components)
            {
                sceneObject.AddComponent(component);
            }
        }

        private IEnumerable<SceneObjectAddedEventParameters> GetDummySceneObjectsParameters()
        {
            return dummySceneObjectsProvider.GetSceneObjects()
                .Select(
                    dummySceneObject => new SceneObjectParameters(
                        dummySceneObject.Id,
                        dummySceneObject.Name,
                        dummySceneObject.Position.x,
                        dummySceneObject.Position.y,
                        dummySceneObject.SpawnDirection))
                .Select(
                    parameters =>
                        new SceneObjectAddedEventParameters(parameters));
        }
    }
}