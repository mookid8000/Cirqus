using d60.Cirqus.Identity;
using NUnit.Framework;
using Shouldly;
using Sprache;

namespace d60.Cirqus.Tests.Identity
{
    public class KeyFormatTests
    {
        [Test]
        public void ConvertsBackToString()
        {
            new KeyParser('-').KeySpecification.Parse("ko-guid-{Int}-*-abe").ToString()
                .ShouldBe("ko-guid-{Int}-*-abe");
        }
    }
}