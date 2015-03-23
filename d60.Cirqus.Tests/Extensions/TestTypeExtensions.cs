using d60.Cirqus.Extensions;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Extensions
{
    [TestFixture]
    public class TestTypeExtensions : FixtureBase
    {
        [Test]
        public void CanPrettyFormatSimpleTypeName()
        {
            Assert.That(typeof(string).GetPrettyName(), Is.EqualTo("String"));
            Assert.That(typeof(string).GetPrettyName(includeNamespace:true), Is.EqualTo("System.String"));
        }

        [Test]
        public void CanPrettyFormatGenericType()
        {
            Assert.That(typeof (GenericsBaby<string>).GetPrettyName(), Is.EqualTo("GenericsBaby<String>"));
            Assert.That(typeof(GenericsBaby<string>).GetPrettyName(includeNamespace: true), Is.EqualTo("d60.Cirqus.Tests.Extensions.GenericsBaby<System.String>"));
        }
    }

    class GenericsBaby<T> { }
}