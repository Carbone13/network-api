using Godot;
using System;
using Carbone13.Physic;
using Carbone13.Physic.Utils;
using FixMath.NET;

public class Player : Actor
{
    public bool local;
    public int id;

    private Vector2 Velocity;
    public float MoveSpeed = 50f;
    public float JumpHeight = 50f;
    public float JumpApexTime = 0.3f;

    private float gravity;
    private float jumpVelocity;
    
    public VF64 velocity;
    [Export] public bool isGrounded;
    public float xInput;

    
    public override void _Ready ()
    {
        base._Ready();
        CalculatePhysicConstants();
    }
    
    private void OnCollideX (int dir)
    {
        GD.Print("x");
        ClearRemainderX();
        velocity.X = Fix64.Zero;
    }

    private void OnCollideY (int dir)
    {
        if (dir == 1)
        {
            isGrounded = true;
        }
        
        ClearRemainderY();
        velocity.Y = Fix64.Zero;
    }
    
    private void CalculatePhysicConstants ()
    {
        gravity = (2 * JumpHeight) / (float)(Math.Pow(JumpApexTime, 2));

        jumpVelocity = (float)Math.Sqrt(2 * gravity * JumpHeight);
    }
    
    public void SimulateOneFrame (Inputs _inputs, int f)
    {
        xInput = _inputs.AD;
        velocity.X = (Fix64)xInput * (Fix64)MoveSpeed;
        
        if (_inputs.WS == 1 && isGrounded)
        {
            isGrounded = false;
            velocity.Y = (Fix64)jumpVelocity;
        }
        
        if(!isGrounded)
        {
            velocity.Y += (Fix64)gravity * (Fix64.One / (Fix64)64);
        }

        Move(velocity * (Fix64.One / (Fix64)64), OnCollideX, OnCollideY);

        GetNode<Label>("Velocity").Text = velocity.ToString();
        GetNode<Label>("Position").Text = Position.ToString() + (local ? " (local" : " remot");

    }
}
