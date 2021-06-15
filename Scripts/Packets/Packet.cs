using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Collections.Generic;

// NOTE: Lobby-Er refers to https://github.com/Carbone13/lobby-er
namespace Network.Packet
{
    // Contains a Endpoint, considered as your public address
    // Lobby-Er sent you this packet so you are aware of your public address
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
}