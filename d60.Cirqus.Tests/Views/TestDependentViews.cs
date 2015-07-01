using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.MongoDb.Views;
using d60.Cirqus.Serialization;
using d60.Cirqus.Tests.Extensions;
using d60.Cirqus.Tests.MongoDb;
using d60.Cirqus.Views;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Views
{
    [TestFixture]
    public class TestDependentViews : FixtureBase
    {
        [Test]
        public async Task YeyItWorks()
        {
            CirqusLoggerFactory.Current = new ConsoleLoggerFactory();

            var mongoDatabase = MongoHelper.InitializeTestDatabase();

            var firstView = new MongoDbViewManager<HeyCounter>(mongoDatabase);
            var secondView = new MongoDbViewManager<WordCounter>(mongoDatabase);

            /* |---===^^^===---| */
            var dependentView = new MongoDbViewManager<HeyPercentageCalculator>(mongoDatabase);
            /* |---===___===---| */

            var waitHandle = new ViewManagerWaitHandle();
            var specialWaitHandle = new ViewManagerWaitHandle();

            var commandProcessor = CommandProcessor.With()
                .EventStore(e => e.UseInMemoryEventStore())
                .EventDispatcher(e =>
                {
                    e.UseViewManagerEventDispatcher(firstView).WithWaitHandle(waitHandle);
                    e.UseViewManagerEventDispatcher(secondView).WithWaitHandle(waitHandle);

                    e.UseEventDispatcher(c =>
                    {
                        var dependencies = new IViewManager[] { firstView, secondView };

                        var viewManagers = new IViewManager[] { dependentView };

                        var eventStore = c.Get<IEventStore>();
                        var domainEventSerializer = c.Get<IDomainEventSerializer>();
                        var aggregateRootRepository = c.Get<IAggregateRootRepository>();
                        var domainTypeNameMapper = c.Get<IDomainTypeNameMapper>();

                        return new DependentViewManagerEventDispatcher(dependencies, viewManagers, eventStore,
                            domainEventSerializer, aggregateRootRepository, domainTypeNameMapper, specialWaitHandle,
                            new Dictionary<string, object>
                            {
                                {"heys", firstView},
                                {"words", secondView},
                            });
                    });
                })
                .Create();

            RegisterForDisposal(commandProcessor);

            var result = Enumerable.Range(0, 100)
                .Select(i => commandProcessor.ProcessCommand(new DoStuff("test", "hej med dig min ven " + i)))
                .Last();

            await waitHandle.WaitForAll(result, TimeSpan.FromSeconds(5));

            var viewId = InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId("test");

            var firstViewInstance = firstView.Load(viewId);
            var secondViewInstance = secondView.Load(viewId);

            Assert.That(firstViewInstance.Count, Is.EqualTo(100));
            Assert.That(secondViewInstance.Count, Is.EqualTo(600));

            Console.WriteLine("Waiting for dependent views to catch up...");
            await specialWaitHandle.WaitForAll(result, TimeSpan.FromSeconds(5));
            Console.WriteLine("DOne!");

            var heyPercentageCalculator = dependentView.Load(viewId);
        }

        public class HeyPercentageCalculator : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<DidStuff>
        {
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }

            public long HeyPosition { get; set; }
            public long WordPosition { get; set; }
            public decimal HeyPercentage { get; set; }

            public void Handle(IViewContext context, DidStuff domainEvent)
            {
                var heyCounter = context.Get<IViewManager<HeyCounter>>("heys");
                var wordCounter = context.Get<IViewManager<WordCounter>>("words");

                var heyPosition = heyCounter.GetPosition().Result;
                var wordPosition = wordCounter.GetPosition().Result;

                if (heyPosition == HeyPosition) return;
                if (wordPosition == WordPosition) return;

                Console.WriteLine("Loading and stuff - {0} {1}", heyPosition, wordPosition);

                var heys = heyCounter.Load(Id);
                var words = wordCounter.Load(Id);

                HeyPercentage = 100M*heys.Count/words.Count;

                HeyPosition = heyPosition;
                WordPosition = wordPosition;
            }
        }

        public class HeyCounter : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<DidStuff>
        {
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }
            public int Count { get; set; }
            public void Handle(IViewContext context, DidStuff domainEvent)
            {
                var words = domainEvent.Text
                    .Split(" ;,".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                var numberOfHeys = words.Count(c => c.Equals("hej", StringComparison.CurrentCultureIgnoreCase));

                Count += numberOfHeys;
            }
        }

        public class WordCounter : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<DidStuff>
        {
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }
            public int Count { get; set; }
            public void Handle(IViewContext context, DidStuff domainEvent)
            {
                var words = domainEvent.Text
                    .Split(" ;,".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                Count += words.Length;
            }
        }

        public class DoStuff : Command<SomeRoot>
        {
            public string Text { get; private set; }

            public DoStuff(string rootId, string text)
                : base(rootId)
            {
                Text = text;
            }

            public override void Execute(SomeRoot root)
            {
                root.DoStuff(Text);
            }
        }

        public class DidStuff : DomainEvent<SomeRoot>
        {
            public string Text { get; private set; }

            public DidStuff(string text)
            {
                Text = text;
            }
        }

        public class SomeRoot : AggregateRoot, IEmit<DidStuff>
        {
            public void DoStuff(string text)
            {
                Emit(new DidStuff(text));
            }

            public void Apply(DidStuff e)
            {
            }
        }
    }

    public static class ViewContextEx
    {
        public static T Get<T>(this IViewContext context) where T : class
        {
            if (context == null) throw new ArgumentNullException("context");
            return Get<T>(context, typeof (T).FullName);
        }

        public static T Get<T>(this IViewContext context, string key) where T : class
        {
            if (context == null) throw new ArgumentNullException("context");
            object value;
            if (context.Items.TryGetValue(key, out value))
            {
                try
                {
                    return (T)value;

                }
                catch (Exception exception)
                {
                    throw new InvalidCastException(string.Format("Could not get item {0} with key '{1}' as {2}!", value, key, typeof(T)), exception);
                }
            }
            throw new KeyNotFoundException(string.Format("Could not find item with key '{0}' in the current view context!", key));
        }
    }
}