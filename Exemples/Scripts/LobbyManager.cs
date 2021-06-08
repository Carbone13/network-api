using Godot;
using System;
using System.Threading.Tasks;
using LiteNetLib;
using Network;
using Network.Packet;
using LiteNetLib.Utils;

public class LobbyManager : Node
{
    private NetPeer _nat;
    private LineEdit _address;
    private Button _host, _join, _leave;
    private LineEdit _textInput;
    private TextEdit _chatBox;

    private bool _isHost;
    private string _nickname;
    
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
        if (from != _nat) return;
        
        GD.Print("> Received connection order");
        GD.Print(" >> We must connect toward " + order.target);

        //NetworkManager.singleton.Connect(new PeerAddress(order.target.Address.ToString(), order.target.Port), "");
        TryConnect(order);
    }

    public async void TryConnect (ConnectTowardOrder order)
    {
        GD.Print("  >>> Starting connections request");
        int tryCount = 0;
        
        // This is only relevant on local, you can't connect if you have pending request
        // TODO cleaner way (like await NoPendingRequest;)
        if(_isHost)
            await Task.Delay(100);
        
        NetPeer con = NetworkManager.singleton.Connect(new PeerAddress(order.target.Address.ToString(), order.target.Port), "");
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
            con = NetworkManager.singleton.Connect(new PeerAddress(order.target.Address.ToString(), order.target.Port), "");
            
            tryCount++;
        }
        
        GD.Print("  >>> Connected to peer !");
        GD.Print("  >>> Confirming to peer that he is connected");
        LobbyConnectConfirmationFromHost conf = new LobbyConnectConfirmationFromHost();
        con.Send(NetworkManager.Processor.Write(conf), DeliveryMethod.ReliableOrdered);
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
