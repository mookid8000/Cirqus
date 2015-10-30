using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Views;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
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
            Assert.That(typeof(string).GetPrettyName(includeNamespace: true), Is.EqualTo("System.String"));
        }

        [Test]
        public void CanPrettyFormatGenericType()
        {
            Assert.That(typeof(GenericsBaby<string>).GetPrettyName(), Is.EqualTo("GenericsBaby<String>"));
            Assert.That(typeof(GenericsBaby<string>).GetPrettyName(includeNamespace: true), Is.EqualTo("d60.Cirqus.Tests.Extensions.GenericsBaby<System.String>"));
        }

        [Test]
        public void CanGetViewTypeFromViewManager()
        {
            IViewManager viewManager = new InMemoryViewManager<SomeView>();

            Assert.That(viewManager.GetViewType(), Is.EqualTo(typeof(SomeView)));
        }

        class SomeView : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<DomainEvent>
        {
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }
            public void Handle(IViewContext context, DomainEvent domainEvent)
            {
                
            }
        }
    }

    class GenericsBaby<T> { }
}