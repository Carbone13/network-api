using Godot;
using LiteNetLib;
using LiteNetLib.Utils;
using Network.Packet;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Network
{
    public class NetworkManager : Node
    {
        public static NetworkManager singleton;
        public static NetPacketProcessor Processor => NetworkManager.singleton.Socket.Processor;

        public Socket Socket { get; private set; }
        
        public NetPeer Nat, Host;

        public Action<NetPeer, ConnectTowardOrder> OnConnectionOrderTreated;

        public bool lanHost;

        public override void _Ready ()
        {
            Socket = new Socket();
            Socket.Listen();

            singleton = this;
            Socket.Processor.SubscribeReusable<ConnectTowardOrder, NetPeer>(TreatConnectionOrder);
        }
        
        public NetPeer Connect (PeerAddress address, string key)
        {
            return Socket.TryConnect(address.Address, address.Port, key);
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

        public async void TreatConnectionOrder (ConnectTowardOrder order, NetPeer sender)
        {
            // TODO check if sender are clean
            GD.Print("> Received connection order");

            NetPeer peer = await TryConnect(order);
            ConnectionOrderTreated(peer, order);
        }

        public void ConnectionOrderTreated (NetPeer peer, ConnectTowardOrder order)
        {
            OnConnectionOrderTreated?.Invoke(peer, order);
        }       

        public async Task<NetPeer> TryConnect (ConnectTowardOrder order)
        {
            GD.Print(" >> Trying to connect toward " + (order.usePrivate ? order.addresses.Private : order.addresses.Public));
            int tryCount = 0;
    
            PeerAddress targetAddress = new PeerAddress();
            if(order.usePrivate)
                targetAddress = new PeerAddress(order.addresses.Private.Address.ToString(), order.addresses.Private.Port);
            else
                targetAddress = new PeerAddress(order.addresses.Public.Address.ToString(), order.addresses.Public.Port);

            // TODO find something more elegant than this shit
            if(lanHost) 
                await Task.Delay(20);

            NetPeer con = NetworkManager.singleton.Connect(targetAddress, "");
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
                con = NetworkManager.singleton.Connect(targetAddress, "");
                
                tryCount++;
            }
            
            GD.Print("  >>> Connected to peer !");

            return con; 
        }
    }
}