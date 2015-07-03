#region

using d60.Cirqus.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestContext = Microsoft.VisualStudio.TestTools.UnitTesting.TestContext;

#endregion

namespace d60.Cirqus.MSTest
{
    public class CirqusTests : CirqusTestsHarness
    {
        public TestContext TestContext { get; protected set; }

        [TestInitialize]
        public void SetupInternal()
        {
            Begin();
            Setup();
        }

        protected virtual void Setup()
        {
        }

        [TestCleanup]
        public void TeardownInternal()
        {
            Teardown();
            End(TestContext.CurrentTestOutcome != UnitTestOutcome.Error);
        }

        protected virtual void Teardown()
        {
        }

        protected override void Fail()
        {
            Assert.Fail();
        }
    }
}