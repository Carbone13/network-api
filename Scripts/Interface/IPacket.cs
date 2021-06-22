using LiteNetLib;

namespace Network
{
    public interface IPacket
    {
        NetworkPeer Sender { get; set; }

        bool CheckIfLegit ();
        void Send(NetPeer target, DeliveryMethod method);
    }
}