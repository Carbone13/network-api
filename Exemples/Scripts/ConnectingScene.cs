using Godot;
using System.Threading.Tasks;
using LiteNetLib;
using Network;
using Network.Packet;
using Network.HolePunching;
using Network.HolePunching.Packet;

public class ConnectingScene : Node
{
    public Lobby TargetLobby;
    
    private NetPeer LobbyHost;
    
    private bool _ready;
    private int _connectedClientsCount;

    public override void _Ready ()
    {
        GD.Print();

        _ready = true;
    }

    public async void Initialize (Lobby target)
    {
        while (!_ready)
            await Task.Delay(16);
        
        GetTree().Root.RemoveChild(GetTree().Root.GetNode("Lobby"));

        TargetLobby = target;

        GD.Print("> Trying to join " + TargetLobby.Name + " lobby.");
        GD.Print(" >> Connecting to the Host");
        GD.Print(" >> Asking Lobby-Er to setup a Rendez-Vous");
        
        NetworkManager.singleton.HolePuncher.Connect(TargetLobby.Host);

        // Subscribe to "Connect" & "Disconnect" event
        NetworkManager.singleton.Socket.PeerConnection += OnConnect;
        NetworkManager.singleton.Socket.PeerDisconnection += OnDisconnect;
    }

    private void OnConnect (NetPeer peer)
    {
        if (TargetLobby.Host.Endpoints.CorrespondTo(peer.EndPoint))
        {
            LobbyHost = peer;
            WhenConnectedToHost();
        }
        else
        {
            foreach (NetworkPeer lobbyClient in TargetLobby.ConnectedPeers)
            {
                if (lobbyClient.Endpoints.CorrespondTo(peer.EndPoint))
                {
                    WhenConnectedToOneClient();
                }
            }
        }
    }

    private void OnDisconnect (NetPeer peer, DisconnectInfo info)
    {
        if (TargetLobby.Host.Endpoints.CorrespondTo(peer.EndPoint))
        {
            LobbyHost = peer;
            WhenConnectedToHost();
        }
    }

    public void WhenConnectedToHost ()
    {
        // Once Connected to Host, ask for others peers
        AskForOtherClients ask = new AskForOtherClients(NetworkManager.singleton.Us);
        ask.Send(LobbyHost, DeliveryMethod.ReliableOrdered);
    }

    public void OnHostAcceptationReceived ()
    {
        
    }

    public void WhenConnectedToOneClient ()
    {
        _connectedClientsCount++;

        if (_connectedClientsCount - 1 == TargetLobby.ConnectedPeers.Count)
        {
            WhenConnectedToEveryClient();
        }
    }
    
    public void WhenConnectedToEveryClient ()
    {
        GD.Print("  >>> Connected to every clients, connection successfull !");
    }
}
