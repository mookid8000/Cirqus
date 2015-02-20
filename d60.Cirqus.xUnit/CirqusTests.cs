using System;
using d60.Cirqus.Testing;
using EnergyProjects.Tests;

namespace d60.Cirqus.xUnit
{
    public class CirqusTests : CirqusTestsHarness, IDisposable
    {
        public CirqusTests() : base(() => new ConsoleWriter())
        {
            Begin();
        }

        public void Dispose()
        {
            
        }
    }
}
