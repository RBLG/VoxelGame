using System;
using System.Numerics;

namespace VoxelGame.scripts.common;


public struct Vector2T<TYPE> {
    public TYPE X, Y;
    public Vector2T(TYPE xyz) : this(xyz, xyz) { }
    public Vector2T(TYPE x, TYPE y) {
        X = x;
        Y = y;
    }
}

public struct Vector3T<TYPE> where TYPE : INumber<TYPE> {
    public TYPE X, Y, Z;



    public Vector3T(TYPE xyz) : this(xyz, xyz, xyz) { }
    public Vector3T(TYPE x, TYPE y, TYPE z) {
        X = x;
        Y = y;
        Z = z;
    }
    public Vector3T() {
        X = TYPE.Zero;
        Y = TYPE.Zero;
        Z = TYPE.Zero;
    }

    public static bool operator ==(Vector3T<TYPE> l, Vector3T<TYPE> r) => l.X == r.X && l.Y == r.Y && l.Z == r.Z;
    public static bool operator ==(Vector3T<TYPE> l, TYPE r) => l.X == r && l.Y == r && l.Z == r;
    public static bool operator ==(Vector3T<TYPE> l, (TYPE X, TYPE Y, TYPE Z) r) => l.X == r.X && l.Y == r.Y && l.Z == r.Z;
    public static bool operator !=(Vector3T<TYPE> l, Vector3T<TYPE> r) => !(l == r);
    public static bool operator !=(Vector3T<TYPE> l, TYPE r) => !(l == r);
    public static bool operator !=(Vector3T<TYPE> l, (TYPE X, TYPE Y, TYPE Z) r) => !(l == r);
    public static bool operator <(Vector3T<TYPE> l, Vector3T<TYPE> r) => l.X < r.X && l.Y < r.Y && l.Z < r.Z;
    public static bool operator >(Vector3T<TYPE> l, Vector3T<TYPE> r) => l.X > r.X && l.Y > r.Y && l.Z > r.Z;
    public static bool operator <=(Vector3T<TYPE> l, Vector3T<TYPE> r) => l.X <= r.X && l.Y <= r.Y && l.Z <= r.Z;
    public static bool operator >=(Vector3T<TYPE> l, Vector3T<TYPE> r) => l.X >= r.X && l.Y >= r.Y && l.Z >= r.Z;
    public static bool operator <(TYPE l, Vector3T<TYPE> r) => l < r.X && l < r.Y && l < r.Z;
    public static bool operator >(TYPE l, Vector3T<TYPE> r) => l > r.X && l > r.Y && l > r.Z;
    public static bool operator <=(TYPE l, Vector3T<TYPE> r) => l <= r.X && l <= r.Y && l <= r.Z;
    public static bool operator >=(TYPE l, Vector3T<TYPE> r) => l >= r.X && l >= r.Y && l >= r.Z;
    public static bool operator <(Vector3T<TYPE> l, TYPE r) => l.X < r && l.Y < r && l.Z < r;
    public static bool operator >(Vector3T<TYPE> l, TYPE r) => l.X > r && l.Y > r && l.Z > r;
    public static bool operator <=(Vector3T<TYPE> l, TYPE r) => l.X <= r && l.Y <= r && l.Z <= r;
    public static bool operator >=(Vector3T<TYPE> l, TYPE r) => l.X >= r && l.Y >= r && l.Z >= r;
    public static Vector3T<TYPE> operator +(Vector3T<TYPE> l, Vector3T<TYPE> r) => new(l.X + r.X, l.Y + r.Y, l.Z + r.Z);
    public static Vector3T<TYPE> operator -(Vector3T<TYPE> l, Vector3T<TYPE> r) => new(l.X - r.X, l.Y - r.Y, l.Z - r.Z);
    public static Vector3T<TYPE> operator *(Vector3T<TYPE> l, Vector3T<TYPE> r) => new(l.X * r.X, l.Y * r.Y, l.Z * r.Z);
    public static Vector3T<TYPE> operator /(Vector3T<TYPE> l, Vector3T<TYPE> r) => new(l.X / r.X, l.Y / r.Y, l.Z / r.Z);
    public static Vector3T<TYPE> operator %(Vector3T<TYPE> l, Vector3T<TYPE> r) => new(l.X % r.X, l.Y % r.Y, l.Z % r.Z);
    public static Vector3T<TYPE> operator +(Vector3T<TYPE> l, TYPE r) => new(l.X + r, l.Y + r, l.Z + r);
    public static Vector3T<TYPE> operator -(Vector3T<TYPE> l, TYPE r) => new(l.X - r, l.Y - r, l.Z - r);
    public static Vector3T<TYPE> operator *(Vector3T<TYPE> l, TYPE r) => new(l.X * r, l.Y * r, l.Z * r);
    public static Vector3T<TYPE> operator /(Vector3T<TYPE> l, TYPE r) => new(l.X / r, l.Y / r, l.Z / r);
    public static Vector3T<TYPE> operator %(Vector3T<TYPE> l, TYPE r) => new(l.X % r, l.Y % r, l.Z % r);
    public static Vector3T<TYPE> operator +(TYPE l, Vector3T<TYPE> r) => new(l + r.X, l + r.Y, l + r.Z);
    public static Vector3T<TYPE> operator -(TYPE l, Vector3T<TYPE> r) => new(l - r.X, l - r.Y, l - r.Z);
    public static Vector3T<TYPE> operator *(TYPE l, Vector3T<TYPE> r) => new(l * r.X, l * r.Y, l * r.Z);
    public static Vector3T<TYPE> operator /(TYPE l, Vector3T<TYPE> r) => new(l / r.X, l / r.Y, l / r.Z);
    public static Vector3T<TYPE> operator -(Vector3T<TYPE> l) => new(-l.X, -l.Y, -l.Z);
    public static Vector3T<TYPE> operator +(Vector3T<TYPE> l, (TYPE X, TYPE Y, TYPE Z) r) => new(l.X + r.X, l.Y + r.Y, l.Z + r.Z);
    public static Vector3T<TYPE> operator -(Vector3T<TYPE> l, (TYPE X, TYPE Y, TYPE Z) r) => new(l.X - r.X, l.Y - r.Y, l.Z - r.Z);
    public static Vector3T<TYPE> operator *(Vector3T<TYPE> l, (TYPE X, TYPE Y, TYPE Z) r) => new(l.X * r.X, l.Y * r.Y, l.Z * r.Z);

    public override readonly bool Equals(object? obj) => base.Equals(obj);

    public override readonly int GetHashCode() => base.GetHashCode();

    public readonly Vector3T<TYPE2> Do<TYPE2>(Func<TYPE, TYPE2> func) where TYPE2 : INumber<TYPE2> {
        return new(func(X), func(Y), func(Z));
    }
    public readonly Vector3T<TYPE2> Do<TYPE2, TYPE3>(Vector3T<TYPE3> r, Func<TYPE, TYPE3, TYPE2> func)
        where TYPE2 : INumber<TYPE2>
        where TYPE3 : INumber<TYPE3> {
        return new(func(X, r.X), func(Y, r.Y), func(Z, r.Z));
    }
    public readonly Vector3T<TYPE2> Do<TYPE2, TYPE3, TYPE4>(Vector3T<TYPE3> r, Vector3T<TYPE4> r2, Func<TYPE, TYPE3, TYPE4, TYPE2> func)
        where TYPE2 : INumber<TYPE2>
        where TYPE3 : INumber<TYPE3>
        where TYPE4 : INumber<TYPE4> {
        return new(func(X, r.X, r2.X), func(Y, r.Y, r2.Y), func(Z, r.Z, r2.Z));
    }

    public readonly (bool x, bool y, bool z) Do(Func<TYPE, bool> func) {
        return new(func(X), func(Y), func(Z));
    }
    public readonly (bool x, bool y, bool z) Do<TYPE3>(Vector3T<TYPE3> r, Func<TYPE, TYPE3, bool> func)
        where TYPE3 : INumber<TYPE3> {
        return new(func(X, r.X), func(Y, r.Y), func(Z, r.Z));
    }
    public readonly (bool x, bool y, bool z) Do<TYPE3, TYPE4>(Vector3T<TYPE3> r, Vector3T<TYPE4> r2, Func<TYPE, TYPE3, TYPE4, bool> func)
        where TYPE3 : INumber<TYPE3>
        where TYPE4 : INumber<TYPE4> {
        return new(func(X, r.X, r2.X), func(Y, r.Y, r2.Y), func(Z, r.Z, r2.Z));
    }

    public readonly bool Any<TYPE3>(Vector3T<TYPE3> r, Func<TYPE, TYPE3, bool> func)
        where TYPE3 : INumber<TYPE3> {
        return func(X, r.X) || func(Y, r.Y) || func(Z, r.Z);
    }
    public readonly bool All<TYPE3>(Vector3T<TYPE3> r, Func<TYPE, TYPE3, bool> func)
        where TYPE3 : INumber<TYPE3> {
        return func(X, r.X) && func(Y, r.Y) && func(Z, r.Z);
    }

    public readonly TYPE Max() => GMath.Max(X, Y, Z);
    public readonly TYPE Min() => GMath.Min(X, Y, Z);

    public readonly Vector3T<TYPE> Max(Vector3T<TYPE> r) => new(TYPE.Max(X, r.X), TYPE.Max(Y, r.Y), TYPE.Max(Z, r.Z));
    public readonly Vector3T<TYPE> Min(Vector3T<TYPE> r) => new(TYPE.Min(X, r.X), TYPE.Min(Y, r.Y), TYPE.Min(Z, r.Z));

    public readonly Vector3T<TYPE> Clamp(Vector3T<TYPE> min, Vector3T<TYPE> max) => Min(max).Max(min);

    public readonly TYPE Pick(Vector3T<TYPE> picker) => (picker.X != TYPE.Zero) ? X : (picker.Y != TYPE.Zero) ? Y : Z;

    public readonly Vector3T<TYPE> Abs() => new(TYPE.Abs(X), TYPE.Abs(Y), TYPE.Abs(Z));
    public readonly TYPE Sum() => X + Y + Z;
    public readonly TYPE Product() => X * Y * Z;

    public readonly Vector3T<TYPE> Modulo(Vector3T<TYPE> by) => new(GMath.Modulo(X, by.X), GMath.Modulo(Y, by.Y), GMath.Modulo(Z, by.Z));

    public readonly Vector3T<TYPE> DistanceTo(Vector3T<TYPE> target) => (this - target).Abs();
    public readonly Vector3T<TYPE> Square() => this * this;
    public readonly Vector3T<int> Sign() => new(TYPE.Sign(X), TYPE.Sign(X), TYPE.Sign(X));

    public readonly TYPE LengthSquared() => (this * this).Sum();

    public readonly double Length => Math.Sqrt(Convert.ToDouble(LengthSquared()));

    public readonly Godot.Vector3 ToVector3() => new(Convert.ToSingle(X), Convert.ToSingle(Y), Convert.ToSingle(Z));

    public override readonly string ToString() => $"Vec3<{typeof(TYPE).Name}>({X},{Y},{Z})";
    public readonly string ToShortString() => $"({X},{Y},{Z})";

    public readonly Vector3T<TYPE> Normalized() {
        if (X == TYPE.Zero && Y == TYPE.Zero && Z == TYPE.Zero) {
            return this;
        }
        dynamic lenInv = 1 / Length;
        TYPE x = (TYPE)(X * lenInv);
        TYPE y = (TYPE)(Y * lenInv);
        TYPE z = (TYPE)(Z * lenInv);
        return new(x, y, z);
    }
}

public static class GMath {
    //public static NUM Min<NUM>(NUM arg1, NUM arg2) where NUM : INumber<NUM> => (arg1 < arg2) ? arg1 : arg2;
    //public static NUM Max<NUM>(NUM arg1, NUM arg2) where NUM : INumber<NUM> => (arg1 < arg2) ? arg2 : arg1;

    public static NUM Min<NUM>(NUM arg1, NUM arg2, NUM arg3) where NUM : INumber<NUM> {
        NUM val = NUM.Min(arg1, arg2);
        return NUM.Min(val, arg3);
    }
    public static NUM Max<NUM>(NUM arg1, NUM arg2, NUM arg3) where NUM : INumber<NUM> {
        NUM val = NUM.Max(arg1, arg2);
        return NUM.Max(val, arg3);
    }

    public static NUM Modulo<NUM>(NUM arg1, NUM arg2) where NUM : INumber<NUM> {
        NUM mod = arg1 % arg2;
        if (mod < NUM.Zero) { mod += NUM.One * arg2; }
        return mod;
    }

    public static Vector3T<uint> ToUint(this Vector3T<int> vec) => vec.Do((v) => (uint)v);
    public static Vector3T<int> ToInt(this Vector3T<uint> vec) => new((int)vec.X, (int)vec.Y, (int)vec.Z);
    public static Vector3T<int> ToInt(this Vector3T<float> vec) => new((int)vec.X, (int)vec.Y, (int)vec.Z);
    public static Vector3T<float> ToFloat(this Vector3T<int> vec) => new(vec.X, vec.Y, vec.Z);
    public static Vector3T<Half> ToHalf(this Vector3T<int> vec) => new((Half)vec.X, (Half)vec.Y, (Half)vec.Z);
    public static Vector3T<TYPE> Not<TYPE>(this Vector3T<TYPE> vec) where TYPE : IBinaryNumber<TYPE> => new(~vec.X, ~vec.Y, ~vec.Z);
    public static Vector3T<TYPE> And<TYPE>(this Vector3T<TYPE> vec, Vector3T<TYPE> mask) where TYPE : IBinaryNumber<TYPE>
        => new(vec.X & mask.X, vec.Y & mask.Y, vec.Z & mask.Z);
    public static Vector3T<TYPE> Or<TYPE>(this Vector3T<TYPE> vec, Vector3T<TYPE> mask) where TYPE : IBinaryNumber<TYPE>
        => new(vec.X | mask.X, vec.Y | mask.Y, vec.Z | mask.Z);
    public static Vector3T<TYPE> And<TYPE>(this Vector3T<TYPE> vec, TYPE mask) where TYPE : IBinaryNumber<TYPE>
        => new(vec.X & mask, vec.Y & mask, vec.Z & mask);
    public static Vector3T<TYPE> Or<TYPE>(this Vector3T<TYPE> vec, TYPE mask) where TYPE : IBinaryNumber<TYPE>
        => new(vec.X | mask, vec.Y | mask, vec.Z | mask);
    public static Vector3T<TYPE> LogicalRightShift<TYPE>(this Vector3T<TYPE> vec, Vector3T<int> shift) where TYPE : INumber<TYPE>, IShiftOperators<TYPE, int, TYPE>
        => new(vec.X >>> shift.X, vec.Y >>> shift.Y, vec.Z >>> shift.Z);
    public static Vector3T<TYPE> LogicalRightShift<TYPE>(this Vector3T<TYPE> vec, int shift) where TYPE : INumber<TYPE>, IShiftOperators<TYPE, int, TYPE>
        => new(vec.X >>> shift, vec.Y >>> shift, vec.Z >>> shift);
    public static Vector3T<TYPE> ArithmRightShift<TYPE>(this Vector3T<TYPE> vec, Vector3T<int> shift) where TYPE : INumber<TYPE>, IShiftOperators<TYPE, int, TYPE>
        => new(vec.X >> shift.X, vec.Y >> shift.Y, vec.Z >> shift.Z);
    public static Vector3T<TYPE> ArithmRightShift<TYPE>(this Vector3T<TYPE> vec, int shift) where TYPE : INumber<TYPE>, IShiftOperators<TYPE, int, TYPE>
        => new(vec.X >> shift, vec.Y >> shift, vec.Z >> shift);
    public static Vector3T<TYPE> LeftShift<TYPE>(this Vector3T<TYPE> vec, Vector3T<int> shift) where TYPE : INumber<TYPE>, IShiftOperators<TYPE, int, TYPE>
        => new(vec.X << shift.X, vec.Y << shift.Y, vec.Z << shift.Z);
    public static Vector3T<TYPE> LeftShift<TYPE>(this Vector3T<TYPE> vec, int shift) where TYPE : INumber<TYPE>, IShiftOperators<TYPE, int, TYPE>
        => new(vec.X << shift, vec.Y << shift, vec.Z << shift);

    public static Vector3T<float> Normalized2(this Vector3T<float> vec) {
        if (vec.X == 0 && vec.Y == 0 && vec.Z == 0) {
            return vec;
        }
        float lenInv = 1 / (float)vec.Length;
        return vec * lenInv;
    }
}
