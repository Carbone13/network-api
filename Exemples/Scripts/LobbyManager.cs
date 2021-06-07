using Godot;
using System;
using System.Threading.Tasks;
using LiteNetLib;
using Network;
using Network.Packet;

public class LobbyManager : Node
{
    private NetPeer _nat;

    private bool _isHost;
    
    public void Initialize (Lobby lobby, bool host, NetPeer nat)
    {
        GD.Print("> Joined lobby");
        
        GetTree().Root.RemoveChild(GetTree().Root.GetNode("Menu"));

        if (host) _nat = nat;
        _isHost = host;
        
        NetworkManager.Processor.SubscribeReusable<ConnectTowardOrder, NetPeer>(OnConnectOrderReceived);
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
            await Task.Delay(10);
        
        NetPeer con = NetworkManager.singleton.Connect(new PeerAddress(order.target.Address.ToString(), order.target.Port), "");
        await Task.Delay(5000);
        
        while (con.ConnectionState != ConnectionState.Connected)
        {
            if (tryCount > 10)
            {
                GD.Print("  >>> Time out !");
                return;
            }
            
            GD.Print("  >>> Failed ! Retrying in 5 seconds");
            await Task.Delay(5000);
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
}
