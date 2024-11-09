using System;
using System.Runtime.InteropServices;

namespace VoxelGame.scripts.common;


public struct ByteBuffer : IDisposable
{
    unsafe byte* buffer;
    nuint capacity;

    public ByteBuffer(nuint initial_size)
    {
        unsafe
        {
            buffer = (byte*)NativeMemory.AlignedAlloc(initial_size, 1);
            capacity = initial_size;
        }
    }

    public Span<byte> Slice(nuint size)
    {
        unsafe
        {
            if (capacity < size)
            {
                buffer = (byte*)NativeMemory.AlignedRealloc(buffer, size, 1);
                capacity = size;
            }
            return new Span<byte>(buffer, (int)size);
        }
    }

    public readonly void Dispose()
    {
        unsafe
        {
            NativeMemory.AlignedFree(buffer);
        }
    }

    public static ByteBuffer[] News(int quantity, nuint bufferSize)
    {
        var rtn = new ByteBuffer[quantity];
        for (uint it = 0; it < quantity; it++)
        {
            rtn[it] = new(bufferSize);
        }
        return rtn;
    }

    public static void DisposeAll(ByteBuffer[] buffers)
    {
        foreach (var buffer in buffers)
        {
            buffer.Dispose();
        }
    }
}

