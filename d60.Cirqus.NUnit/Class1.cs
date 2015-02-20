using d60.Cirqus.Testing;
using EnergyProjects.Tests;
using NUnit.Framework;

namespace d60.Cirqus.NUnit
{
    public class NUnitCirqusTests : CirqusTestsHarness
    {
        public NUnitCirqusTests() : base(() => new ConsoleWriter()) { }

        [SetUp]
        public void SetupInternal()
        {
            Begin();
            Setup();
        }

        protected virtual void Setup()
        {
        }
    }
}
