using System.Net;
using LiteNetLib.Utils;

namespace Network
{
    // Represent someone in the network
    public struct NetworkPeer : INetSerializable
    {
        public string Nickname { get; set; }  // Nickname
        public EndpointCouple Endpoints { get; set; } // Addresses
        public bool HighAuthority { get; set; } // Does it has high authority ? (= is it an Host or the Lobby-Er) ?

        public NetworkPeer (string _name, EndpointCouple _addresses, bool _authority = false)
        {
            Nickname = _name;
            Endpoints = _addresses;
            HighAuthority = _authority;
        }

        public void Serialize (NetDataWriter writer)
        {
            writer.Put(Nickname);
            writer.Put(Endpoints);
            writer.Put(HighAuthority);
        }

        public void Deserialize (NetDataReader reader)
        {
            Nickname = reader.GetString();
            Endpoints = reader.Get<EndpointCouple>();
            HighAuthority = reader.GetBool();
        }
    }

    // Represent both the public and the private address of a NetworkPeer (or anything else)
    // Public should be used in remote context, and private in lan context
    public struct EndpointCouple : INetSerializable
    {
        public IPEndPoint Public { get; set;}
        public IPEndPoint Private { get; set; }

        public EndpointCouple (IPEndPoint _public, IPEndPoint _private)
        {
            Public = _public;
            Private = _private;
        }

        // Check if this address correspond to the specified peer
        public bool CorrespondTo (IPEndPoint peer)
        {
            return
                Public.ToString() == peer.ToString()
                ||
                Private.ToString() == peer.ToString();
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
}