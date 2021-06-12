using Godot;
using System;
using System.Threading.Tasks;
using LiteNetLib;
using Network;
using Network.Packet;
using LiteNetLib.Utils;
using System.Collections.Generic;

public class LobbyManager : Node
{
    private NetPeer _nat;
    private LineEdit _address;
    private Button _host, _join, _leave;
    private LineEdit _textInput;
    private TextEdit _chatBox;

    private bool _isHost;
    private string _nickname;

    public Dictionary<NetPeer, EndpointCouple> connectedClients = new Dictionary<NetPeer, EndpointCouple>();
    
    public void Initialize (Lobby lobby, bool host, NetPeer nat, string nickname)
    {
        NetworkManager.singleton.lanHost = host;
        GD.Print("> Joined lobby");
        
        GatherNodeReferences();
        GetTree().Root.RemoveChild(GetTree().Root.GetNode("Menu"));

        if (host) _nat = nat;
        _isHost = host;
        _nickname = nickname;

        NetworkManager.Processor.SubscribeReusable<LobbyMessage>(MessagePacketReceived);

        NetworkManager.singleton.OnConnectionOrderTreated += OnPeerJoin;
    }

    // Network/Lobby management
    public void OnPeerJoin (NetPeer newPeer, ConnectTowardOrder initialOrder)
    {
        if(newPeer == null) return;
        if(!_isHost) return;

        connectedClients.Add(newPeer, new EndpointCouple(initialOrder.addresses.Public, initialOrder.addresses.Private));

        LobbyConnectConfirmationFromHost conf = new LobbyConnectConfirmationFromHost();
        newPeer.Send(NetworkManager.Processor.Write(conf), DeliveryMethod.ReliableOrdered);
        
    
        ConnectTowardOrder _connectToNewPeer = new ConnectTowardOrder(connectedClients[newPeer]);

        foreach(NetPeer alreadyConnected in connectedClients.Keys)
        {
            if(alreadyConnected != newPeer)
            {
                // Trade addressed !
                ConnectTowardOrder _connectToPresentPeer = new ConnectTowardOrder(connectedClients[alreadyConnected]);

                bool usePrivate = 
                    _connectToNewPeer.addresses.Private.Address.ToString() == _connectToPresentPeer.addresses.Private.Address.ToString();
                _connectToNewPeer.usePrivate = usePrivate;
                _connectToPresentPeer.usePrivate = usePrivate;

                alreadyConnected.Send(NetworkManager.Processor.Write(_connectToNewPeer), DeliveryMethod.ReliableOrdered);
                newPeer.Send(NetworkManager.Processor.Write(_connectToPresentPeer), DeliveryMethod.ReliableOrdered);
            }
        }
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
                    GD.Print("Sending message to every peer...");
                    
                    NetDataWriter writer = new NetDataWriter();
                    
                    LobbyMessage lm = new LobbyMessage()
                    {
                        header =  _nickname + ": ",
                        message = _textInput.Text,
                    };
                    
                    writer.Reset();
                    NetworkManager.Processor.Write(writer, lm);
                    NetworkManager.singleton.Socket.BroadcastToPeers(writer, DeliveryMethod.ReliableOrdered);

                    AddLine(lm);
                    _textInput.Text = "";
                }
            }
        }
    }
    
    public void MessagePacketReceived (LobbyMessage message)
    {
        GD.Print("> Received message packet");
        AddLine(message);
    }

    private void AddLine (LobbyMessage message)
    {
        _chatBox.Text += message.header + message.message + "\n";
    }
    
    private void GatherNodeReferences ()
    {
        _leave = GetNode<Button>("Leave Button");
        
        _textInput = GetNode<LineEdit>("Panel/Input");
        _chatBox = GetNode<TextEdit>("Panel/Chatbox");
    }
}
