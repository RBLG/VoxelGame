using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using VoxelGame.scripts.common.math;

namespace VoxelGame.scripts.common.arrays;
public interface IArray3d<OBJ>
{

    public Vector3T<int> Size { get; }

    public OBJ this[Vector3T<int> xyz]
    {
        get;// => this[GetIndexFromXyz(x, y, z)];
        set;// => this[GetIndexFromXyz(x, y, z)] = value;
    }

    public OBJ this[int i] { get; set; }

    public int GetIndexFromXyz(Vector3T<int> xyz);

    public void ForAll(Action<Vector3T<int>> action) => ForAll(Size, action);

    public static void ForAll(Vector3T<int> size, Action<Vector3T<int>> action)
    {
        for (int itx = 0; itx < size.X; itx++)
        {
            for (int ity = 0; ity < size.Y; ity++)
            {
                for (int itz = 0; itz < size.Z; itz++)
                {
                    action(new(itx, ity, itz));
                }
            }
        }
    }


}

