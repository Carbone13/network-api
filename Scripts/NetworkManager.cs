using Godot;
using LiteNetLib;
using LiteNetLib.Utils;
using Network.Packet;

namespace Network
{
    public class NetworkManager : Node
    {
        public static NetworkManager singleton;
        public static NetPacketProcessor Processor => NetworkManager.singleton.Socket.Processor;

        public Socket Socket { get; private set; }
        
        public override void _Ready ()
        {
            GD.Print("base init");
            Socket = new Socket();
            Socket.Listen();
            
            singleton = this;
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
    }
}