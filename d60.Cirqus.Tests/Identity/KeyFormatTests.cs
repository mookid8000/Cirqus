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
            GetKeyFormat("ko-guid-{Int}-*-abe").ToString()
                .ShouldBe("ko-guid-{Int}-*-abe");
        }

        [Test]
        public void CanGetValueFromNaturalKey()
        {
            GetKeyFormat("user-{Username}")
                .Get("Username", "user-ahl@nonsense.dk")
                .ShouldBe("ahl@nonsense.dk");
        }

        KeyFormat GetKeyFormat(string format)
        {
            return new KeyFormatParser('-').KeySpecification.Parse(format);
        }
    }
}