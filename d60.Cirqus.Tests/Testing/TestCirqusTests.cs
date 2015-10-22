using System;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.NUnit;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;
using Shouldly;

namespace d60.Cirqus.Tests.Testing
{
    [TestFixture]
    public class TestCirqusTests : CirqusTests
    {
        static TestWriter writer;

        static TestCirqusTests()
        {
            Writer = () =>
            {
                writer = new TestWriter();
                return writer;
            };
        }

        [Test]
        public void FormatsGiven()
        {
            Emit(NewId<RootA>(), new EventA1());

            Assert.AreEqual(string.Format(
@"Given that:
  EventA1
    Id: {0}

", Id<RootA>()), writer.Buffer);
        }

        [Test]
        public void FormatsGivenWhenThen()
        {
            Emit(NewId<RootA>(), new EventA1());

            When(new CommandA { Id = Id<RootA>() });

            Then(Id<RootA>(), new EventA2());

            Assert.AreEqual(string.Format(
@"Given that:
  EventA1
    Id: {0}

When users:
  CommandA
    Id: {0}

Then:
  √ EventA2
      Id: {0}

", Id<RootA>()), writer.Buffer);
        }

        [Test]
        public void GivenWhenThenWrongId()
        {
            Emit(NewId<RootA>(), new EventA1());

            When(new CommandA { Id = Id<RootA>() });

            Assert.Throws<AssertionException>(() => Then(NewId<RootA>(), new EventA2()));
        }

        [Test]
        public void GivenWithImplicitId()
        {
            Emit(NewId<RootA>(), new EventA1());
            Emit<RootA>(new EventA2());

            var history = Context.History.ToList();
            Assert.AreEqual(Id<RootA>().ToString(), history[0].GetAggregateRootId());
            Assert.AreEqual(Id<RootA>().ToString(), history[1].GetAggregateRootId());
        }

        [Test]
        public void ThenWithImplicitId()
        {
            Emit(NewId<RootA>(), new EventA1());

            When(new CommandA { Id = Id<RootA>() });

            Then<RootA>(new EventA2());
        }

        [Test]
        public void ThenWithExplicitId()
        {
            var id = Guid.NewGuid().ToString();

            Emit<RootA>(id, new EventA1());

            When(new CommandA { Id = id });

            Then<RootA>(new EventA2());
        }

        [Test]
        public void IdForDerivedClassIsNotApplicableToBaseClass()
        {
            Emit(NewId<RootAExtended>(), new EventA1());
            
            Assert.Throws<InvalidOperationException>(() => Emit<RootA>(new EventA2()))
                .Message.ShouldBe("Can not get latest RootA id, since none exists.");
        }

        [Test]
        public void EmittingForDerivedRootThenBaseRootWithSameIdYieldsOneRootOfDerivedType()
        {
            var id = Guid.NewGuid().ToString();

            Emit<RootAExtended>(id, new EventA1());
            Emit<RootA>(id, new EventA2());

            Context.AggregateRoots.Count().ShouldBe(1);
            Context.AggregateRoots.Single().ShouldBeOfType<RootAExtended>();
        }

        [Test]
        public void EmittingForBaseRootThenDerivedRootWithSameIdShouldFailOnEmit()
        {
            var id = Guid.NewGuid().ToString();
            Emit<RootA>(id, new EventA1());

            Should.Throw<InvalidOperationException>(() => Emit<RootAExtended>(id, new EventA2()));
        }

        [Test]
        public void EmitWithHook()
        {
            var flag = false;
            OnEvent += x => flag = true;

            var id = Guid.NewGuid().ToString();
            Emit<RootA>(id, new EventA1());

            Assert.IsTrue(flag);
        }

        [Test]
        public void WhenWithHook()
        {
            var flag = false;
            OnCommand += x => flag = true;

            var id = Guid.NewGuid().ToString();
            Emit<RootA>(id, new EventA1());

            When(new CommandA { Id = id });

            Assert.IsTrue(flag);
        }

        [Test]
        public void CanTestWithDependentViewManagerEventDispatcher()
        {
            var a = new InMemoryViewManager<ViewA>();
            var b = new InMemoryViewManager<ViewB>();

            Configure(x =>
            {
                x.EventDispatcher(d =>
                {
                    d.UseViewManagerEventDispatcher(a);
                    d.UseDependentViewManagerEventDispatcher(b).DependentOn(a);
                });
            });

            Emit(NewId<RootA>(), new EventA1());

            Assert.NotNull(a.Load(GlobalInstanceLocator.GetViewInstanceId()));
            Assert.NotNull(b.Load(GlobalInstanceLocator.GetViewInstanceId()));
        }

        public class RootA : AggregateRoot, IEmit<EventA1>, IEmit<EventA2>
        {
            public void DoA1()
            {
                Emit(new EventA1());
            }

            public void DoA2()
            {
                Emit(new EventA2());
            }

            public void Apply(EventA1 e)
            {

            }

            public void Apply(EventA2 e)
            {

            }
        }

        public class RootAExtended : RootA
        {

            public void DoA3()
            {
                Emit(new EventA1());
                Emit(new EventA2());
            }

        }

        public class EventA1 : DomainEvent<RootA>
        {

        }

        public class EventA2 : DomainEvent<RootA>
        {

        }

        public class CommandA : ExecutableCommand
        {
            public string Id { get; set; }

            public override void Execute(ICommandContext context)
            {
                var rootA = context.Load<RootA>(Id);
                rootA.DoA2();
            }
        }

        public class ViewA : IViewInstance<GlobalInstanceLocator>, ISubscribeTo<EventA1>
        {
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }
            
            public void Handle(IViewContext context, EventA1 domainEvent)
            {
            }
        }

        public class ViewB : IViewInstance<GlobalInstanceLocator>, ISubscribeTo<EventA1>
        {
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }
            
            public void Handle(IViewContext context, EventA1 domainEvent)
            {
            }
        }
    }
}