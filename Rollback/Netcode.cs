using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using LiteNetLib;
using LiteNetLib.Utils;
using Network;
using Network.Packet;
using System.Threading;
using Thread = System.Threading.Thread;

// Represent a frame and the associated inputs
public class InputFrame
{
    public int Frame { get; set; }
    public Inputs LocalInputs { get; set; }
    public Inputs PredictedInputs { get; set; }
    public Inputs RemoteInputs { get; set; }
}

// Represent the state of the game at a specified frame
public class GameFrame
{
    public int Frame;

    public Vector2 localPosition;
    public Vector2 remotePosition;
}

public struct Inputs : INetSerializable
{ 
    public int AD { get; set; }
    public int WS { get; set; }
    
    public void Serialize (NetDataWriter writer)
    {
        writer.Put(AD);
        writer.Put(WS);
    }

    public void Deserialize (NetDataReader reader)
    {
        AD = reader.GetInt();
        WS = reader.GetInt();
    }

    public override string ToString ()
    {
        return "x: " + AD + " y: " + WS;
    }
}

public class InputsPacket
{
    public int Frame { get; set; }
    public int FrameAdvantage { get; set; }
    public Inputs Inputs { get; set; }
}


public class Netcode : Node
{
    public List<Player> Players = new List<Player>();
    
    public const int MAX_ROLLBACK_FRAMES = 64;
    public const int FRAME_ADVANTAGE_LIMIT = 64;                                                                           
    public const int INITIAL_FRAME = 0;

    [Export]private int localFrame = INITIAL_FRAME;
    [Export]private int remoteFrame = INITIAL_FRAME;
    [Export]private int syncFrame = INITIAL_FRAME;
    [Export]private int remoteFrameAdvantage = 0;
    [Export]private int onlineFrame = 0;

    private Inputs lastRemoteInput;
    private Inputs lastSentInputs;
    private Inputs localInputs;

    private LinkedList<InputFrame> RecordedInputs = new LinkedList<InputFrame>();
    private LinkedList<GameFrame> RecordedGameFrames = new LinkedList<GameFrame>();

    private NetManager socket;
    private NetPeer other;
    private NetPacketProcessor processor;

    [Export] private PackedScene PlayerPrefab;
    private bool connected;

    private LineEdit Address, Port;
    #region Networking
    public override void _Ready ()
    {
        EventBasedNetListener listener = new EventBasedNetListener();
        processor = new NetPacketProcessor();
        
        socket = new NetManager(listener);
        socket.AutoRecycle = true;
        socket.SimulateLatency = true;
        socket.SimulationMinLatency = 500;
        socket.SimulationMaxLatency = 800;
        socket.SimulatePacketLoss = true;

        listener.ConnectionRequestEvent += _request => _request.Accept();
        listener.PeerConnectedEvent += _peer =>
        {
            other = _peer;
            Node player = PlayerPrefab.Instance();
            GetTree().Root.AddChild(player);

            Players.Add(player as Player);
            Player p = player as Player;

            if (host)
            {
                p.Position = new Vector2(500, 0);
            }
            else
            {
                p.Position = new Vector2(300, 0);
            }
            connected = true;
            
            p._Ready();
            
            NewSaveState();
        };
        
        listener.NetworkReceiveEvent += (_peer, _reader, _method) => processor.ReadAllPackets(_reader, _peer);
        //listener.NetworkLatencyUpdateEvent += (_peer, _latency) => GD.Print("Latency: " + _latency + " ms");
        
        processor.RegisterNestedType<Inputs>();
        processor.SubscribeReusable<InputsPacket> (GetInput);

        Address = GetNode<LineEdit>("Address");
        Port = GetNode<LineEdit>("Port");
    }

    public bool host;
    
    public void Join ()
    {
        socket.Start();
        other = socket.Connect(Address.Text, 3456, "");
        
        Node player = PlayerPrefab.Instance();
        GetTree().Root.AddChild(player);
        Players.Add(player as Player);
        Player p = player as Player;
        p.Position = new Vector2(500, 0);
        p._Ready();
        p.local = true;
        
        p._Ready();
        
        DeleteUI();
    }

    public void Host ()
    {
        //UPNP upnp = new UPNP();
        //upnp.Discover();
        //upnp.AddPortMapping(3456);
        socket.Start(3456);
        
        Node player = PlayerPrefab.Instance();
        GetTree().Root.AddChild(player);
        
        Players.Add(player as Player);
        Player p = player as Player;
        p.Position = new Vector2(300, 0);
        p._Ready();
        p.local = true;
        host = true;
        
        p._Ready();
        
        DeleteUI();
    }

    private void DeleteUI ()
    {
        GetNode("Address").QueueFree();
        GetNode("Port").QueueFree();
        GetNode("Button").QueueFree();
        GetNode("Button2").QueueFree();
    }
    #endregion
    
    public override void _PhysicsProcess (float delta)
    {
        socket.PollEvents();
        if (!connected) return;
        
        remoteFrame = onlineFrame;
        
        UpdateSync();

        if (TimeSynced())
        {
            NormalUpdate();
        }
    }

    private void UpdateSync ()
    {
        DetermineSyncFrame();

        if (RollbackCondition())
        {
            ExecuteRollbacks();
        }
    }
    
    private void DetermineSyncFrame ()
    {
        int finalFrame = remoteFrame;
        if(remoteFrame > localFrame) 
        {
            finalFrame = localFrame;
        }

        InputFrame oic = null; // find the oldest incorrect input frame
 
        foreach(InputFrame inp in RecordedInputs) 
        {
            if(inp.Frame >= syncFrame + 1 && inp.Frame <= finalFrame) 
            {
                if(!inp.RemoteInputs.Equals(inp.PredictedInputs)) 
                {
                    if(oic == null) 
                    {
                        oic = inp;
                    }
                    else 
                    {
                        if(inp.Frame < oic.Frame) 
                        {
                            oic = inp;
                        }
                    }
                }
            }
        }
 
        if(oic != null) 
        {
            syncFrame = oic.Frame - 1;
        }
        else {
            syncFrame = finalFrame;
        }
    }

    private InputFrame GetInputFrame (int f)
    {
        foreach (InputFrame frame in RecordedInputs)
        {
            if (frame.Frame == f)
                return frame;
        }

        return null;
    }
    
    private bool RollbackCondition ()
    {
        return localFrame > syncFrame && remoteFrame > syncFrame;
    }
    
    private void ExecuteRollbacks ()
    {
        //GD.Print("Rolling back to sync time : " + syncFrame);
        LoadFrame(syncFrame);

        for (int i = syncFrame + 1; i <= localFrame; i++)
        {
            foreach (InputFrame input in RecordedInputs)
            {
                if (input.Frame == i)
                {
                    input.PredictedInputs = input.RemoteInputs;
                    
                    //GD.Print("Fixed frame " + input.Frame + " new inputs : " + input.PredictedInputs.ToString());
                    
                    UpdateInput(input);
                    UpdateGame(i);
                    OverwriteSaveState(i);
                }
            }
        }
    }

    private void NormalUpdate ()
    {
        localFrame++;

        localInputs = new Inputs();
        localInputs.AD = (int)Input.GetActionStrength("ui_right") - (int)Input.GetActionStrength("ui_left");
        localInputs.WS = (int)Input.GetActionStrength("ui_down") - (int)Input.GetActionStrength("ui_up");
        InputFrame lfi = GetLocalPlayerInput();

        SendInputToRemoteClients();
        
        UpdateInput(lfi);
        UpdateGame(localFrame);

        NewSaveState();
    }

    private void UpdateInput (InputFrame i)
    {
        foreach (Player p in Players)
        {
            if (p.local)
            {
                p.SimulateOneFrame(i.LocalInputs, i.Frame);
            }
            else
            {
                p.SimulateOneFrame(i.PredictedInputs, i.Frame);
            }
        }
    }

    private void UpdateGame (int f)
    {
        /*
        p1.sim_frame = fr;
        p2.sim_frame = fr;
        p1.frame_update(fr);
        p2.frame_update(fr);*/
    }

    private bool TimeSynced ()
    {
        int local_frame_advantage = localFrame - remoteFrame;
        int frame_advantage_difference = local_frame_advantage - remoteFrameAdvantage;
 
        return local_frame_advantage < MAX_ROLLBACK_FRAMES && frame_advantage_difference <= FRAME_ADVANTAGE_LIMIT;
    }
    InputFrame GetLocalPlayerInput ()
    {
        InputFrame inp = null;
        
        foreach (InputFrame i in RecordedInputs)
        {
            if (i.Frame == localFrame)
                inp = i;
        }

        if (inp == null)
        {
            inp = new InputFrame();
            inp.PredictedInputs = lastRemoteInput;
            inp.Frame = localFrame;

            RecordedInputs.AddLast(inp);
            if (RecordedInputs.Count > MAX_ROLLBACK_FRAMES)
            {
                RecordedInputs.RemoveFirst();
            }
        }

        inp.LocalInputs = localInputs;
        return inp;
    }

    private int sent;
    // TODO only send input when they change, need to refacto other things tho
    private void SendInputToRemoteClients ()
    {
        //GD.Print("Updating our inputs");
        sent++;
        //GD.Print("Sent " + sent + " packets");
        InputsPacket packet = new InputsPacket();
        packet.Inputs = localInputs;
        packet.Frame = localFrame;
        packet.FrameAdvantage = localFrame - remoteFrame;
        lastSentInputs = packet.Inputs;
    
        foreach (NetPeer peer in socket.ConnectedPeerList)
        {
            peer.Send(processor.Write(packet), DeliveryMethod.ReliableUnordered);
        }
        
    }
    private void LoadFrame (int f)
    {
        //GD.Print("Loading frame " + f + " ...");
        GameFrame frame = null;
        foreach (GameFrame storedFrame in RecordedGameFrames)
        {
            if (storedFrame.Frame == f)
            {
                frame = storedFrame;
            }
        }

        if (frame == null)
        {
            GD.Print("could not find frame !");
            return;
        }
        
        foreach (Player p in Players)
        {
            if (p.local)
            {
                p.GlobalPosition = frame.localPosition;
            }
            else
            {
                p.GlobalPosition = frame.remotePosition;
            }
        }
    }

    private bool debugSyncframe;
    
    private void NewSaveState ()
    {
        GameFrame snapshot = new GameFrame();
        snapshot.Frame = localFrame;
        foreach (Player p in Players)
        {
            if (p.local)
            {
                snapshot.localPosition = p.GlobalPosition;
            }
            else
            {
                snapshot.remotePosition = p.GlobalPosition;
            }
        }
        
        RecordedGameFrames.AddLast(snapshot);
        if (RecordedGameFrames.Count > MAX_ROLLBACK_FRAMES)
        {
            RecordedGameFrames.RemoveFirst();
        }
    }

    private void OverwriteSaveState (int f)
    {
        GameFrame frame = null;
        foreach (GameFrame storedFrame in RecordedGameFrames)
        {
            if (storedFrame.Frame == f)
            {
                frame = storedFrame;
            }
        }

        if (frame == null)
        {
            //GD.PrintErr("Could not find frame " + f);
            return;
        }
        foreach (Player p in Players)
        {
            if (p.local)
            {
                frame.localPosition = p.GlobalPosition;
            }
            else
            {
                frame.remotePosition = p.GlobalPosition;
            }
        }
    }

    private int received;
    private void GetInput (InputsPacket inp)
    {
        received++;
        //GD.Print("Received " + received + " packets");
        onlineFrame = inp.Frame;
        lastRemoteInput = inp.Inputs;
        remoteFrameAdvantage = inp.FrameAdvantage;
        
        InputFrame frame = GetInputFrame(inp.Frame);

        if (frame != null)
        {
            if (!frame.PredictedInputs.Equals(inp.Inputs))
            {
                //GD.PrintErr("Frame " + frame.Frame);
                debugSyncframe = true;
            }
            
            frame.RemoteInputs = inp.Inputs;
        }
        else
        {
            InputFrame i = new InputFrame();
            
            i.Frame = inp.Frame;
            i.RemoteInputs = inp.Inputs;
            i.PredictedInputs = inp.Inputs;
            
            RecordedInputs.AddLast(i);
            if (RecordedInputs.Count > MAX_ROLLBACK_FRAMES)
            {
                RecordedInputs.RemoveFirst();
            }
        }

    }
}
