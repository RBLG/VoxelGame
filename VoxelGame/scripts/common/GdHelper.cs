﻿using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoxelGame.scripts.common.math;

namespace VoxelGame.scripts.common;
public static class GdHelper {
    public static Texture2DArray NewBlankTexture2DArray(Vector3T<int> size, bool mipmap, Image.Format format) {
        return CleanTexture(new(), size, mipmap, format);
    }

    public static Texture2DArray CleanTexture(Texture2DArray texture, Vector3T<int> size, bool mipmap, Image.Format format) {
        var imgs = new Image[size.Z].Select((i) => Image.CreateEmpty(size.X, size.Y, mipmap, format)).ToArray();
        _ = texture.CreateFromImages(new(imgs));
        return texture;
    }

}




