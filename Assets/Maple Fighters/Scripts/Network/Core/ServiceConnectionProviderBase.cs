﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Authorization.Client.Common;
using CommonCommunicationInterfaces;
using CommonTools.Coroutines;
using CommonTools.Log;
using CommunicationHelper;
using Scripts.ScriptableObjects;
using Scripts.Utils;
using WaitForSeconds = CommonTools.Coroutines.WaitForSeconds;

namespace Scripts.Services
{
    public abstract class ServiceConnectionProviderBase<T> : DontDestroyOnLoad<T>
        where T : ServiceConnectionProviderBase<T>
    {
        protected ExternalCoroutinesExecutor CoroutinesExecutor
        {
            get
            {
                if (coroutinesExecutor == null)
                {
                    coroutinesExecutor = new ExternalCoroutinesExecutor();
                }

                return coroutinesExecutor;
            }
        }
        private ExternalCoroutinesExecutor coroutinesExecutor = new ExternalCoroutinesExecutor();

        private IServiceBase serviceBase => GetServiceBase();
        private ICoroutine disconnectAutomatically;

        protected bool IsDestroying;

        private void Update()
        {
            CoroutinesExecutor?.Update();
        }

        private void OnDestroy()
        {
            IsDestroying = true;

            CoroutinesExecutor?.Dispose();

            Dispose();
        }

        protected async Task Connect(IYield yield, ServerConnectionInformation serverConnectionInformation, bool disconnectAutomatically = false, bool authorize = true)
        {
            if (serviceBase == null || CoroutinesExecutor == null)
            {
                LogUtils.Log("A service base is not initialized.");
                return;
            }

            OnPreConnection();

            var connectionStatus = ConnectionStatus.Failed;

            try
            {
                connectionStatus = await serviceBase.ServiceConnectionHandler.Connect(yield, CoroutinesExecutor, serverConnectionInformation);
            }
            catch (ServerConnectionFailed exception)
            {
                LogUtils.Log(exception.Message, LogMessageType.Error);
            }

            if (connectionStatus == ConnectionStatus.Failed)
            {
                OnConnectionFailed();
                return;
            }

            if (authorize)
            {
                serviceBase.SetPeerLogic<AuthorizationPeerLogic, AuthorizationOperations, EmptyEventCode>(new AuthorizationPeerLogic());
            }
            else
            {
                SetPeerLogicAfterAuthorization();
            }

            SubscribeToDisconnectionNotifier();
            OnConnectionEstablished();

            if (disconnectAutomatically)
            {
                const int TIME_TO_DISCONNECT = 60;
                DisconnectAutomatically(TIME_TO_DISCONNECT);
            }
        }

        protected abstract void OnPreConnection();
        protected abstract void OnConnectionFailed();
        protected abstract void OnConnectionEstablished();

        protected virtual void OnDisconnected(DisconnectReason reason, string details)
        {
            UnsubscribeFromDisconnectionNotifier();
            Dispose();
        }

        private void SubscribeToDisconnectionNotifier()
        {
            serviceBase.ServiceConnectionHandler.ConnectionNotifier.Disconnected += OnDisconnected;
        }

        private void UnsubscribeFromDisconnectionNotifier()
        {
            serviceBase.ServiceConnectionHandler.ConnectionNotifier.Disconnected -= OnDisconnected;
        }

        protected async Task Authorize(IYield yield)
        {
            OnPreAuthorization();

            var authorizationStatus = AuthorizationStatus.Failed;

            try
            {
                var parameters = new AuthorizeRequestParameters(AccessTokenProvider.AccessToken);
                var responseParameters = await Authorize(yield, parameters);
                authorizationStatus = responseParameters.Status;
            }
            catch (Exception)
            {
                // Left blank intentionally
            }

            if (authorizationStatus == AuthorizationStatus.Failed)
            {
                return;
            }

            SetPeerLogicAfterAuthorization();
            OnAuthorized();
        }

        protected abstract Task<AuthorizeResponseParameters> Authorize(IYield yield, AuthorizeRequestParameters parameters);

        protected abstract void OnPreAuthorization();
        protected abstract void OnAuthorized();

        protected abstract void SetPeerLogicAfterAuthorization();
        protected abstract IServiceBase GetServiceBase();

        protected void DisconnectAutomatically(int timer)
        {
            if (disconnectAutomatically == null)
            {
                disconnectAutomatically = CoroutinesExecutor.StartCoroutine(DisconnectAutomaticallyTimer(timer));
            }
        }

        protected IEnumerator<IYieldInstruction> DisconnectAutomaticallyTimer(int timer)
        {
            yield return new WaitForSeconds(timer);
            Dispose();
        }

        public void Dispose()
        {
            disconnectAutomatically?.Dispose();
            disconnectAutomatically = null;

            serviceBase.ServiceConnectionHandler?.Dispose();
        }

        public bool IsConnected()
        {
            return serviceBase != null && serviceBase.ServiceConnectionHandler.IsConnected();
        }

        protected ServerConnectionInformation GetServerConnectionInformation(ServerType serverType)
        {
            var connectionInformation = ServicesConfiguration.GetInstance().GetConnectionInformation(serverType);
            var peerConnectionInformation = NetworkConfiguration.GetInstance().GetPeerConnectionInformation(connectionInformation);
            return new ServerConnectionInformation(serverType, peerConnectionInformation);
        }

        protected ServerConnectionInformation GetServerConnectionInformation(ServerType serverType, PeerConnectionInformation peerConnectionInformation)
        {
            return new ServerConnectionInformation(serverType, peerConnectionInformation);
        }
    }
}