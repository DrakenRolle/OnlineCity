﻿using Model;
using OCUnion;
using System;
using System.Net;
using Transfer;
using Util;

namespace OC.Chat
{
    public class ChatProvider
    {
        public ClientData Data { get; set; }
        public Player My { get; set; }
        public TimeSpan ServerTimeDelta { get; set; }

        private readonly Transfer.SessionClient _sessionClient;

        public ChatProvider()
        {
            Loger.Log("Chat Init");
            _sessionClient = new SessionClient();
        }

        public string Login(IPEndPoint addr, string login, string password)
        {
            var msgError = Connect(addr);
            if (msgError != null) return msgError;

            var logMsg = "Login: " + login;
            Loger.Log("Chat " + logMsg);
            My = null;
            var pass = new CryptoProvider().GetHash(password);

            if (!_sessionClient.Login(login, pass))
            {
                logMsg = "Login fail: " + _sessionClient.ErrorMessage;
                Loger.Log("Chat " + logMsg);

                return _sessionClient.ErrorMessage;
            }

            InitConnected();
            return null;
        }

        private string Connect(IPEndPoint addr)
        {
            var stringAdress = addr.Address.ToString();
            var logMsg = "Connecting to server. Addr: " + stringAdress + ":" + addr.Port.ToString();
            Loger.Log("Chat " + logMsg);

            if (!_sessionClient.Connect(stringAdress, addr.Port))
            {
                logMsg = "Connection fail: " + _sessionClient.ErrorMessage;
                return _sessionClient.ErrorMessage;
            }

            Loger.Log("Chat " + logMsg);

            return null;
        }

        public void Disconnected(string msg = "Error Connection.")
        {
            var login = _sessionClient.GetInfo(OCUnion.Transfer.ServerInfoType.Short).My.Login;
            _sessionClient.Disconnect();
        }

        public void UpdateChats()
        {
            ModelUpdateChat dc;
            dc = _sessionClient.UpdateChat(Data.ChatsTime);
            if (dc == null)
            {
                Data.LastServerConnectFail = true;
                if (!Data.ServerConnected)
                {
                    Disconnected();
                }

                return;
            }

            Data.LastServerConnectFail = false;
            Data.LastServerConnect = DateTime.UtcNow;
            var lastMessage = string.Empty;
            Data.ApplyChats(dc, ref lastMessage);
        }

        public ModelStatus SendMessage(string message, long idChanell = 0)
        {
            return _sessionClient.PostingChat(idChanell, message);
        }

        /// <summary>
        /// После успешной регистрации или входа
        /// </summary>
        private void InitConnected()
        {
            var serverInfo = _sessionClient.GetInfo(OCUnion.Transfer.ServerInfoType.Full);
            My = serverInfo.My;
            Data = new ClientData(My.Login, _sessionClient);
            ServerTimeDelta = serverInfo.ServerTime - DateTime.UtcNow;
        }
    }
}