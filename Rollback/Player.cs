using Godot;
using System;

public class Player : Node2D
{
    public bool local;
    public int id;
    
    public void SimulateOneFrame (Inputs _inputs, int f)
    {
        Position = new Vector2(Position.x + _inputs.AD * 50 * 1f / 64f, Position.y + _inputs.WS * 50 * 1f / 64f);
        GetNode<Label>("Position").Text = Position.ToString();
        
        //GD.Print(f + _inputs.ToString());
        if (!local)
        {
            //GD.Print(f + " " + Position);
        }

        if (id == 2)
        {
            //GD.Print(f + " " + Position + " " + _inputs);
        }
        
    }
}
