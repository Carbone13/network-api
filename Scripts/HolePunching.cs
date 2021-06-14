using Godot;
using LiteNetLib;
using Network.Packet;
using System.Threading.Tasks;
using System.Net;
using System;
using Network.HolePunching.Packet;

namespace Network.HolePunching
{
    public class HolePuncher
    {
        public Action<NetPeer, NetworkPeer> OnConnectSuccessful;
        public HolePuncher ()
        {
            NetworkManager.Processor.SubscribeReusable<RendezVousInvitation>(OnInvitationReceiption);
        }
        
        // Try to connect toward a Network Peer
        public void Connect (NetworkPeer target)
        {
            GD.Print("> Asking the Lobby-Er to setup a Rendez-Vous between us and " + target.Endpoints.Public);
            AskRendezVous lobbyerRequest =
                new AskRendezVous(NetworkManager.singleton.Us, NetworkManager.singleton.Us, target);
            
            lobbyerRequest.Send(NetworkManager.singleton.LobbyEr, DeliveryMethod.ReliableOrdered);
        }
        
        // Setup your own rendezvous without using the Lobby-Er
        // Require that the 2 target are already connected to you 
        public void SetupRendezVous (NetworkPeer first, NetworkPeer second)
        {
            NetPeer peerOne = NetworkManager.singleton.Socket.GetPeer(first);
            NetPeer peerTwo = NetworkManager.singleton.Socket.GetPeer(second);

            RendezVousInvitation invitationToOne = new RendezVousInvitation(NetworkManager.singleton.Us, second);
            RendezVousInvitation invitationToTwo = new RendezVousInvitation(NetworkManager.singleton.Us, first);
            
            invitationToOne.Send(peerOne, DeliveryMethod.ReliableOrdered);
            invitationToTwo.Send(peerTwo, DeliveryMethod.ReliableOrdered);
        }
        
        private async void OnInvitationReceiption (RendezVousInvitation _invitation)
        {
            GD.Print("> Received Rendez-Vous invitation");
            await ConnectToward(_invitation.GetCorrectEndPoint());
            
            OnConnectSuccessful?.Invoke(NetworkManager.singleton.Socket.GetPeer(_invitation.Target), _invitation.Target);
        }
        
        /// <summary>
        /// Try to connect toward a specific Endpoints Couple
        /// The peer at the endpoint must do the same toward us or it won't work !
        /// </summary>
        /// <returns></returns>
        private async Task<NetPeer> ConnectToward (IPEndPoint target)
        {
            int connectionAttempts = 0;
            GD.Print(" >> Connecting toward " + target + " using NAT Hole Punching");

            NetPeer peer = NetworkManager.singleton.TryConnect(target, "");

            // If peer is null, wait one frame and retry to connect
            while (peer == null)
            {
                await Task.Delay(17);
                peer = NetworkManager.singleton.TryConnect(target, "");
            }

            await Task.Delay(400);
            while (peer.ConnectionState != ConnectionState.Connected)
            {
                if (connectionAttempts > 3)
                {
                    GD.PrintErr("  >>> Could not connect after 4 attempts");
                }
                
                peer = NetworkManager.singleton.TryConnect(target, "");
                connectionAttempts++;

                await Task.Delay(400);
            }
            
            GD.Print("  >>> Connection successful");
            return peer;
        }
    }
}

// Hole punching packets
namespace Network.HolePunching.Packet
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
        {
            throw new NotImplementedException();
        }
        
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