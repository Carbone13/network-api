using LiteNetLib;

namespace Network.Packet
{
    public interface IPacket
    {
        NetworkPeer Sender { get; set; }

        bool CheckIfLegit ();
        void Send(NetPeer target, DeliveryMethod method);
    }
}