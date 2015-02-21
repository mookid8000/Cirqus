using d60.Cirqus.Testing;
using NUnit.Framework;
using TestContext = NUnit.Framework.TestContext;

namespace d60.Cirqus.NUnit
{
    public class CirqusTests : CirqusTestsHarness
    {
        [SetUp]
        public void SetupInternal()
        {
            Begin(CreateContext());
            Setup();
        }

        protected virtual Testing.TestContext CreateContext()
        {
            return Testing.TestContext.Create();
        }

        protected virtual void Setup()
        {
        }

        [TearDown]
        public void TeardownInternal()
        {
            Teardown();
            End(TestContext.CurrentContext.Result.State == TestState.Error);
        }

        protected virtual void Teardown()
        {
        }

        protected override void Fail()
        {
            throw new AssertionException("");
        }
    }
}
