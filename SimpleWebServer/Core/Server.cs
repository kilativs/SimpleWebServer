using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SimpleWebServer.Core
{
    public class Server : IDisposable
    {
        private static List<Client> ClientList { get; set; } = new();

        private readonly TcpListener _listener;
        private Thread _listenerThread;
        private const int Duration = 1;
        private bool _isNotSuspended = true;

        public Server(): this(80)
        {}

        private Server(ushort port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            StartServer();
        }

        private void StartServer()
        {
            _isNotSuspended = true;

            if (_listenerThread is not null) return;

            _listener.Start();
            _listenerThread = new Thread(ServerThread)
            {
                IsBackground = true,
                Name = "Server Main Thread",
                Priority = ThreadPriority.AboveNormal
            };
            _listenerThread.Start();
        }

        public void SuspendServer() => _isNotSuspended = false;

        private void ServerThread()
        {
            while (_isNotSuspended)
            {
                ThreadPool.QueueUserWorkItem(ClientThread, _listener.AcceptTcpClient());
                Thread.Sleep(Duration);
            }
        }

        private static void ClientThread([NotNull]object stateInfo)
        {
            var client = (TcpClient)stateInfo;
            var serverClient = new Client(client);
            ClientList.Add(serverClient);
        }

        ~Server()
        {
            _listener?.Stop();
        }

        public void Dispose()
        {
            _isNotSuspended = false;
            _listener.Stop();
        }
    }
}
