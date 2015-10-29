using System;
using System.Runtime.InteropServices;
using d60.Cirqus.Testing;
using Xunit.Abstractions;

namespace d60.Cirqus.xUnit
{
    public class CirqusTests : CirqusTestsHarness, IDisposable
    {
        public CirqusTests(ITestOutputHelper output)
        {
            Begin(new TestOutputWriter(output));
        }

        public void Dispose()
        {
            End(Marshal.GetExceptionCode() != 0);
        }

        protected override void Fail()
        {
            Xunit.Assert.False(true, "Assertion failed.");
        }
    }
}
