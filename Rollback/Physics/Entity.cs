using System.Collections.Generic;
using System.Linq;
using Godot;
using FixMath.NET;
using Carbone13.Physic.Utils;


namespace Carbone13.Physic
{
    public class Entity : Node2D
    {
        // Editor variables
        [Export] public List<NodePath> Colliders;
        
        // Represent our positions
        public FixedPosition Fixed;
        public FloatingPosition Floating;

        private Dictionary<ColliderType, List<AABB>> _colliders = new Dictionary<ColliderType, List<AABB>>();

        public override void _Ready ()
        {
            Floating.X = (Fix64)GlobalPosition.x;
            Floating.Y = (Fix64)GlobalPosition.y;

            Fixed.X = (int) GlobalPosition.x;
            Fixed.Y = (int) GlobalPosition.y;
            
            SortColliders();
            
            GD.Print("rdy");
        }

        private void SortColliders ()
        {
            // Initialize the lists
            _colliders[ColliderType.Pushbox] = new List<AABB>();
            _colliders[ColliderType.Hitbox] = new List<AABB>();
            _colliders[ColliderType.Hurtbox] = new List<AABB>();
            
            foreach (AABB _collider in Colliders.Select(GetNode<AABB>))
            {
                _colliders[_collider.Type].Add(_collider);
            }
        }

        public bool CollideAt (Vector2 offset)
        {
            foreach (AABB collider in Tracker.singleton.pushboxes)
            {
                if (!_colliders[ColliderType.Pushbox].Contains(collider))
                {
                    foreach (AABB ourCollider in _colliders[ColliderType.Pushbox])
                    {
                        Rect2 ourRect = ourCollider.Bounds(ourCollider.GlobalPosition + offset);
                        Rect2 otherRect = collider.Bounds(collider.GlobalPosition);

                        if (ourRect.Intersects(otherRect))
                        {
                            return true;
                        }
                    }
                }
            }                                                                                                                                                                                                                                   
            return false;
        }
    }

    public struct FixedPosition
    {
        public int X;
        public int Y;

        public FixedPosition (int _x, int _y)
        {
            X = _x;
            Y = _y;
        }
    }
    
    public struct FloatingPosition
    {
        public Fix64 X;
        public Fix64 Y;
        
        
    }
}