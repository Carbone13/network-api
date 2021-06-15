using Godot;
using LiteNetLib;
using System.Threading.Tasks;
using System.Net;
using System;

namespace Network.HolePunching
{
    public class HolePuncherModule
    {
        public Action<NetPeer, NetworkPeer> OnConnectSuccessful;
        public HolePuncherModule ()
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
            NetPeer peer = await ConnectToward(_invitation.GetCorrectEndPoint());
            
            if(peer != null)
                OnConnectSuccessful?.Invoke(peer, _invitation.Target);
        }
        
        /// <summary>
        /// Try to connect toward a specific Endpoints Couple
        /// The peer at the endpoint must do the same toward us or it won't work !
        /// </summary>
        /// <returns></returns>
        private async Task<NetPeer> ConnectToward (IPEndPoint target)
        {
            //if (NetworkManager.singleton.Us.HighAuthority) return null;
            
            int connectionAttempts = 0;
            GD.Print(" >> Connecting toward " + target + " using NAT Hole Punching");

            NetPeer peer = NetworkManager.singleton.TryConnect(target, "");

            // If peer is null, wait one frame and retry to connect
            while (peer == null)
            {
                await Task.Delay(10);
                peer = NetworkManager.singleton.TryConnect(target, "");
            }

            await Task.Delay(500);
            while (peer.ConnectionState != ConnectionState.Connected)
            {
                if (connectionAttempts > 3)
                {
                    GD.PrintErr("  >>> Could not connect after 4 attempts");
                    return null;
                }
                await Task.Delay(500);
                
                peer.Disconnect();
                peer = NetworkManager.singleton.TryConnect(target, "");
                
                connectionAttempts++;
            }
            
            GD.Print("  >>> Connection successful");
            return peer;
        }
    }
}