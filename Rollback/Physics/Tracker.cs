using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Static class that track every AABB in the scene
/// </summary>
public class Tracker : Node
{
    public readonly List<AABB> pushboxes = new List<AABB>();
    
    public static Tracker singleton;
    
    public override void _Ready()
    {
        if (singleton == null)
        {
            singleton = this;
        }
    }

    public void Subscribe (AABB who)
    {
        switch (who.Type)
        {
            case ColliderType.Pushbox:
                pushboxes.Add(who);
                break;
            case ColliderType.Hitbox:

                break;
            case ColliderType.Hurtbox:

                break;
        }
    }
}
