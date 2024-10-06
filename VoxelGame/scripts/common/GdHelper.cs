using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoxelGame.scripts.common;
public static class GdHelper {
    public static Texture2DArray NewBlankTexture2DArray(Vector3T<int> size, bool mipmap, Image.Format format) {
        var imgs = new Image[size.Z].Select((i) => Image.CreateEmpty(size.X, size.Y, mipmap, format));
        Texture2DArray texture = new();
        _ = texture.CreateFromImages(new(imgs.ToArray()));
        return texture;
    }

}




