using System.Collections.Generic;
using Network.Packet;
using LiteNetLib;
using Network;
using Godot;

public class MenuManager : Node
{
    // Scene references
    public AcceptDialog popup;
    public LineEdit nickname, lobbyName, lobbyPassword;
    public Label maxPlayerLabel;
    public ItemList lobbyList;
    
    // Available Lobbies
    private List<Lobby> lobbies = new List<Lobby>();
    
    private int _selectedLobbyID;
    private int _maxPlayer = 3;
    
    public override void _Ready ()
    {
        GatherReferences();

        NetworkManager.Processor.SubscribeReusable<LobbyListAnswer>(ReceiveLobbyInfo);
    }
    
    #region UI & Scene 
    // When we select a lobby in this list
    public void LobbySelected (int id)
    {
        _selectedLobbyID = id;
    }
    
    // When we change the max allowed player in the lobby creation tab
    public void OnMaxPlayerSliderChange (float maxPlayer)
    {
        _maxPlayer = (int)maxPlayer;
        maxPlayerLabel.Text = _maxPlayer.ToString();
    }
    
    // Gather ui references
    public void GatherReferences ()
    {
        popup = GetNode<AcceptDialog>("Popup");

        lobbyList = GetNode<ItemList>("Lobbies/List");
        
        nickname = GetNode<LineEdit>("Nickname/LineEdit");
        lobbyName = GetNode<LineEdit>("Hosting/Name input");
        lobbyPassword = GetNode<LineEdit>("Hosting/Password input");

        maxPlayerLabel = GetNode<Label>("Hosting/Max Player Count");
    }
    #endregion
    
    #region Hosting
    
    public void TryLobbyCreation ()
    {
        GD.Print("> Trying to create a lobby.");

        if (IsNicknameValid() && IsLobbyNameValid())
        {
            NetworkManager.singleton.Us.HighAuthority = true;
            NetworkManager.singleton.Us.Nickname = nickname.Text;
        
            Lobby hosted = new Lobby(NetworkManager.singleton.Us, lobbyName.Text, 
                PasswordHasher.Hash(lobbyPassword.Text, 1000), _maxPlayer, new List<NetworkPeer>());
            hosted.ConnectedPeers.Add(NetworkManager.singleton.Us);

            GD.Print(" >> Setted up lobby.");
            GD.Print("  >>> Registering our server toward Lobby-Er");

            RegisterAndUpdateLobbyState registering = new RegisterAndUpdateLobbyState(NetworkManager.singleton.Us, hosted);
            registering.Send(NetworkManager.singleton.LobbyEr, DeliveryMethod.ReliableOrdered);
            
            GD.Print("  >>> Changing scene");
            
            Node lobbyScene = ResourceLoader.Load<PackedScene>("res://Exemples/Scenes/Lobby.tscn").Instance();
            GetTree().Root.AddChild(lobbyScene);
            LobbyManager manager = lobbyScene as LobbyManager;

            GD.Print("  >>> Initializing lobby as Host");
            manager.Initialize(hosted, true);
        }
    }
    
    #endregion
    
    #region Joining
    
    public void TryLobbyJoin ()
    {
        GD.Print("> Trying to connect to selected lobby");

        if (IsNicknameValid())
        {
            if (_selectedLobbyID < 0)
            {
                GD.PrintErr(" >> ERROR: no lobby selected");
                return;
            }

            NetworkManager.singleton.Us.Nickname = nickname.Text;
            
            Node lobbyScene = ResourceLoader.Load<PackedScene>("res://Exemples/Scenes/Connecting.tscn").Instance();
            GetTree().Root.AddChild(lobbyScene);
            ConnectingScene connectingScene = lobbyScene as ConnectingScene;
        
            connectingScene.Initialize(lobbies[_selectedLobbyID]);
        }
    }
    
    #endregion
    
    #region Lobby Query
    
    // When we press the refresh button
    public void RefreshLobbyList ()
    {
        QueryLobbyList query = new QueryLobbyList(NetworkManager.singleton.Us);
        query.Send(NetworkManager.singleton.LobbyEr, DeliveryMethod.ReliableOrdered);
        
        lobbies.Clear();
        lobbyList.Clear();
    }
    
    // When we receive the list of available lobbies from Lobby-Er
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
    #endregion
    
    #region Shared

    public bool IsNicknameValid ()
    {        
        if (nickname.Text == "")
        {
            popup.DialogText = "Please specifiy a nickname";
            GD.PrintErr(" >> ERROR: no nickname specified");
            popup.Show();
            return false;
        }

        return true;
    }

    public bool IsLobbyNameValid ()
    {
        if (lobbyName.Text == "")
        {
            popup.DialogText = "Please specifiy a name for your Lobby";
            GD.PrintErr(" >> ERROR: no lobby name specified");
            popup.Show();
            
            return false;
        }

        return true;
    }
    
    #endregion
}
