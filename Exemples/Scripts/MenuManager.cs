using Godot;
using Network;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using Network.Packet;

public class MenuManager : Node
{
    public AcceptDialog popup;
    public LineEdit nickname, lobbyName, lobbyPassword;
    public ItemList lobbyList;

    public NetPeer toNat;

    private List<Lobby> lobbies = new List<Lobby>();

    private bool _connectedToNat;

    private bool host;
    //TODO check if sender is the intended sender at most of the packet
    public override void _Ready ()
    {
        GatherReferences();

        GD.Print("> Connecting to Lobby-Er");
        GD.Print(" >> Waiting for confirmation...");
        
        toNat = NetworkManager.singleton.Connect(new PeerAddress("90.76.187.136", 3456), "");
        NetworkManager.singleton.Socket.PeerConnection += OnConnected;

        NetworkManager.Processor.SubscribeReusable<Lobby>(ReceiveLobbyInfo);
        NetworkManager.Processor.SubscribeReusable<LobbyConnectConfirmationFromHost>(OnConnectionToLobbyConfirmed);
    }

    public void OnConnected (NetPeer who)
    {
        if (who == toNat)
        {
            GD.Print(" >> Connected !");
            _connectedToNat = true;
        }
    }

    public void TryLobbyCreation ()
    {
        GD.Print("> Trying to create a lobby.");
        if(!_connectedToNat) GD.PrintErr(" >> ERROR: not connected to Lobby-Er");
        if (lobbyName.Text == "")
        {
            popup.DialogText = "Please specifiy a name for your Lobby";
            GD.PrintErr(" >> ERROR: no lobby name specified");
            popup.Show();
            return;
        }
        if (nickname.Text == "")
        {
            popup.DialogText = "Please specifiy a nickname";
            GD.PrintErr(" >> ERROR: no nickname specified");
            popup.Show();
            return;
        }

        Lobby hosted = new Lobby();
        hosted.HostName = nickname.Text;
        hosted.LobbyName = lobbyName.Text;
        hosted.HostPublicAddress = new IPEndPoint(IPAddress.Any, 0000);
        hosted.PlayerCount = 1;
        hosted.MaxPlayer = 3;
        
        GD.Print(" >> Setted up lobby.");
        GD.Print("  >>> Registering our server toward Lobby-Er");
        toNat.Send(NetworkManager.Processor.Write(hosted), DeliveryMethod.ReliableOrdered);
        host = true;
        
        Node lobbyScene = ResourceLoader.Load<PackedScene>("res://Exemples/Scenes/Lobby.tscn").Instance();
        GetTree().Root.AddChild(lobbyScene);
        LobbyManager manager = lobbyScene as LobbyManager;

        manager.Initialize(hosted, true, toNat, nickname.Text);
    }

    public void TryLobbyJoin ()
    {
        GD.Print("> Trying to connect to selected lobby");
        if (selectedLobbyID < 0)
        {
            GD.PrintErr(" >> ERROR: no lobby selected");
            return;
        }
        if(!_connectedToNat) GD.PrintErr(" >> ERROR: not connected to Lobby-Er");
        if (nickname.Text == "")
        {
            popup.DialogText = "Please specifiy a nickname";
            GD.PrintErr(" >> ERROR: no nickname specified");
            popup.Show();
            return;
        }

        GD.Print("  >> Notifying Lobby-Er that we want to join.");
        Lobby selected = lobbies[selectedLobbyID];
        JoinLobby order = new JoinLobby();
        order.HostPublicAddress = selected.HostPublicAddress;
        
        toNat.Send(NetworkManager.Processor.Write(order), DeliveryMethod.ReliableOrdered);
    }

    private bool _connectedToLobby;
    
    // This is the final packet sent by the host, when every primal connections are successfull
    public void OnConnectionToLobbyConfirmed (LobbyConnectConfirmationFromHost confirmation)
    {
        if(_connectedToLobby) return;
        _connectedToLobby = true;
        GD.Print("> Host accepted us !");
        GD.Print(" >> Loading the Lobby...");
        GD.Print(" >> Disconnecting from Lobby-Er");
        toNat.Disconnect();

        Node lobbyScene = ResourceLoader.Load<PackedScene>("res://Exemples/Scenes/Lobby.tscn").Instance();
        GetTree().Root.AddChild(lobbyScene);
        LobbyManager manager = lobbyScene as LobbyManager;

        manager.Initialize(new Lobby(), false, null, nickname.Text);
    }

    public void RefreshLobbyList ()
    {
        GD.Print("> Refreshing lobby list");
        if(!_connectedToNat) GD.PrintErr(" >> ERROR: not connected to Lobby-Er");
        
        lobbyList.Clear();
        lobbies.Clear();
        
        RequestLobbyList request = new RequestLobbyList();

        GD.Print(" >> Sending request to Lobby-Er");
        toNat.Send(NetworkManager.Processor.Write(request), DeliveryMethod.ReliableOrdered);
    }

    public void ReceiveLobbyInfo (Lobby lobby)
    {
        GD.Print("  >>> Received one lobby-answer from the Lobby-Er");
        lobbies.Add(lobby);
        lobbyList.AddItem(lobby.LobbyName + " - hosted by " + lobby.HostName + " (" + lobby.PlayerCount + "/" + lobby.MaxPlayer + ")");
    }

    private int selectedLobbyID;
    
    public void LobbySelected (int id)
    {
        selectedLobbyID = id;
    }

    private void GatherReferences ()
    {
        popup = GetNode<AcceptDialog>("Popup");

        lobbyList = GetNode<ItemList>("Lobbies/List");
        
        nickname = GetNode<LineEdit>("Nickname/LineEdit");
        lobbyName = GetNode<LineEdit>("Hosting/Name input");
        lobbyPassword = GetNode<LineEdit>("Hosting/Password input");
    }
}
