using Eocron.Algorithms.Disposing;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class DisposableTests
    {
        [Test]
        public void StreamClosedAndDisposed()
        {
            var ts = new TestStream(10, 42);
            using (new DisposableStream(ts, ts)) { }

            Assert.IsTrue(ts.Closed);
            Assert.IsTrue(ts.Disposed);
        }

        [Test]
        public void Disposed()
        {
            var disposed = 0;
            using (new Disposable(()=> disposed++)) { }

            Assert.AreEqual(1, disposed);
        }
    }
}
