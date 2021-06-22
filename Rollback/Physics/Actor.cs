using System;
using FixMath.NET;
using Godot;
using Carbone13.Physic.Utils;

namespace Carbone13.Physic
{
    public class Actor : Entity
    {
        protected void Move (VF64 amount, Action<int> collideX, Action<int> collideY)
        {
            MoveX(amount.X, collideX);
            MoveY(amount.Y, collideY);
        }

        private void MoveX (Fix64 amount, Action<int> collideX)
        {
            Floating.X += amount;
            int _rounded = (int) Fix64.Round(Floating.X);
            int move = _rounded - Fixed.X;
            
            if (move != 0)
            {
                int sign = Math.Sign(move);

                while (move != 0)
                {
                    if (CollideAt(new Vector2(sign, 0)))
                    {
                        Floating.X -= amount;
                        collideX.Invoke(sign);
                        break;
                    }
                    else
                    {
                        move -= sign;
                        Fixed.X += sign;
                        GlobalPosition = new Vector2(Fixed.X, GlobalPosition.y);
                    }
                }
            }
        }

        private void MoveY (Fix64 amount, Action<int> collideY)
        {
            Floating.Y += amount;
            int _rounded = (int) Fix64.Round(Floating.Y);
            int move = _rounded - Fixed.Y;

            if (move != 0)
            {
                int sign = Math.Sign(move);

                while (move != 0)
                {
                    if (CollideAt(new Vector2(0, sign)))
                    {
                        Floating.Y -= amount;
                        collideY.Invoke(sign);
                        break;
                    }
                    else
                    {
                        move -= sign;
                        Fixed.Y += sign;
                        GlobalPosition = new Vector2(GlobalPosition.x, Fixed.Y);
                    }
                }
            }

        }

        protected void ClearRemainderX ()
        {
            Floating.X = (Fix64)Fixed.X;
        }

        protected void ClearRemainderY ()
        {
            Floating.Y = (Fix64) Fixed.Y;
        }
    }
}