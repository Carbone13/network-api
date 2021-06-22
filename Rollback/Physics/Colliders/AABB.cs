using Godot;
using System;

[Tool]
public class AABB : Node2D
{
    [Export] private Rect2 Rect;
    [Export] public bool Collidable;
    [Export] public ColliderType Type;

    [Export] public bool Draw;
    [Export] public float Alpha;

    public Rect2 Bounds (Vector2 offset)
    {
        return new Rect2(offset + Rect.Position, Rect.Size);
    }
    
    public override void _Ready ()
    {
        Tracker.singleton.Subscribe(this);
    }


    /// <summary>
    /// Draw the collider in the scene
    /// </summary>
    public override void _Draw ()
    {
        if (!Draw) return;
        
        Color col = Colors.White;
        
        switch (Type)
        {
            case ColliderType.Pushbox:
                col = Colors.Aqua;
                break;
            case ColliderType.Hitbox:
                col = Colors.Red;
                break;
            case ColliderType.Hurtbox:
                col = Colors.Green;
                break;
        }
        
        
        // Draw outline
        DrawRect(Rect, col, false);
        // Reduce alpha
        col.a = Alpha;
        // Draw inside
        DrawRect(Rect, col);
    }

    public override void _Process (float delta)
    {
        if (Engine.EditorHint)
            Update();
    }
}

public enum ColliderType
{
    Pushbox,
    Hitbox,
    Hurtbox
}