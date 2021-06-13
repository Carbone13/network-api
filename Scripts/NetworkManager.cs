using Godot;
using LiteNetLib;
using LiteNetLib.Utils;
using Network.Packet;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System;

namespace Network
{
    public class NetworkManager : Node
    {
        public static NetworkManager singleton;
        public static NetPacketProcessor Processor => NetworkManager.singleton.Socket.Processor;

        public NetworkPeer Us;
        public Socket Socket { get; private set; }
        
        public NetPeer Nat, Host;

        public Action<NetPeer, NetworkPeer, HolePunchAddress> OnHolePunchSuccess;

        public bool lanHost;

        public override void _Ready ()
        {
            Socket = new Socket();
            Socket.Listen();

            IPEndPoint _private = new IPEndPoint(IPAddress.Parse("127.0.0.1"), Socket.net.LocalPort);
            IPEndPoint _public = new IPEndPoint(IPAddress.Any, Socket.net.LocalPort);

            Us = new NetworkPeer("", new EndpointCouple(_public, _private));

            singleton = this;

            Socket.Processor.SubscribeReusable<HolePunchAddress, NetPeer>(TreatHolePunchAddress);
            Socket.Processor.SubscribeReusable<PublicAddress>(ReceivePublicAddress);
        }

        public void ReceivePublicAddress (PublicAddress address)
        {
            Us.Endpoints = new EndpointCouple(address.Address, Us.Endpoints.Private);
            GD.Print(Us.Endpoints.Private + " " + Us.Endpoints.Public);
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

        public async void TreatHolePunchAddress (HolePunchAddress target, NetPeer sender)
        {
            if(!target.CheckIfLegit()) return;

            GD.Print("> Received connection order");

            NetPeer peer = await HolePunchConnect(target);

            if(peer != null)
                OnHolePunchSuccess?.Invoke(peer, target.Target, target);
        }    

        public async Task<NetPeer> HolePunchConnect (HolePunchAddress target)
        {
            IPEndPoint targetAddress = target.UsePrivate ? target.Target.Endpoints.Private : target.Target.Endpoints.Public;

            GD.Print(" >> Trying to connect toward " +  targetAddress);
            int tryCount = 0;

            NetPeer con = NetworkManager.singleton.TryConnect(targetAddress, "");

            while(con == null)
            {
                await Task.Delay(10);
                con = NetworkManager.singleton.TryConnect(targetAddress, "");
            }

            await Task.Delay(500);
            
            while (con.ConnectionState != ConnectionState.Connected)
            {
                if (tryCount > 3)
                {
                    GD.Print("  >>> Time out !");
                    return null;
                }

                GD.Print(" >> Failed to connect, retrying...");
                await Task.Delay(500);
                
                // Retry
                con.Disconnect();
                con = NetworkManager.singleton.TryConnect(targetAddress, "");
                
                tryCount++;
            }
            
            GD.Print("  >>> Connected to peer !");
            return con; 
        }
    }
}