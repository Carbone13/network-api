using Godot;
using Network;
using System.Collections.Generic;
using System.Net;
using LiteNetLib;
using Network.Packet;

// TODO Lobby join

public class MenuManager : Node
{
    public AcceptDialog popup;
    public LineEdit nickname, lobbyName, lobbyPassword;
    public ItemList lobbyList;

    public NetPeer toNat;

    private List<Lobby> lobbies = new List<Lobby>();

    public override void _Ready ()
    {
        GatherReferences();

        toNat = NetworkManager.singleton.Connect(new PeerAddress("127.0.0.1", 3456), "");
        
        NetworkManager.Processor.SubscribeReusable<Lobby>(ReceiveLobbyInfo);
    }


    public void TryLobbyCreation ()
    {
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

        Lobby hosted = new Lobby();
        hosted.HostName = nickname.Text;
        hosted.LobbyName = lobbyName.Text;
        hosted.HostPublicAddress = new IPEndPoint(IPAddress.Any, 0000);
        hosted.PlayerCount = 1;

        toNat.Send(NetworkManager.Processor.Write(hosted), DeliveryMethod.ReliableOrdered);
    }

    public void RefreshLobbyList ()
    {
        lobbyList.Clear();
        lobbies.Clear();
        
        RequestLobbyList request = new RequestLobbyList();

        toNat.Send(NetworkManager.Processor.Write(request), DeliveryMethod.ReliableOrdered);
    }

    public void ReceiveLobbyInfo (Lobby lobby)
    {
        lobbies.Add(lobby);
        lobbyList.AddItem(lobby.LobbyName + " - hosted by " + lobby.HostName);
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
