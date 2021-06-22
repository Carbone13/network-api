using Godot;
using System;

public class Player : Node2D
{
    public bool local;
    
    public void SimulateOneFrame (Inputs _inputs)
    {
        if (local)
        {
            GD.Print("Simulating remote inputs with " + _inputs.AD + " , " + _inputs.WS);
        };
        Position = new Vector2(Position.x + _inputs.AD * 50 * 1f / 64f, Position.y + _inputs.WS * 50 * 1f / 64f);
        GetNode<Label>("Position").Text = Position.ToString();
    }
}
