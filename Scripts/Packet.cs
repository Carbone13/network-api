using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;

// TODO rework packets for v2
namespace Network.Packet
{
    // Public & Private IP inside one struct
    public struct EndpointCouple : INetSerializable
    {
        public IPEndPoint Public { get; set;}
        public IPEndPoint Private { get; set;}

        public EndpointCouple (IPEndPoint _public, IPEndPoint _private)
        {
            Public = _public;
            Private = _private;
        }

        public void Serialize (NetDataWriter writer)
        {
            writer.Put(Public);
            writer.Put(Private);
        }

        public void Deserialize (NetDataReader reader)
        {
            Public = reader.GetNetEndPoint();
            Private = reader.GetNetEndPoint();
        }
    }

    /// Represent the address of a peer
    public struct PeerAddress : INetSerializable
    {
        public string Address { get; set; }
        public int Port { get; set; }

        public PeerAddress (string addr, int port)
        {
            Address = addr;
            Port = port;
        }
        
        public void Serialize (NetDataWriter writer)
        {
            writer.Put(Address);
            writer.Put(Port);
        }

        public void Deserialize (NetDataReader reader)
        {
            Address = reader.GetString();
            Port = reader.GetInt();
        }
    }

    // Represent a joinable Lobby
    // If you sent this to the host it will register it as your lobby
    // Further resent will update the state (you can only have 1 lobby per host)
    public class Lobby
    {
        public IPEndPoint HostPublicAddress { get; set; }
        public string LobbyName { get; set; }
        public string HostName { get; set; }
        public int PlayerCount { get; set; }
        public int MaxPlayer { get; set;}
    }

    // Ask to join a specific lobby
    public class JoinLobby
    {
        public IPEndPoint HostPublicAddress { get; set; }
        public string LobbyName { get; set; }
        public string HostName { get; set; }
        public int PlayerCount { get; set; }
        public int MaxPlayer { get; set;}
    }

    // Order packet, sent by Lobby-Er or by an Host only (as this require strong authority)
    // These packets are managed by the network manager itself
    // TODO that
    public class ConnectTowardOrder
    {
        public EndpointCouple addresses { get; set; }
        public bool usePrivate { get; set; }

        public IPEndPoint EndPoint() => usePrivate ? addresses.Private : addresses.Public;

        public ConnectTowardOrder(EndpointCouple endpoints)
        {
            addresses = endpoints;
        }

        public ConnectTowardOrder(IPEndPoint _private, IPEndPoint _public)
        {
            addresses = new EndpointCouple(_private, _public);
        }

        public ConnectTowardOrder(IPEndPoint _private, IPEndPoint _public, bool _usePrivate)
        {
            addresses = new EndpointCouple(_private, _public);
            usePrivate = _usePrivate;
        }

        public ConnectTowardOrder() { }
    }

    // Packet Error, see const
    public class Error
    {
        public const int UNKNOWN = 0;
        public const int LOBBY_HOST_LOST = 1;
        public const int LOBBY_REJECTEd = 2;
        public const int LOBBY_KICKED = 3;
        public const int LOBBY_FULL = 4;
        
        public int error { get; set; }

        public Error () {}

        public Error(int err)
        {
            error = err;
        }
    }
    
    // Empty packet, sent by a lobby host to someone who just connected successfully !
    public class LobbyConnectConfirmationFromHost {}

    // Empty packet, notify that you want to get the lobbies list
    public class RequestLobbyList {}

    // A message in the lobby's chat
    public class LobbyMessage
    {
        // Mostly contains the sender's username
        public string header { get; set; }
        // The message itself
        public string message { get; set; }
    }
}