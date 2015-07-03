using System;
using System.Runtime.InteropServices;
using d60.Cirqus.Testing;
using Xunit;

namespace d60.Cirqus.xUnit
{
    public class CirqusTests : CirqusTestsHarness, IDisposable
    {
        public CirqusTests()
        {
            Begin();
        }

        public void Dispose()
        {
            End(Marshal.GetExceptionCode() != 0);
        }

        protected override void Fail()
        {
            Assert.False(true, "Assertion failed.");
        }
    }
}
