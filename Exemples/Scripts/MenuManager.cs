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
    public HSlider maxPlayerSlider;
    public Label maxPlayerLabel;
    public ItemList lobbyList;

    public NetPeer toNat;

    private List<Lobby> lobbies = new List<Lobby>();

    private bool _connectedToNat;

    private bool host;
    private Lobby targetLobby;

    public override void _Ready ()
    {
        GatherReferences();

        GD.Print("> Connecting to Lobby-Er");
        GD.Print(" >> Waiting for confirmation...");
        
        toNat = NetworkManager.singleton.TryConnect(new IPEndPoint(IPAddress.Parse("90.76.187.136"), 3456), "");

        NetworkManager.singleton.Socket.PeerConnection += OnConnected;

        NetworkManager.Processor.SubscribeReusable<LobbyListAnswer>(ReceiveLobbyInfo);
        //NetworkManager.Processor.SubscribeReusable<LobbyConnectConfirmationFromHost>(OnConnectionToLobbyConfirmed);

        _maxPlayer = (int)maxPlayerSlider.Value;
    }

    public void OnConnected (NetPeer who)
    {
        if (who == toNat)
        {
            GD.Print(" >> Connected !");
            _connectedToNat = true;
        }
        if(targetLobby.Host.Endpoints.Private != null)
        {
            if(who.EndPoint.ToString() == targetLobby.Host.Endpoints.Private.ToString() || 
            who.EndPoint.ToString() == targetLobby.Host.Endpoints.Public.ToString())
            {
                OnConnectionToLobbyConfirmed();
            }
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
        NetworkManager.singleton.lanHost = true;

        NetworkManager.singleton.Us.HighAuthority = true;
        NetworkManager.singleton.Us.Nickname = nickname.Text;
        Lobby hosted = new Lobby(NetworkManager.singleton.Us, lobbyName.Text, PasswordHasher.Hash(lobbyPassword.Text, 1000), _maxPlayer, new List<NetworkPeer>());
        hosted.ConnectedPeers.Add(NetworkManager.singleton.Us);

        GD.Print(" >> Setted up lobby.");
        GD.Print("  >>> Registering our server toward Lobby-Er");

        RegisterAndUpdateLobbyState registering = new RegisterAndUpdateLobbyState(NetworkManager.singleton.Us, hosted);
        registering.Send(toNat, DeliveryMethod.ReliableOrdered);

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

        NetworkManager.singleton.Us.Nickname = nickname.Text;
        GD.Print("  >> Notifying Lobby-Er that we want to join.");
        Lobby selectedLobby = lobbies[selectedLobbyID];
        targetLobby = selectedLobby;

        AskToJoinLobby joinPacket = new AskToJoinLobby(NetworkManager.singleton.Us, selectedLobby, "");
        
        joinPacket.Send(toNat, DeliveryMethod.ReliableOrdered);
    }

    private bool _connectedToLobby;
    

    public async void OnConnectionToLobbyConfirmed ()
    {
        if(_connectedToLobby) return;
        _connectedToLobby = true;
        GD.Print("> Host accepted us !");
        GD.Print(" >> Loading the Lobby...");
        GD.Print(" >> Disconnecting from Lobby-Er");
        toNat.Disconnect();

        Node lobbyScene = ResourceLoader.Load<PackedScene>("res://Exemples/Scenes/Lobby.tscn").Instance();
        GetTree().Root.CallDeferred("add_child", lobbyScene);
        
        while(GetTree().Root.GetNodeOrNull("Lobby") == null)
        {
            await Task.Delay(17);
        }

        LobbyManager manager = lobbyScene as LobbyManager;

        manager.Initialize(targetLobby, false, null, nickname.Text);
    }

    public void RefreshLobbyList ()
    {
        GD.Print("> Refreshing lobby list");
      
        lobbyList.Clear();
        lobbies.Clear();
        
        QueryLobbyList request = new QueryLobbyList(NetworkManager.singleton.Us);

        GD.Print(" >> Sending request to Lobby-Er");
        toNat.Send(NetworkManager.Processor.Write(request), DeliveryMethod.ReliableOrdered);
    }

    public void ReceiveLobbyInfo (LobbyListAnswer answer)
    {
        if(!answer.CheckIfLegit()) return;

        GD.Print("  >>> Received lobby-answer from the Lobby-Er");

        foreach(Lobby lob in answer.AvailableLobbies)
        {
            lobbies.Add(lob);
            lobbyList.AddItem
                (lob.Name + " - hosted by " + lob.Host.Nickname + 
                " (" + lob.ConnectedPeers.Count + "/" + lob.MaxAuthorizedPlayer + ")");
        }
        
    }

    private int selectedLobbyID;
    
    public void LobbySelected (int id)
    {
        selectedLobbyID = id;
    }

    private int _maxPlayer;

    public void OnMaxPlayerSliderChange (float maxPlayer)
    {
        _maxPlayer = (int)maxPlayer;
        maxPlayerLabel.Text = _maxPlayer.ToString();
    }

    private void GatherReferences ()
    {
        popup = GetNode<AcceptDialog>("Popup");

        lobbyList = GetNode<ItemList>("Lobbies/List");
        
        nickname = GetNode<LineEdit>("Nickname/LineEdit");
        lobbyName = GetNode<LineEdit>("Hosting/Name input");
        lobbyPassword = GetNode<LineEdit>("Hosting/Password input");

        maxPlayerLabel = GetNode<Label>("Hosting/Max Player Count");
        maxPlayerSlider = GetNode<HSlider>("Hosting/Max Player Slider");
    }
}
