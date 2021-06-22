using Thread = System.Threading.Thread;
using System.Collections.Generic;
using System.Net.Sockets;
using LiteNetLib.Utils;
using LiteNetLib;
using System.Net;
using System;
using System.Threading;
using Godot;
using Network.Packet;

namespace Network
{
    /// <summary>
    /// A socket allow you the receive packets on a port (specified or not)
    /// You can connect to multiple other peers and send them data
    /// </summary>
    public class Socket
    {
        public NetPacketProcessor Processor;
        public NetManager net;
        public EventBasedNetListener Events;

        public readonly List<NetworkPeer> peers = new List<NetworkPeer>();

        private bool _listening;
        private bool _autoAccepting;
        private Thread _netThread;
        
        public Socket (bool autoAccept = true)
        {
            Processor = new NetPacketProcessor();
            Events = new EventBasedNetListener();
            
            // Register nested types
            Processor.RegisterNestedType<NetworkPeer>();
            Processor.RegisterNestedType<EndpointCouple>();
            Processor.RegisterNestedType<Lobby>();

            _autoAccepting = autoAccept;
        }

        public NetPeer GetPeer (NetworkPeer peer)
        {
            foreach (NetPeer _peer in net.ConnectedPeerList)
            {
                if (peer.Endpoints.CorrespondTo(_peer.EndPoint))
                    return _peer;
            }

            return null;
        }

        public void Listen (int port = -1)
        {
            if (_listening) return;
            
            net = new NetManager(Events);
            
            Events.NetworkReceiveEvent += (_peer, _reader, _method) =>
            {
                Processor.ReadAllPackets(_reader, _peer);
            };
            if (_autoAccepting)
                Events.ConnectionRequestEvent += _request => _request.Accept();
            

            if(port == -1)
                net.Start();
            else
                net.Start(port);
            
            _listening = true;

            StartNetworkThread();
        }

        public NetPeer TryConnect (IPEndPoint target, string key)
        {
            if (!_listening)
            {
                Listen();
            }

            NetPeer peer = net.Connect(target.Address.ToString(), target.Port, key);
            return peer;
        }

        public void Disconnect (NetPeer target)
        {
            target.Disconnect();
        }


        public void BroadcastToPeers (NetDataWriter data, DeliveryMethod method)
        {
            foreach (NetPeer peer in net.ConnectedPeerList)
            {
                if(peer.EndPoint.Address.ToString() != "90.76.187.136")
                     peer.Send(data, method);
            }
        }
        

        // Terminate everything !
        public void Close ()
        {
            net.DisconnectAll();
            net.Stop();
            _netThread.Abort();
        }

        #region Threading

        private void StartNetworkThread ()
        {
            _netThread = new Thread(NetworkThread);
            _netThread.Priority = ThreadPriority.AboveNormal;
            
            _netThread.Start();
        }

        public void NetworkThread ()
        {
            while (_listening)
            {
                net.PollEvents();
                Thread.Sleep(10);
            }
        }

        #endregion
    }
}