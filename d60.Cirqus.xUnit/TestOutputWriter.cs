using d60.Cirqus.Testing;
using Xunit.Abstractions;

namespace d60.Cirqus.xUnit
{
    public class TestOutputWriter : IWriter
    {
        private readonly ITestOutputHelper output;

        public TestOutputWriter(ITestOutputHelper output)
        {
            this.output = output;
        }

        public void Write(string text)
        {
            output.WriteLine(text);
        }
    }
}