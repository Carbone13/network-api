using Godot;
using Network;
using System.Collections.Generic;
using System.Linq;
using LiteNetLib;
using LiteNetLib.Utils;
using Network.Packet;


public class MenuManager : Node
{
    public AcceptDialog popup;
    public LineEdit nickname, lobbyName, lobbyPassword;
    public ItemList lobbyList;

    public List<Lobby> lobbies = new List<Lobby>();

    private int _selectedLobby;

    private NetPeer _nat;

    private Lobby targetLobby;
    
    public override void _Ready ()
    {
        GatherReferences();

        // Connect to the NAT Server
        _nat = NetworkManager.singleton.Connect(new PeerAddress("127.0.0.1", 3455), "");
        NetworkManager.singleton.Socket.Processor.SubscribeReusable<AvailableLobbies>(AvailableLobbyQueryAnswer);
        NetworkManager.singleton.Socket.Processor.SubscribeReusable<JoinLobbyRequestAnswer, NetPeer>(OnConnectionRequestAnswer);
        
        RefreshLobbyList();
    }


    public void TryLobbyCreation ()
    {
        if (targetLobby != null)
            return;
        
        if (lobbyName.Text == "")
        {
            popup.DialogText = "Please specifiy a name for your Lobby";
            popup.Show();
            return;
        }
        if (nickname.Text == "")
        {
            popup.DialogText = "Please specifiy a nickname";
            popup.Show();
            return;
        }
        
        CreateAndRegisterLobby();
    }
    
    public void TryLobbyJoin ()
    {
        if (targetLobby != null)
            return;
        
        if (nickname.Text == "")
        {
            popup.DialogText = "Please specifiy a nickname";
            popup.Show();
            return;
        }
        
        JoinLobby();
    }
    
    public void CreateAndRegisterLobby ()
    {
        Lobby lobby = new Lobby();
        lobby.ClientCount = 1;
        lobby.HostNickname = nickname.Text;
        lobby.LobbyName = lobbyName.Text;
        lobby.LobbyPassword = lobbyPassword.Text;

        _nat.Send(NetworkManager.singleton.Socket.Processor.Write(lobby), DeliveryMethod.ReliableOrdered);

        Node lobbyMenu = ResourceLoader.Load<PackedScene>("res://Exemples/Scenes/Lobby.tscn").Instance();
        GetTree().Root.AddChild(lobbyMenu);
        GetTree().Root.GetNode<LobbyManager>("Lobby").Initialize(lobby, true, _nat);
    }
    
    public void JoinLobby ()
    {
        targetLobby = lobbies[_selectedLobby];
        if ( targetLobby == null) return;


        JoinLobbyRequest request = new JoinLobbyRequest();
        request.Name = nickname.Text;

        // Connect to the host of the target lobby and ask to join
        NetPeer lobbyHost = NetworkManager.singleton.Connect( targetLobby.HostAddress, "");
        lobbyHost.Send(NetworkManager.singleton.Socket.Processor.Write(request), DeliveryMethod.ReliableOrdered);
    }

    public void OnConnectionRequestAnswer (JoinLobbyRequestAnswer answer, NetPeer peer)
    {
        if (answer.canConnect)
        {
            Node lobbyMenu = ResourceLoader.Load<PackedScene>("res://Exemples/Scenes/Lobby.tscn").Instance();
            GetTree().Root.AddChild(lobbyMenu);
            GetTree().Root.GetNode<LobbyManager>("Lobby").Initialize(targetLobby, false, _nat);
        }
        else
        {
            NetworkManager.singleton.Socket.peers.Remove(peer);
            peer.Disconnect();
        }
    }
    
    public void RefreshLobbyList ()
    {
        // Clear actual list
        lobbyList.Clear();
        lobbies.Clear();
        
        // And send a Request to the NAT Server
        QueryAvailableLobbies query = new QueryAvailableLobbies();
        _nat.Send(NetworkManager.singleton.Socket.Processor.Write(query), DeliveryMethod.ReliableOrdered);
    }

    public void AvailableLobbyQueryAnswer (AvailableLobbies answer)
    {
        GD.Print("gg");
        foreach (LobbyInformation lobby in answer.Lobbies)
        {
            Lobby lobbyInstance = new Lobby();
            lobbyInstance.ClientCount = lobby.ClientCount;
            lobbyInstance.HostAddress = lobby.HostAddress;
            lobbyInstance.HostNickname = lobby.HostNickname;
            lobbyInstance.LobbyName = lobby.LobbyName;
            lobbyInstance.LobbyPassword = lobby.LobbyPassword;
            
            lobbyList.AddItem(lobby.LobbyName + " - hosted by " + lobby.HostNickname);
            lobbies.Add(lobbyInstance);
        }
    }
    
    public void LobbySelected (int id)
    {
        _selectedLobby = id;
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
