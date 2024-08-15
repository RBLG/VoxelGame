using Godot;
using System;
using System.Collections.Generic;
using System.Numerics;
using VoxelGame.scripts.content;

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

    public static bool operator ==(Vector3T<TYPE> l, Vector3T<TYPE> r) => l.X == r.X && l.Y == r.Y && l.Z == r.Z;
    public static bool operator ==(Vector3T<TYPE> l, TYPE r) => l.X == r && l.Y == r && l.Z == r;
    public static bool operator !=(Vector3T<TYPE> l, Vector3T<TYPE> r) => !(l == r);
    public static bool operator !=(Vector3T<TYPE> l, TYPE r) => !(l == r);
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
}
