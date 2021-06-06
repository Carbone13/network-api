using LiteNetLib;
using LiteNetLib.Utils;

namespace Network.Packet
{
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

    
    /// <summary>
    /// A packet to join a lobby
    /// Contains a nickname and the lobby password
    /// </summary>
    public class JoinLobbyRequest
    {
        public string Name { get; set; }
        public string Password { get; set; }
    }

    public class JoinLobbyRequestAnswer
    {
        public bool canConnect { get; set; }
        public string errorMessage { get; set; }
    }
    
    public class Lobby
    {
        public string HostNickname { get; set; }
        public PeerAddress HostAddress { get; set; }
        public string LobbyName { get; set; }
        public string LobbyPassword { get; set; }
        public int ClientCount { get; set; }
    }
    
    public struct LobbyInformation : INetSerializable
    {
        public string HostNickname { get; set; }
        public PeerAddress HostAddress { get; set; }
        public string LobbyName { get; set; }
        public string LobbyPassword { get; set; }
        public int ClientCount { get; set; }
        
        public void Serialize (NetDataWriter writer)
        {
            writer.Put(HostNickname);
            writer.Put(HostAddress.Address);
            writer.Put(HostAddress.Port);
            writer.Put(LobbyName);
            writer.Put(LobbyPassword);
            writer.Put(ClientCount);
        }

        public void Deserialize (NetDataReader reader)
        {
            HostNickname = reader.GetString();
            HostAddress = new PeerAddress(reader.GetString(), reader.GetInt());
            LobbyName = reader.GetString();
            LobbyPassword = reader.GetString();
            ClientCount = reader.GetInt();
        }
    }
    
    public class QueryAvailableLobbies {}
    
    public class AvailableLobbies
    {
        public LobbyInformation[] Lobbies { get; set; }
    }


}