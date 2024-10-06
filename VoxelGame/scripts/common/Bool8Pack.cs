﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace VoxelGame.scripts.common;
public class Bool8Pack {
    private byte data = 0;

    public Bool8Pack() { }

    public bool this[int index] {
        get => (data & (1 << index)) != 0;
        set {
            if (value) { data |= (byte)(1 << index); } else { data &= (byte)~(1 << index); }
        }
    }

    public int Get(int index) => (data & (1 << index)) >> index;

    public void Set(bool b1 = false, bool b2 = false, bool b3 = false, bool b4 = false, bool b5 = false, bool b6 = false, bool b7 = false, bool b8 = false) {
        data = (byte)(
            (b1 ? 1u : 0u) |
            (b2 ? 2u : 0u) |
            (b3 ? 4u : 0u) |
            (b4 ? 8u : 0u) |
            (b5 ? 16u : 0u) |
            (b6 ? 32u : 0u) |
            (b7 ? 64u : 0u) |
            (b8 ? 128u : 0u)
            );
    }
    public int Sum() => BitOperations.PopCount(data);

    public bool IsEmpty() => data == 0u;
}
