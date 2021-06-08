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
        #region Field
        
        public NetPacketProcessor Processor;
        private NetManager net; // This things allow us to connect & receive packets
        public readonly List<NetPeer> peers = new List<NetPeer>(); // List of peers we are connected to.

        private bool _listening; // If we are currently listening one port (= can we receive packets ?)
        
        private Thread _netThread;
        
        // These are actions linking everything from INetEvenListener, you can connect external function to them.
        public Action<NetPeer> PeerConnection;
        public Action<NetPeer, DisconnectInfo> PeerDisconnection;
        public Action<IPEndPoint, SocketError> NetworkError;
        public Action<NetPeer, int> LatencyUpdate;
        public Action<ConnectionRequest> ConnectionRequest;
        public Action<NetPeer, NetPacketReader, DeliveryMethod> PacketReception;
        public Action<IPEndPoint, NetPacketReader, UnconnectedMessageType> PacketReceptionUnconnected;

        #endregion

        public Socket ()
        {
            Processor = new NetPacketProcessor();
            Processor.RegisterNestedType<PeerAddress>();
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

        public NetPeer TryConnect (string address, int port, string key)
        {
            if (!_listening)
            {
                Listen();
            }

            GD.Print("> connecting to " + address + ":" + port);
            NetPeer peer = net.Connect(address, port, key);
            return peer;
        }

        public void Disconnect (NetPeer target)
        {
            target.Disconnect();
        }


        public void BroadcastToPeers (NetDataWriter data, DeliveryMethod method)
        {
            foreach (NetPeer peer in peers)
            {
                if(peer.EndPoint.Address.ToString() != "90.76.187.136")
                     peer.Send(data, method);
            }
        }
        

        // Terminate everything !
        public void Close ()
        {
            foreach (NetPeer peer in peers)
            {
                peer.Disconnect();
            }
            
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
                Thread.Sleep(5);
            }
        }

        #endregion

        #region Net Events
        public void OnPeerConnected (NetPeer peer)
        {
            peers.Add(peer);
            GD.Print("> New peer connected (confirmed) " + peer.EndPoint);
            PeerConnection?.Invoke(peer);
        }

        public void OnPeerDisconnected (NetPeer peer, DisconnectInfo disconnectInfo)
        {
            peers.Remove(peer);
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

        public void OnConnectionRequest (ConnectionRequest request)
        {
            // TODO deactivate that
            request.Accept();
            ConnectionRequest?.Invoke(request);
        }

        #endregion
    }
}