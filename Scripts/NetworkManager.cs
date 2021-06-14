using Godot;
using LiteNetLib;
using LiteNetLib.Utils;
using Network.Packet;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System;
using Network.HolePunching;

namespace Network
{
    public class NetworkManager : Node
    {
        public static NetworkManager singleton;
        public static NetPacketProcessor Processor => singleton.Socket.Processor;

        public NetworkPeer Us;
        public Socket Socket { get; private set; }
        public NetPeer LobbyEr { get; private set; }
        public HolePuncher HolePuncher { get; private set; }

        public override void _Ready ()
        {
            singleton = this;
            
            Socket = new Socket();
            Socket.Listen();
            
            IPEndPoint _private = new IPEndPoint(GetLocalIP(), Socket.net.LocalPort);
            IPEndPoint _public = new IPEndPoint(IPAddress.Any, Socket.net.LocalPort);
            Socket.Processor.SubscribeReusable<PublicAddress>(ReceivePublicAddress);

            Us = new NetworkPeer("", new EndpointCouple(_public, _private));
            LobbyEr = TryConnect(new IPEndPoint(IPAddress.Parse("90.76.187.136"), 3456), "");
            HolePuncher = new HolePuncher();
        }

        // Contains your public address, sent by the Lobby-Er
        public void ReceivePublicAddress (PublicAddress address)
        {
            UpdateUs(Us.Nickname, new EndpointCouple(address.Address, Us.Endpoints.Private), Us.HighAuthority);
        }

        public NetPeer TryConnect (IPEndPoint target, string key)
        {
            return Socket.TryConnect(target, key);
        }
        
        public override void _Notification(int what)
        {
            // If we want the quit the game
            if (what == MainLoop.NotificationWmQuitRequest)
            {
                // Do a clean disconnect before
                Socket.Close();
                GetTree().Quit(); 
            }
        }

        public void UpdateUs (string _nickname, EndpointCouple _endpoints, bool _authority)
        {
            Us.Nickname = _nickname;
            Us.Endpoints = _endpoints;
            Us.HighAuthority = _authority;
        }

        private IPAddress GetLocalIP ()
        {
            using (System.Net.Sockets.Socket socket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint.Address;
            }
        }
    }
}