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
    private NetPeer _nat;
    
    private LineEdit _textInput;
    private TextEdit _chatBox;
    public Label _connectedCount;
    public ItemList _connectedPlayersList;

    private bool _isHost;

    public readonly Dictionary<NetPeer, NetworkPeer> connectedClients = new Dictionary<NetPeer, NetworkPeer>();

    private Lobby _lobby;

    public void Initialize (Lobby lobby, bool host, NetPeer nat, string nickname)
    {
        GD.Print("> Joined lobby");
        
        GatherNodeReferences();
        GetTree().Root.CallDeferred("remove_child", GetTree().Root.GetNode("Menu"));

        if (host) _nat = nat;
        _isHost = host;
        _lobby = lobby;

        NetworkManager.Processor.SubscribeReusable<LobbyChatMessage>(MessagePacketReceived);
        NetworkManager.Processor.SubscribeReusable<RegisterAndUpdateLobbyState, NetPeer>(OnLobbyUpdate);

        NetworkManager.singleton.OnHolePunchSuccess += OnPeerJoin;
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
        if(peer.EndPoint.ToString() == _lobby.Host.Endpoints.Public.ToString() ||
                peer.EndPoint.ToString() == _lobby.Host.Endpoints.Private.ToString())
        {
            GD.Print("Refused from lobby/Kicked");

            Node menuScene = ResourceLoader.Load<PackedScene>("res://Exemples/Scenes/Menu.tscn").Instance();
            GetTree().Root.CallDeferred("add_child", menuScene);
            MenuManager manager = menuScene as MenuManager;

            GetTree().Root.RemoveChild(GetTree().Root.GetNode("Lobby"));
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
                update.Send(_nat, DeliveryMethod.ReliableOrdered);

                foreach(NetPeer _peer in connectedClients.Keys)
                    update.Send(_peer, DeliveryMethod.ReliableOrdered);
            }
        }

    }

    // Network/Lobby management
    public void OnPeerJoin (NetPeer peer, NetworkPeer peerInfo, HolePunchAddress initialOrder)
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

        Color col = _chatBox.GetColor("font_color");
        
        _chatBox.AddColorOverride("font_color", Colors.Aqua);
        _chatBox.Text += "New player joined: " + peerInfo.Nickname + "\n";
        _connectedPlayersList.AddItem(peerInfo.Nickname);
        
        connectedClients.Add(peer, peerInfo);

        HolePunchAddress _connectToNewPeer = new HolePunchAddress(NetworkManager.singleton.Us, connectedClients[peer], false);

        foreach(NetPeer alreadyConnected in connectedClients.Keys)
        {
            if(alreadyConnected != peer)
            {
                // Trade addresses !
                HolePunchAddress _connectToPresentPeer = new HolePunchAddress(NetworkManager.singleton.Us, connectedClients[alreadyConnected], false);

                bool usePrivate = 
                    _connectToNewPeer.Target.Endpoints.Public.Address.ToString() == _connectToPresentPeer.Target.Endpoints.Public.Address.ToString();
                _connectToNewPeer.UsePrivate = usePrivate;
                _connectToPresentPeer.UsePrivate = usePrivate;


                alreadyConnected.Send(NetworkManager.Processor.Write(_connectToNewPeer), DeliveryMethod.ReliableOrdered);
                peer.Send(NetworkManager.Processor.Write(_connectToPresentPeer), DeliveryMethod.ReliableOrdered);
            }
        }

        // Update our lobby statut
        _lobby.ConnectedPeers.Add(connectedClients[peer]);
        
        RegisterAndUpdateLobbyState update = new RegisterAndUpdateLobbyState(NetworkManager.singleton.Us, _lobby);
        update.Send(_nat, DeliveryMethod.ReliableOrdered);

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
