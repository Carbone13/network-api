using Godot;
using System;
using System.Threading.Tasks;
using LiteNetLib;
using Network;
using Network.Packet;
using LiteNetLib.Utils;
using System.Collections.Generic;
using System.Linq;

public class LobbyManager : Node
{
    private LineEdit _textInput;
    private TextEdit _chatBox;
    public Label _connectedCount;
    public ItemList _connectedPlayersList;
    
    public readonly Dictionary<NetPeer, NetworkPeer> connectedClients = new Dictionary<NetPeer, NetworkPeer>();
    
    private bool _isHost;
    private Lobby _lobby;

    public void Initialize (Lobby lobby, bool host)
    {
        if (host)
        {
            GD.Print("> Created lobby");
            GetTree().Root.GetNode("Menu").QueueFree();
            //GetTree().Root.CallDeferred("remove_child", GetTree().Root.GetNode("Menu"));
        }
        else
        {
            GD.Print("> Joined lobby");
            GetTree().Root.GetNode("Connecting").QueueFree();
            //GetTree().Root.CallDeferred("remove_child", GetTree().Root.GetNode("Connecting"));
        }
            

        GatherNodeReferences();
        

        _isHost = host;
        _lobby = lobby;

        NetworkManager.Processor.SubscribeReusable<LobbyChatMessage>(MessagePacketReceived);
        NetworkManager.Processor.SubscribeReusable<RegisterAndUpdateLobbyState, NetPeer>(OnLobbyUpdate);

        NetworkManager.singleton.HolePuncher.OnConnectSuccessful += OnPeerJoin;
        NetworkManager.singleton.Socket.PeerDisconnection += OnDisconnect;

        _connectedPlayersList.Clear();
        _connectedPlayersList.AddItem(NetworkManager.singleton.Us.Nickname + " (You)", null, false);
        UpdatePlayerCount();
    }
    

    public void OnLobbyUpdate (RegisterAndUpdateLobbyState updatedLobby, NetPeer sender)
    {
        GD.Print("> Received lobby update");
        if(!updatedLobby.CheckIfLegit()) return;

        _lobby = updatedLobby.Lobby;

        UpdatePlayerCount();
    }

    private void UpdatePlayerCount ()
    {
        _connectedCount.Text = _lobby.ConnectedPeers.Count + "/" + _lobby.MaxAuthorizedPlayer;
    }

    public void OnDisconnect (NetPeer peer, DisconnectInfo info)
    {
        if(_lobby.Host.Endpoints.CorrespondTo(peer.EndPoint))
        {
            GD.Print("Refused from lobby/Kicked");

            Node menuScene = ResourceLoader.Load<PackedScene>("res://Exemples/Scenes/Menu.tscn").Instance();
            GetTree().Root.CallDeferred("add_child", menuScene);
            MenuManager manager = menuScene as MenuManager;

            GetTree().Root.GetNode("Lobby").QueueFree();
            //GetTree().Root.CallDeferred("remove_child", GetTree().Root.GetNode("Lobby"));

            manager.GatherReferences();
            manager.popup.DialogText = "Host refused/kicked us !";
            manager.popup.Show();
        }

        for (int i = 0; i < connectedClients.Keys.Count; i++)
        {
            if (connectedClients.Keys.ToArray()[i] == peer)
            {
                _connectedPlayersList.RemoveItem(i);
                _lobby.ConnectedPeers.Remove(connectedClients[peer]);
                connectedClients.Remove(peer);
                UpdatePlayerCount();
                
                RegisterAndUpdateLobbyState update = new RegisterAndUpdateLobbyState(NetworkManager.singleton.Us, _lobby);
                update.Send(NetworkManager.singleton.LobbyEr, DeliveryMethod.ReliableOrdered);

                foreach(NetPeer _peer in connectedClients.Keys)
                    update.Send(_peer, DeliveryMethod.ReliableOrdered);
            }
        }
    }

    // Network/Lobby management
    public void OnPeerJoin (NetPeer peer, NetworkPeer peerInfo)
    {
        if(peer == null) return;
        if(!_isHost) return;

        if(_lobby.ConnectedPeers.Count >= _lobby.MaxAuthorizedPlayer)
        {
            // TODO
            peer.Disconnect();
            GD.Print("New peer joined but we are full");
            return;
        }
        GD.Print("> New peer joined the lobby");
        Color col = _chatBox.GetColor("font_color");
        
        _chatBox.Text += "New player joined: " + peerInfo.Nickname + "\n";
        _connectedPlayersList.AddItem(peerInfo.Nickname);
        
        GD.Print(" >> Setting up RendezVous with already connected peers");

        foreach(NetworkPeer alreadyConnected in connectedClients.Values)
        {
            NetworkManager.singleton.HolePuncher.SetupRendezVous(peerInfo, alreadyConnected);
        }
        
        connectedClients.Add(peer, peerInfo);
        
        GD.Print("Updating lobby state, and informing Lobby-Er & Clients");
        // Update our lobby statut
        _lobby.ConnectedPeers.Add(connectedClients[peer]);
        
        RegisterAndUpdateLobbyState update = new RegisterAndUpdateLobbyState(NetworkManager.singleton.Us, _lobby);
        update.Send(NetworkManager.singleton.LobbyEr, DeliveryMethod.ReliableOrdered);

        foreach(NetPeer _peer in connectedClients.Keys)
            update.Send(_peer, DeliveryMethod.ReliableOrdered);

        _connectedCount.Text = _lobby.ConnectedPeers.Count + "/" + _lobby.MaxAuthorizedPlayer;
    }

    // Chatbox specific

    public override void _Input (InputEvent @event)
    {
        if (@event is InputEventKey eventKey)
        {
            if (eventKey.IsPressed() && eventKey.Scancode == (int) KeyList.Enter)
            {
                if (_textInput.Text != "")
                {
                    GD.Print("> Sending a Message");
                    
                    NetDataWriter writer = new NetDataWriter();
                    
                    LobbyChatMessage lm = new LobbyChatMessage(NetworkManager.singleton.Us, _textInput.Text);

                    NetworkManager.Processor.Write(writer, lm);
                    NetworkManager.singleton.Socket.BroadcastToPeers(writer, DeliveryMethod.ReliableOrdered);

                    AddLine(lm);
                    _textInput.Text = "";
                }
            }
        }
    }

    private int _selectedPlayer;
    public void OnPlayerSelected (int value)
    {
        _selectedPlayer = value;
    }

    public void OnKickPressed ()
    {
        GD.Print(_selectedPlayer);
        GD.Print(connectedClients.Keys.ToArray().Length);
        connectedClients.Keys.ToArray()[_selectedPlayer - 1].Disconnect();
    }
    
    public void MessagePacketReceived (LobbyChatMessage message)
    {
        GD.Print("> Received message packet");
        AddLine(message);
    }

    private void AddLine (LobbyChatMessage message)
    {
        if (message.Sender.HighAuthority)
        {
            _chatBox.Text += message.Sender.Nickname + " (Host) : " + message.Message + "\n";
        }
        else
        {
            _chatBox.Text += message.Sender.Nickname + ": " + message.Message + "\n";
        }
    }
    
    private void GatherNodeReferences ()
    {
        _textInput = GetNode<LineEdit>("Panel/Input");
        _chatBox = GetNode<TextEdit>("Panel/Chatbox");

        _connectedCount = GetNode<Label>("Connected/Label");

        _connectedPlayersList = GetNode<ItemList>("Connected/Players");
    }
}
