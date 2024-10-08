
using VoxelGame.scripts.common;

namespace VoxelGameTest.scripts.common;

[TestClass]
public class BoolArray3dTest {

    [TestMethod]
    public void GetIndexWhen123() {
        BoolArray3d arr = new();
        int index = arr.GetIndexFromXyz(new(1, 2, 3));
        Assert.AreEqual(57, index);
    }

    [TestMethod]
    public void GetXyzWhen57() {
        BoolArray3d arr = new();
        var index = arr.GetXyzFromIndex(57);
        Assert.AreEqual(new(1, 2, 3), index);
    }

    [TestMethod]
    public void IsEmptyWhen0() {
        BoolArray3d arr = new();
        Assert.AreEqual(true, arr.IsEmpty());
    }

    [TestMethod]
    public void IsEmptyWhenNot() {
        BoolArray3d arr = new();
        arr[25] = true;
        Assert.AreEqual(false, arr.IsEmpty());
    }

    [TestMethod]
    public void GetLastWhenTrue() {
        BoolArray3d arr = new() {
            Data = 0x8000000000000000
        };
        Assert.AreEqual(true, arr[63]);
    }

    [TestMethod]
    public void GetLastWhenFalse() {
        BoolArray3d arr = new() {
            Data = ~0x8000000000000000
        };
        Assert.AreEqual(false, arr[63]);
    }






}

