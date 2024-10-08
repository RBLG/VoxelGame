using Microsoft.VisualStudio.TestTools.UnitTesting;
using VoxelGame.scripts.common;


namespace VoxelGameTest.scripts.common;

[TestClass]
public class Bool8PackTest {

    [TestMethod]
    public void GetWhen0() {
        Bool8Pack pack = new(0);
        Assert.AreEqual(0, pack.Get(3));
    }

    [TestMethod]
    public void GetWhen24() {
        Bool8Pack pack = new(24);
        Assert.AreEqual(1, pack.Get(3));
    }

    [TestMethod]
    public void GetWhen127() {
        Bool8Pack pack = new(127);
        Assert.AreEqual(0, pack.Get(7));
    }

    [TestMethod]
    public void GetWhen255() {
        Bool8Pack pack = new(255);
        Assert.AreEqual(1, pack.Get(7));
    }

    [TestMethod]
    public void IsEmptyWhen0() {
        Bool8Pack pack = new(0);
        Assert.AreEqual(true, pack.IsEmpty());
    }

    [TestMethod]
    public void IsEmptyWhenNot() {
        Bool8Pack pack = new(24);
        Assert.AreEqual(false, pack.IsEmpty());
    }

    [TestMethod]
    public void IsEmptyWhen128() {
        Bool8Pack pack = new(128);
        Assert.AreEqual(false, pack.IsEmpty());
    }

    [TestMethod]
    public void SumWhen25() {
        Bool8Pack pack = new(25);
        Assert.AreEqual(3, pack.Sum());
    }

    [TestMethod]
    public void SetWhen0() {
        Bool8Pack pack = new(0);
        pack.Set(true, false, true, false);
        Assert.AreEqual(5, pack.Data);
    }

    [TestMethod]
    public void SetWhen10() {
        Bool8Pack pack = new(10);
        pack.Set(true, false, true, false);
        Assert.AreEqual(5, pack.Data);
    }

    [TestMethod]
    public void SetTrueWhen10() {
        Bool8Pack pack = new(10);
        pack[2] = true;
        Assert.AreEqual(14, pack.Data);
    }

    [TestMethod]
    public void SetFalseWhen10() {
        Bool8Pack pack = new(10);
        pack[1] = false;
        Assert.AreEqual(8, pack.Data);
    }


}

