using Godot;
using System;
using System.Threading.Tasks;
using LiteNetLib;
using Network;
using Network.Packet;
using LiteNetLib.Utils;
using System.Collections.Generic;

// TODO on lan we shoud also know our public address
// TODO on lan we shoud also know our public address
// TODO on lan we shoud also know our public address
// TODO on lan we shoud also know our public address
// TODO on lan we shoud also know our public address

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
        GD.Print("> Joined lobby");
        
        GetTree().Root.RemoveChild(GetTree().Root.GetNode("Menu"));

        if (host) _nat = nat;
        _isHost = host;
        _nickname = nickname;
        
        NetworkManager.Processor.SubscribeReusable<ConnectTowardOrder, NetPeer>(OnConnectOrderReceived);
        NetworkManager.Processor.SubscribeReusable<LobbyMessage>(MessagePacketReceived);
    }

    public override void _Ready ()
    {
        GatherNodeReferences();
    }

    public void OnConnectOrderReceived (ConnectTowardOrder order, NetPeer from)
    {
        //if (from != _nat) return;
        
        GD.Print("> Received connection order");
        GD.Print(" >> We must connect toward " + order.target);

        TreatOrder(order);
    }

    public async void TreatOrder (ConnectTowardOrder order)
    {
        NetPeer con = await TryConnect(order);
        if(con == null) return;

        connectedClients.Add(con, new EndpointCouple(order.target, con.EndPoint));

        LobbyConnectConfirmationFromHost conf = new LobbyConnectConfirmationFromHost();
        con.Send(NetworkManager.Processor.Write(conf), DeliveryMethod.ReliableOrdered);
        
        if(!_isHost) return;

        ConnectTowardOrder _order = new ConnectTowardOrder();
        _order.target = con.EndPoint;

        foreach(NetPeer alreadyConnect in NetworkManager.singleton.Socket.peers)
        {
            if(alreadyConnect != _nat && alreadyConnect != con)
            {
                alreadyConnect.Send(NetworkManager.Processor.Write(_order), DeliveryMethod.ReliableOrdered);

                ConnectTowardOrder _other = new ConnectTowardOrder();
                _other.target = alreadyConnect.EndPoint;

                con.Send(NetworkManager.Processor.Write(_other), DeliveryMethod.ReliableOrdered);
            }
        }
    }

    public async Task<NetPeer> TryConnect (ConnectTowardOrder order)
    {
        GD.Print("  >>> Starting connections request");
        int tryCount = 0;
        
        // This is only relevant on local, you can't connect if you have pending request
        // TODO cleaner way (like await NoPendingRequest;)
        if(_isHost)
            await Task.Delay(100);
        
        PeerAddress targetAddress = new PeerAddress();

        if(order.usePrivate)
        {
            targetAddress = new PeerAddress(order.privateTarget.Address.ToString(), order.privateTarget.Port);
        }
        else
        {
            targetAddress = new PeerAddress(order.target.Address.ToString(), order.target.Port);
        }

        NetPeer con = NetworkManager.singleton.Connect(targetAddress, "");
        await Task.Delay(800);
        
        while (con.ConnectionState != ConnectionState.Connected)
        {
            if (tryCount > 10)
            {
                GD.Print("  >>> Time out !");
                return;
            }
            
            GD.Print("  >>> Failed ! Retrying in 5 seconds");
            await Task.Delay(800);
            GD.Print("  >>> Retrying to connect...");
            
            con.Disconnect();
            con = NetworkManager.singleton.Connect(targetAddress, "");
            
            tryCount++;
        }
        
        GD.Print("  >>> Connected to peer !");
        GD.Print("  >>> Confirming to peer that he is connected");

        return con; 
    }

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
