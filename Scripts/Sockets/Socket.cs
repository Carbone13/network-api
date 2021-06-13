using Thread = System.Threading.Thread;
using System.Collections.Generic;
using System.Net.Sockets;
using LiteNetLib.Utils;
using LiteNetLib;
using System.Net;
using System;
using Godot;
using Network.Packet;

namespace Network
{
    /// <summary>
    /// A socket allow you the receive packets on a port (specified or not)
    /// You can connect to multiple other peers and send them data
    /// </summary>
    public class Socket : INetEventListener
    {

        public NetPacketProcessor Processor;
        public NetManager net;

        public readonly List<NetworkPeer> peers = new List<NetworkPeer>();

        private bool _listening;
        private Thread _netThread;
        
        // These are actions linking everything from INetEvenListener, you can connect external function to them.
        public Action<NetPeer> PeerConnection;
        public Action<NetPeer, DisconnectInfo> PeerDisconnection;
        public Action<IPEndPoint, SocketError> NetworkError;
        public Action<NetPeer, int> LatencyUpdate;
        public Action<ConnectionRequest> ConnectionRequest;
        public Action<NetPeer, NetPacketReader, DeliveryMethod> PacketReception;
        public Action<IPEndPoint, NetPacketReader, UnconnectedMessageType> PacketReceptionUnconnected;


        public Socket ()
        {
            Processor = new NetPacketProcessor();

            // Register nested types

            Processor.RegisterNestedType<NetworkPeer>();
            Processor.RegisterNestedType<EndpointCouple>();
            Processor.RegisterNestedType<Lobby>();
        }
        
        public void Listen (int port = -1)
        {
            if (_listening) return;
            
            net = new NetManager(this);
            
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

        #region Net Events
        public void OnPeerConnected (NetPeer peer)
        {
            PeerConnection?.Invoke(peer);
        }

        public void OnPeerDisconnected (NetPeer peer, DisconnectInfo disconnectInfo)
        {
            PeerDisconnection?.Invoke(peer, disconnectInfo);
        }

        public void OnNetworkError (IPEndPoint endPoint, SocketError socketError)
        {
            NetworkError?.Invoke(endPoint, socketError);
        }

        public void OnNetworkReceive (NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            PacketReception?.Invoke(peer, reader, deliveryMethod);
            Processor.ReadAllPackets(reader, peer);
        }

        public void OnNetworkReceiveUnconnected (IPEndPoint remoteEndPoint, NetPacketReader reader,
            UnconnectedMessageType messageType)
        {
            PacketReceptionUnconnected?.Invoke(remoteEndPoint, reader, messageType);
        }

        public void OnNetworkLatencyUpdate (NetPeer peer, int latency)
        {
            LatencyUpdate?.Invoke(peer, latency);
        }

        public List<IPEndPoint> awaiting = new List<IPEndPoint>();

        public void OnConnectionRequest (ConnectionRequest request)
        {
            // TODO deactivate that
            request.Accept();
            ConnectionRequest?.Invoke(request);
        }

        #endregion
    }
}