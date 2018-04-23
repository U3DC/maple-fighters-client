﻿using Scripts.UI.Core;
using UnityEngine;

namespace Scripts.UI
{
    public class MapSceneTitleCreator : MonoBehaviour
    {
        [SerializeField] private string titleText;

        private void Start()
        {
            var mapSceneTitleText = UserInterfaceContainer.Instance.Add<MapSceneTitleText>();
            mapSceneTitleText.Text = titleText;

            Destroy(gameObject);
        }
    }
}