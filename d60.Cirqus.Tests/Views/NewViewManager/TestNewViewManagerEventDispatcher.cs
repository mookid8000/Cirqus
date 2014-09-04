using System;
using System.Threading;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.Logging;
using d60.Cirqus.MongoDb.Config;
using d60.Cirqus.MongoDb.Views;
using d60.Cirqus.Numbers;
using d60.Cirqus.Tests.MongoDb;
using d60.Cirqus.Tests.Views.NewViewManager.Commands;
using d60.Cirqus.Tests.Views.NewViewManager.Views;
using d60.Cirqus.Views.NewViewManager;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Views.NewViewManager
{
    [TestFixture]
    public class TestNewViewManagerEventDispatcher : FixtureBase
    {
        NewMongoDbViewManager<AllPotatoesView> _allPotatoesView;
        NewMongoDbViewManager<PotatoTimeToBeConsumedView> _potatoTimeToBeConsumedView;

        NewViewManagerEventDispatcher _dispatcher;

        ICommandProcessor _commandProcessor;

        protected override void DoSetUp()
        {
            var mongoDatabase = MongoHelper.InitializeTestDatabase();

            _commandProcessor = CommandProcessor.With()
                .Logging(l => l.UseConsole(minLevel: Logger.Level.Warn))
                .EventStore(e => e.UseMongoDb(mongoDatabase, "Events"))
                .EventDispatcher(e => e.Registrar.Register<IEventDispatcher>(r =>
                {
                    var repository = r.Get<IAggregateRootRepository>();
                    var eventStore = r.Get<IEventStore>();

                    _allPotatoesView = new NewMongoDbViewManager<AllPotatoesView>(mongoDatabase);
                    _potatoTimeToBeConsumedView = new NewMongoDbViewManager<PotatoTimeToBeConsumedView>(mongoDatabase);

                    _dispatcher = new NewViewManagerEventDispatcher(repository, eventStore, _allPotatoesView, _potatoTimeToBeConsumedView);

                    return _dispatcher;
                }))
                .Create();
        }

        [Test]
        public void ItWorks()
        {
            // arrange
            var potato1Id = Guid.NewGuid();
            var potato2Id = Guid.NewGuid();
            var potato3Id = Guid.NewGuid();

            // act
            var firstPointInTime = new DateTime(1979, 3, 1, 12, 0, 0, DateTimeKind.Utc);
            TimeMachine.FixCurrentTimeTo(firstPointInTime);
            _commandProcessor.ProcessCommand(new BitePotato(potato1Id, 0.5m));
            _commandProcessor.ProcessCommand(new BitePotato(potato2Id, 0.3m));
            _commandProcessor.ProcessCommand(new BitePotato(potato2Id, 0.3m));
            _commandProcessor.ProcessCommand(new BitePotato(potato3Id, 0.3m));

            var nextPointInTime = new DateTime(1981, 6, 9, 12, 0, 0, DateTimeKind.Utc);
            TimeMachine.FixCurrentTimeTo(nextPointInTime);
            _commandProcessor.ProcessCommand(new BitePotato(potato1Id, 0.5m));
            _commandProcessor.ProcessCommand(new BitePotato(potato2Id, 0.5m));

            var lastPointInTime = new DateTime(1981, 6, 9, 12, 0, 0, DateTimeKind.Utc);
            _commandProcessor.ProcessCommand(new BitePotato(potato3Id, 0.8m));

            Thread.Sleep(1000);

            // assert
            var allPotatoes = _allPotatoesView.Load(GlobalInstanceLocator.GetViewInstanceId());

            Assert.That(allPotatoes, Is.Not.Null);

            var potato1View = _potatoTimeToBeConsumedView.Load(InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId(potato1Id));
            var potato2View = _potatoTimeToBeConsumedView.Load(InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId(potato2Id));
            var potato3View = _potatoTimeToBeConsumedView.Load(InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId(potato3Id));

            Assert.That(potato1View, Is.Not.Null);
            Assert.That(potato2View, Is.Not.Null);
            Assert.That(potato3View, Is.Not.Null);

            Assert.That(allPotatoes.NamesOfPotatoes.Count, Is.EqualTo(3));
            Assert.That(allPotatoes.NamesOfPotatoes[potato1Id], Is.EqualTo("Jeff"));
            Assert.That(allPotatoes.NamesOfPotatoes[potato2Id], Is.EqualTo("Bunny"));
            Assert.That(allPotatoes.NamesOfPotatoes[potato3Id], Is.EqualTo("Walter"));

            Assert.That(potato1View.Name, Is.EqualTo("Jeff"));
            Assert.That(potato1View.TimeOfCreation.ToUniversalTime(), Is.EqualTo(firstPointInTime));
            Assert.That(potato1View.TimeToBeEaten, Is.EqualTo(nextPointInTime - firstPointInTime));

            Assert.That(potato2View.Name, Is.EqualTo("Bunny"));
            Assert.That(potato2View.TimeOfCreation.ToUniversalTime(), Is.EqualTo(firstPointInTime));
            Assert.That(potato2View.TimeToBeEaten, Is.EqualTo(nextPointInTime - firstPointInTime));

            Assert.That(potato3View.Name, Is.EqualTo("Walter"));
            Assert.That(potato3View.TimeOfCreation.ToUniversalTime(), Is.EqualTo(firstPointInTime));
            Assert.That(potato3View.TimeToBeEaten, Is.EqualTo(lastPointInTime - firstPointInTime));
        }
    }
}