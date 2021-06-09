using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;

// TODO rework packets for v2
namespace Network.Packet
{
    public struct EndpointCouple
    {
        public IPEndPoint Public, Private;

        public EndpointCouple (IPEndPoint _public, IPEndPoint _private)
        {
            Public = _public;
            Private = _private;
        }
    }

    /// <summary>
    /// Represent the address of a peer
    /// </summary>
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
    public class Lobby
    {
        public IPEndPoint HostPublicAddress { get; set; }
        public string LobbyName { get; set; }
        public string HostName { get; set; }
        public int PlayerCount { get; set; }
    }

    
    // Empty packet, notify that you want to get the lobbies list
    public class RequestLobbyList {}

    // Ask to join a specific lobby
    public class JoinLobby
    {
        public IPEndPoint HostPublicAddress { get; set; }
        public string LobbyName { get; set; }
        public string HostName { get; set; }
        public int PlayerCount { get; set; }
    }

    // Sent by Lobby-er to client, notify them that they need to connect toward the specified end point
    public class ConnectTowardOrder
    {
        public IPEndPoint target { get; set; }
        public IPEndPoint privateTarget { get; set; }
        public bool usePrivate { get; set;}    
    }

    // Send an int linking to an error
    public class NATError
    {
        public const int LOBBY_HOST_LOST = 1;
        
        public int error { get; set; }
    }
    
    public class LobbyConnectConfirmationFromHost {}

    public class LobbyMessage
    {
        public string header { get; set; }
        public string message { get; set; }
    }
}