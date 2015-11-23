using System;
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

        [Test]
        public void CanGetTypeById()
        {
            KeyFormat.For<int>("i-{Username}");
            KeyFormat.For<double>("d-guid");

            KeyFormat.GetTypeById("d-9EA4FEC2-AA9F-460A-A2B7-60903218149D").ShouldBe(typeof(double));
        }

        [Test]
        public void HashCodeMatches()
        {
            GetKeyFormat("sguid").GetHashCode().ShouldBe(GetKeyFormat("sguid").GetHashCode());
        }

        KeyFormat GetKeyFormat(string format)
        {
            return new KeyFormatParser('-', "guid").Execute(format);
        }
    }
}