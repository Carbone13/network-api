using Godot;

public class SceneCommunicator : Node
{
    public string Nickname { get; set;}

    public static SceneCommunicator singleton;

    public override void _Ready () => singleton = this;
}