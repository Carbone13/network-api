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
    
    public const int MAX_ROLLBACK_FRAMES = 10;
    public const int FRAME_ADVANTAGE_LIMIT = 3;                                                                           
    public const int INITIAL_FRAME = 0;

    [Export]private int localFrame = INITIAL_FRAME;
    [Export]private int remoteFrame = INITIAL_FRAME;
    [Export]private int syncFrame = INITIAL_FRAME;
    [Export]private int remoteFrameAdvantage = 0;
    [Export]private int onlineFrame = 0;

    private Inputs lastRemoteInput;
    private Inputs localInputs;

    private LinkedList<InputFrame> RecordedInputs = new LinkedList<InputFrame>();
    private LinkedList<GameFrame> RecordedGameFrames = new LinkedList<GameFrame>();

    private NetManager socket;
    private NetPeer other;
    private NetPacketProcessor processor;

    [Export] private PackedScene PlayerPrefab;
    private bool connected;
    
    #region Networking
    public override void _Ready ()
    {
        EventBasedNetListener listener = new EventBasedNetListener();
        processor = new NetPacketProcessor();
        
        socket = new NetManager(listener);

        listener.ConnectionRequestEvent += _request => _request.Accept();
        listener.PeerConnectedEvent += _peer =>
        {
            Node player = PlayerPrefab.Instance();
            GetTree().Root.AddChild(player);

            Players.Add(player as Player);
            connected = true;
        };
        
        listener.NetworkReceiveEvent += (_peer, _reader, _method) => processor.ReadAllPackets(_reader, _peer);
        
        processor.RegisterNestedType<Inputs>();
        processor.SubscribeReusable<InputsPacket> (GetInput);

        Thread netThread = new Thread(PollEvents);
        netThread.Start();
    }

    public void PollEvents ()
    {
        while (true)
        {
            socket.PollEvents();
            Thread.Sleep(15);
        }
    }

    public void Join1 ()
    {
        GD.Print("1");
        socket.Start(3456);
        other = socket.Connect("127.0.0.1", 3457, "");

        Node player = PlayerPrefab.Instance();
        GetTree().Root.AddChild(player);
        
        Players.Add(player as Player);
        Player p = player as Player;
        p.local = true;
    }

    public void Join2 ()
    {
        socket.Start(3457);
        other = socket.Connect("127.0.0.1", 3456, "");
        
        Node player = PlayerPrefab.Instance();
        GetTree().Root.AddChild(player);
        
        Players.Add(player as Player);
        Player p = player as Player;
        p.local = true;
    }
    #endregion
    
    public override void _PhysicsProcess (float delta)
    {
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

        syncFrame = finalFrame;
        for (int i = syncFrame + 1; i < finalFrame; i++)
        {
            InputFrame inputs = GetInputFrame(i);
            if (!inputs.PredictedInputs.Equals(inputs.LocalInputs))
            {
                syncFrame = i - 1;
            }
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
    
    // Rollback toward syncFrame
    private void ExecuteRollbacks ()
    {
        LoadFrame(syncFrame);

        for (int i = syncFrame + 1; i < localFrame; i++)
        {
            RollbackUpdate(i);
        }
    }

    private void RollbackUpdate (int f)
    {
        InputFrame frame = GetInputFrame(f);
        frame.PredictedInputs = frame.RemoteInputs;
        UpdateInput(frame);
        UpdateGame(f);
        OverwriteSaveState(f);
        
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
                p.SimulateOneFrame(i.LocalInputs);
            }
            else
            {
                p.SimulateOneFrame(i.PredictedInputs);
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
    private void SendInputToRemoteClients ()
    {
        InputsPacket packet = new InputsPacket();
        packet.Inputs = localInputs;
        packet.Frame = localFrame;
        packet.FrameAdvantage = localFrame - remoteFrame;
        //GD.Print("Sending inputs for frame " + packet.Frame);

        foreach (NetPeer peer in socket.ConnectedPeerList)
        {
            peer.Send(processor.Write(packet), DeliveryMethod.ReliableOrdered);
        }
    }
    private void LoadFrame (int f)
    {
        foreach (Player p in Players)
        {
            if (p.local)
            {
                foreach (GameFrame frame in RecordedGameFrames.Where(frame => frame.Frame == f))
                {
                    p.GlobalPosition = frame.localPosition;
                }
            }
            else
            {
                foreach (GameFrame frame in RecordedGameFrames.Where(frame => frame.Frame == f))
                {
                    p.GlobalPosition = frame.remotePosition;
                }
            }
        }
    }
    private void NewSaveState ()
    {
        GameFrame snapshot = new GameFrame();

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
        foreach (Player p in Players)
        {
            if (p.local)
            {
                foreach (GameFrame frame in RecordedGameFrames.Where(frame => frame.Frame == f))
                {
                    frame.localPosition = p.GlobalPosition;
                }
            }
            else
            {
                foreach (GameFrame frame in RecordedGameFrames.Where(frame => frame.Frame == f))
                {
                    frame.remotePosition = p.GlobalPosition;
                }
            }
        }
    }
    
    private void GetInput (InputsPacket inp)
    {
        //GD.Print("Received inputs for frame " + inp.Frame);
        onlineFrame = inp.Frame;
        lastRemoteInput = inp.Inputs;
        remoteFrameAdvantage = inp.FrameAdvantage;
        
        InputFrame frame = GetInputFrame(inp.Frame);

        if (frame != null)
        {
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
