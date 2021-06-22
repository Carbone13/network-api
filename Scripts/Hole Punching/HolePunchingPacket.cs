using System.Net;
using LiteNetLib;

namespace Network.HolePunching
{
    /// <summary>
    /// Ask the Lobby-Er to setup a Rendez-Vous between you and the
    /// specified target
    /// </summary>
    public class AskRendezVous : IPacket
    {
        public NetworkPeer Sender { get; set; }
        
        public NetworkPeer You { get; set; }
        public NetworkPeer Target { get; set; }
        

        public AskRendezVous (NetworkPeer _sender, NetworkPeer _you, NetworkPeer _target)
        {
            Sender = _sender;
            You = _you;
            Target = _target;
        }
        
        public bool CheckIfLegit ()
            => true;

        public void Send (NetPeer target, DeliveryMethod method)
            => target.Send(NetworkManager.Processor.Write(this), method);
        
        public AskRendezVous () {}
    }

    /// <summary>
    /// Rendez-Vous invitation sent by the Lobby-Er
    /// Both the original asker and the target receive an invitation
    /// </summary>
    public class RendezVousInvitation : IPacket
    {
        public NetworkPeer Sender { get; set; }
        
        public NetworkPeer Target { get; set; }

        public IPEndPoint GetCorrectEndPoint ()
        {
            // If we have the same Private address (= we are on the same lan)
            if (Target.Endpoints.Private.Address.ToString() == NetworkManager.singleton.Us.Endpoints.Private.Address.ToString())
            {
                // Use the private Address
                return Target.Endpoints.Private;
            }
            
            // else, use the public one
            return Target.Endpoints.Public;
        }

        public RendezVousInvitation (NetworkPeer _sender, NetworkPeer _target)
        {
            Sender = _sender;
            Target = _target;
        }
        
        public bool CheckIfLegit ()
            => Sender.HighAuthority;

        public void Send (NetPeer target, DeliveryMethod method)
            => target.Send(NetworkManager.Processor.Write(this), method);
        
        public RendezVousInvitation () {}
    }
}