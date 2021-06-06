using Godot;
using LiteNetLib;
using Network.Packet;

namespace Network
{
    public class NetworkManager : Node
    {
        public static NetworkManager singleton;

        public Socket Socket { get; private set; }
        
        public override void _Ready ()
        {
            singleton = this;

            Socket = new Socket();
            Socket.Listen();
        }
        
        public NetPeer Connect (PeerAddress address, string key)
        {
            return Socket.Connect(address.Address, address.Port, key);
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