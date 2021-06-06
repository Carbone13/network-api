using Godot;
using System;
using LiteNetLib;
using Network.Packet;

public class LobbyManager : Node
{
    private NetPeer _nat;
    
    public void Initialize (Lobby lobby, bool host, NetPeer nat)
    {
        GetTree().Root.RemoveChild(GetTree().Root.GetNode("Menu"));

        if (host) _nat = nat;
        
    }
}
