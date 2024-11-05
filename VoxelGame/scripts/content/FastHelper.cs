using Godot;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace VoxelGame.scripts.content;


public struct Buffer : IDisposable {
    unsafe byte* buffer;
    nuint capacity;

    public Buffer(nuint initial_size) {
        unsafe {
            buffer = (byte*)NativeMemory.AlignedAlloc(initial_size, 1);
            capacity = initial_size;
        }
    }

    public Span<byte> Slice(nuint size) {
        unsafe {
            if (size > capacity) {
                buffer = (byte*)NativeMemory.AlignedRealloc(buffer, size, 1);
                capacity = size;
            }
            return new Span<byte>(buffer, (int)size);
        }
    }

    public void Dispose() {
        unsafe {
            NativeMemory.AlignedFree(buffer);
        }
    }

    public static Buffer[] News(int quantity, nuint bufferSize) {
        var rtn = new Buffer[quantity];
        for (uint it = 0; it < quantity; it++) {
            rtn[it] = new(bufferSize);
        }
        return rtn;
    }

    public static void DisposeAll(Buffer[] buffers) {
        foreach (var buffer in buffers) {
            buffer.Dispose();
        }
    }
}

