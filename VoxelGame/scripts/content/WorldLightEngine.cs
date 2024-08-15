using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoxelGame.scripts.common;

namespace VoxelGame.scripts.content;
public class WorldLightEngine {


    public void ComputeCardinalInChunk(Chunk chunk, Vector3T<long> offset, Chunk last, Vector3T<long> incr, byte[] rslt) {
        Vector3T<long> start = offset.Clamp(new(0), Chunk.LSize);
        Vector3T<long> end = (Chunk.LSize * incr + start).Clamp(new(0), Chunk.LSize);
        for (uint it = 0; Math.Abs(it) < Chunk.Size.X; it++) {

        }
        throw new NotImplementedException();
    }

    public byte[] ComputeDiagonalInChunk(Chunk chunk, Vector3T<long> offset, Chunk last) {
        throw new NotImplementedException();
    }


    public void ComputeCubonalInChunk(Chunk chunk, Vector3T<long> offset, Chunk lx, Chunk ly, Chunk lz) {
        throw new NotImplementedException();
    }


}

