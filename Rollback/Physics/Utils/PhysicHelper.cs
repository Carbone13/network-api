using FixMath.NET;
using Godot;

namespace Carbone13.Physic.Utils
{
    public struct VF64
    {
        public Fix64 X;
        public Fix64 Y;

        public VF64 (int _x, int _y)
        {
            X = (Fix64) _x;
            Y = (Fix64) _y;
        }
        
        public VF64 (Fix64 _x, Fix64 _y)
        {
            X = _x;
            Y = _y;
        }

        public VF64 Normalize ()
        {
            Fix64 length = Length();

            Fix64 _X = X / length;
            Fix64 _Y = Y / length;

            return new VF64(_X, _Y);
        }

        public Fix64 Length ()
        {
            return Fix64.Sqrt(X * X + Y * Y);
        }
        

        public static VF64 operator *(VF64 factorOne, Fix64 factorTwo)
        {
            return new VF64(factorOne.X * factorTwo, factorOne.Y * factorTwo);
        }

        public override string ToString () => "x: " + X + "; y: " + Y;
    }
}