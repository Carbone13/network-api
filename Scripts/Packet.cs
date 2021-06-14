using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Collections.Generic;

// NOTE: Lobby-Er refers to https://github.com/Carbone13/lobby-er
namespace Network.Packet
{
    public interface IPacket
    {
        NetworkPeer Sender { get; set; }

        bool CheckIfLegit ();
        void Send(NetPeer target, DeliveryMethod method);
    }
    public struct Lobby : INetSerializable
    {
        public NetworkPeer Host { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public int MaxAuthorizedPlayer { get; set; }
        public List<NetworkPeer> ConnectedPeers { get; set; }

        public Lobby (NetworkPeer _host, string _name, string _password, int _maxPlayer, List<NetworkPeer> _connected)
        {
            Host = _host;
            Name = _name;
            Password = _password;
            MaxAuthorizedPlayer = _maxPlayer;
            ConnectedPeers = _connected;
        }

        public void Serialize (NetDataWriter writer)
        {
            writer.Put(Host);
            writer.Put(Name);
            writer.Put(Password);
            writer.Put(MaxAuthorizedPlayer);

            writer.Put(ConnectedPeers.Count);
            foreach(NetworkPeer peer in ConnectedPeers)
            {
                writer.Put(peer);
            }
        }

        public void Deserialize (NetDataReader reader)
        {
            Host = reader.Get<NetworkPeer>();
            Name = reader.GetString();
            Password = reader.GetString();
            MaxAuthorizedPlayer = reader.GetInt();

            int peerLength = reader.GetInt();
            ConnectedPeers = new List<NetworkPeer>();
            for(int i = 0; i < peerLength; i++)
            {
                ConnectedPeers.Add(reader.Get<NetworkPeer>());
            }
        }
    }
    
    public class PublicAddress : IPacket
    {
         public NetworkPeer Sender { get; set; }

         public IPEndPoint Address { get; set; }

         public PublicAddress (NetworkPeer _sender, IPEndPoint _address)
         {
             Sender = _sender;
             Address = _address;
         }

         public bool CheckIfLegit () 
            => Sender.HighAuthority;

        public void Send (NetPeer target, DeliveryMethod method)
            => target.Send(NetworkManager.Processor.Write(this), method);

        public PublicAddress () {}
    }

    // Ask Lobby-Er for the list of available lobbies
    public class QueryLobbyList : IPacket
    {
        public NetworkPeer Sender { get; set; }

        public QueryLobbyList (NetworkPeer sender)
        {
            Sender = sender;
        }

        public bool CheckIfLegit () 
            => !Sender.HighAuthority;

        public void Send (NetPeer target, DeliveryMethod method)
            => target.Send(NetworkManager.Processor.Write(this), method);

        // Empty constructor needed to serialize with PacketProcessor
        public QueryLobbyList () {}
    }

    // Answer from a QueryLobbyList by Lobby-Er containing available lobbies
    public class LobbyListAnswer : IPacket
    {
        public NetworkPeer Sender { get; set; }

        public Lobby[] AvailableLobbies { get; set; }

        public LobbyListAnswer (NetworkPeer _sender, Lobby[] _lobbies)
        {
            Sender = _sender;
            AvailableLobbies = _lobbies;
        }

        public bool CheckIfLegit () 
            => Sender.HighAuthority;

        public void Send (NetPeer target, DeliveryMethod method)
            => target.Send(NetworkManager.Processor.Write(this), method);

        // Empty constructor needed to serialize with PacketProcessor
        public LobbyListAnswer () {}
    }

    // Sent by an Host to register/update its own lobby state
    public class RegisterAndUpdateLobbyState : IPacket
    {
        public NetworkPeer Sender { get; set; }

        public Lobby Lobby { get; set; }

        public RegisterAndUpdateLobbyState (NetworkPeer _sender, Lobby _lobby)
        {
            Sender = _sender;
            Lobby = _lobby;
        }

        public bool CheckIfLegit () 
            => Sender.HighAuthority;

        public void Send (NetPeer target, DeliveryMethod method)
            => target.Send(NetworkManager.Processor.Write(this), method);
        

        // Empty constructor needed to serialize with PacketProcessor
        public RegisterAndUpdateLobbyState () {}
    }

    public class AskForOtherClients : IPacket
    {
        public NetworkPeer Sender { get; set; }

        public AskForOtherClients (NetworkPeer _sender)
        {
            Sender = _sender;
        }
        public bool CheckIfLegit () 
            => true;

        public void Send (NetPeer target, DeliveryMethod method) 
            => target.Send(NetworkManager.Processor.Write(this), method);

        // Empty constructor needed to serialize with PacketProcessor
        public AskForOtherClients () {}
    }

    public class LobbyChatMessage : IPacket
    {
        public NetworkPeer Sender { get; set; }

        public string Message { get; set; }

        public LobbyChatMessage (NetworkPeer _sender, string _message)
        {
            Sender = _sender;
            Message = _message;
        }

        public bool CheckIfLegit () 
            => true;

        public void Send (NetPeer target, DeliveryMethod method) 
            => target.Send(NetworkManager.Processor.Write(this), method);

        // Empty constructor needed to serialize with PacketProcessor
        public LobbyChatMessage () {}
    }
}