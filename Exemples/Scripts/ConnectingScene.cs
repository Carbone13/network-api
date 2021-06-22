using Godot;
using System.Threading.Tasks;
using LiteNetLib;
using Network;
using Network.Packet;

public class ConnectingScene : Node
{
    // Status UI Nodes
    public Node ConnecToHost, WaitForHostAccept, ConnectToClient, AdditionalInfo;

    public Lobby TargetLobby;
    
    private NetPeer LobbyHost;
    
    private bool _ready;
    private int _connectedClientsCount;

    public override void _Ready ()
    {
        GD.Print();
        GatherReferences();
        GetTree().Root.GetNode("Menu").QueueFree();
        //GetTree().Root.CallDeferred("remove_child", GetTree().Root.GetNode("Menu"));
        _ready = true;
    }

    public async void Initialize (Lobby target)
    {
        while (!_ready)
            await Task.Delay(16);

        TargetLobby = target;

        GD.Print("> Trying to join " + TargetLobby.Name + " lobby.");
        GD.Print(" >> Connecting to the Host");
        GD.Print(" >> Asking Lobby-Er to setup a Rendez-Vous");
        
        NetworkManager.singleton.HolePuncher.Connect(TargetLobby.Host);

        // Subscribe to "Connect" & "Disconnect" event
        NetworkManager.singleton.Socket.Events.PeerConnectedEvent += OnConnect;
        NetworkManager.singleton.Socket.Events.PeerDisconnectedEvent += OnDisconnect;
        
        NetworkManager.Processor.SubscribeReusable<RegisterAndUpdateLobbyState>(OnAdditionalInformationsReceived);
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
            //WhenConnectedToHost();
        }
    }

    public void WhenConnectedToHost ()
    {
        GD.Print("> Connecting to Host, asking for others peers");
        ConnecToHost.GetNode<Node2D>("Checkmark").Show();
        ConnecToHost.GetNode<Node2D>("Load").Hide();
        WaitForHostAccept.GetNode<Node2D>("Load").Show();
        
        OnHostAcceptationReceived();
        if (TargetLobby.ConnectedPeers.Count > 1)
        {
            GD.Print(" >> Some clients were already here, asking host their address");
        }
        else
        {
            GD.Print(" >> Host is alone");
            WhenConnectedToEveryClient();
        }
    }

    public void OnHostAcceptationReceived ()
    {
        WaitForHostAccept.GetNode<Node2D>("Checkmark").Show();
        WaitForHostAccept.GetNode<Node2D>("Load").Hide();
        ConnectToClient.GetNode<Node2D>("Load").Show();
            
        GD.Print(" >> Host accepted us");
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
        ConnectToClient.GetNode<Node2D>("Checkmark").Show();
        ConnectToClient.GetNode<Node2D>("Load").Hide();
        AdditionalInfo.GetNode<Node2D>("Load").Show();
    }

    public async void OnAdditionalInformationsReceived (RegisterAndUpdateLobbyState infos)
    {
        GD.Print("  >>> Received last informations, ready to load lobby scene !");
        TargetLobby = infos.Lobby;
        
        AdditionalInfo.GetNode<Node2D>("Checkmark").Show();
        AdditionalInfo.GetNode<Node2D>("Load").Hide();
        
        GD.Print("  >>> Changing scene");
            
        Node lobbyScene = ResourceLoader.Load<PackedScene>("res://Exemples/Scenes/Lobby.tscn").Instance();
        GetTree().Root.CallDeferred("add_child", lobbyScene);

        while (GetTree().Root.GetNodeOrNull("Lobby") == null)
        {
            await Task.Delay(17);
        }

        LobbyManager manager = lobbyScene as LobbyManager;

        GD.Print("  >>> Initializing lobby as Client");
        manager.Initialize(TargetLobby, false);
    }

    private void GatherReferences ()
    {
        ConnecToHost = GetNode("C -> Host");
        WaitForHostAccept = GetNode("Host Accept");
        ConnectToClient = GetNode("C -> Client");
        AdditionalInfo = GetNode("Additional");
    }
}
