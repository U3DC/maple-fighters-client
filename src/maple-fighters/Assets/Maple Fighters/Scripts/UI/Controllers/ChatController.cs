﻿using System;
using Scripts.Services;
using Scripts.UI.Windows;
using UI.Manager;
using UnityEngine;

namespace Scripts.UI.Controllers
{
    public class ChatController : MonoBehaviour
    {
        public event Action<string> MessageSent;

        private ChatWindow chatWindow;

        private void Awake()
        {
            chatWindow = UIElementsCreator.GetInstance().Create<ChatWindow>();
            chatWindow.MessageAdded += OnMessageAdded;
        }

        private void Start()
        {
            ChatConnectionProvider.GetInstance().Connect();
        }

        private void OnDestroy()
        {
            if (chatWindow != null)
            {
                chatWindow.MessageAdded -= OnMessageAdded;

                Destroy(chatWindow.gameObject);
            }
        }

        public void SetCharacterName(string name)
        {
            if (chatWindow != null)
            {
                chatWindow.CharacterName = name;
            }
        }

        public void OnMessageReceived(string message)
        {
            if (chatWindow != null)
            {
                chatWindow.AddMessage(message);
            }
        }

        private void OnMessageAdded(string message)
        {
            MessageSent?.Invoke(message);
        }
    }
}