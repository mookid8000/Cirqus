using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.Serialization;
using d60.Cirqus.Testing.Internals;
using d60.Cirqus.Views;
using d60.Cirqus.Views.ViewManagers;
using Moq;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Bugs
{
    [TestFixture]
    public class VerifyThatMaxDomainEventsPrBatchHasAValidValue : FixtureBase
    {
        [Test]
        public void WhenCreatingADependantViewDispatcherThenMaxValueIsNotZeroOrLess()
        {

            //arrange
            var viewManager1 = new Mock<IViewManager>().Object;
            var viewManager2 = new Mock<IViewManager>().Object;
            var domainSerializer = new Mock<IDomainEventSerializer>().Object;
            var eventStore = new InMemoryEventStore(domainSerializer);
            var aggregateRootRepo = new Mock<IAggregateRootRepository>().Object;
            var domainTypemapper = new Mock<IDomainTypeNameMapper>().Object;
            //act
            var sut = new DependentViewManagerEventDispatcher(new[] { viewManager1 }, new[] { viewManager2 }, eventStore,
                domainSerializer, aggregateRootRepo, domainTypemapper, null);


            //assert
            Assert.IsTrue(sut.MaxDomainEventsPerBatch >= 1);
        }


    }
}
