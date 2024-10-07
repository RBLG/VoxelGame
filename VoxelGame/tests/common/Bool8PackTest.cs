using Microsoft.VisualStudio.TestTools.UnitTesting;
using VoxelGame.scripts.common;


namespace VoxelGameTest.scripts.common;

[TestClass]
public class Bool8PackTest {

    [TestMethod]
    public void GetWhen0() {
        Bool8Pack pack = new(0);
        Assert.AreEqual(pack.Get(3), 0);
    }

    [TestMethod]
    public void GetWhen24() {
        Bool8Pack pack = new(0);
        Assert.AreEqual(pack.Get(3), 1);
    }

    [TestMethod]
    public void GetWhen127() {
        Bool8Pack pack = new(0);
        Assert.AreEqual(pack.Get(7), 0);
    }

    [TestMethod]
    public void GetWhen255() {
        Bool8Pack pack = new(0);
        Assert.AreEqual(pack.Get(7), 1);
    }
}

