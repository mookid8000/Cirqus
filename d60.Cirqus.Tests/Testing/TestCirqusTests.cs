using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Identity;
using d60.Cirqus.NUnit;
using d60.Cirqus.Testing;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;
using Shouldly;

namespace d60.Cirqus.Tests.Testing
{
    [TestFixture]
    public class TestCirqusTests : CirqusTests
    {
        TestWriter writer;

        protected override IWriter GetWriter()
        {
            writer = new TestWriter();
            return writer;
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

            When(new CommandResultingInOneEvent { Id = Id<RootA>() });

            Then(Id<RootA>(), new EventA2());

            Assert.AreEqual(string.Format(
@"Given that:
  EventA1
    Id: {0}

When users:
  CommandResultingInOneEvent
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

            When(new CommandResultingInOneEvent { Id = Id<RootA>() });

            Assert.Throws<AssertionException>(() => Then(NewId<RootA>(), new EventA2()));
        }

        [Test]
        public void GivenWhenThenDiff()
        {
            Emit(NewId<RootA>(), new EventA1());

            When(new CommandResultingInA3 { Id = Id<RootA>() });

            Should.Throw<AssertionException>(() =>
                Then(NewId<RootA>(), new EventA3
                {
                    Content = "asger"
                }));

            writer.Buffer.Trim().ShouldContain(
@"
-   ""Content"": null
+   ""Content"": ""asger""
");
        }

        [Test]
        public void GivenWithImplicitId()
        {
            Emit(NewId<RootA>(), new EventA1());
            Emit(Id<RootA>(), new EventA2());

            var history = Context.History.ToList();
            Assert.AreEqual(Id<RootA>().ToString(), history[0].GetAggregateRootId());
            Assert.AreEqual(Id<RootA>().ToString(), history[1].GetAggregateRootId());
        }

        [Test]
        public void ThenWithImplicitId()
        {
            Emit(NewId<RootA>(), new EventA1());

            When(new CommandResultingInOneEvent { Id = Id<RootA>() });

            Then<RootA>(new EventA2());
        }

        [Test]
        public void ThenWithExplicitId()
        {
            var id = Guid.NewGuid().ToString();

            Emit<RootA>(id, new EventA1());

            When(new CommandResultingInOneEvent { Id = id });

            Then<RootA>(new EventA2());
        }

        [Test]
        public void ThenWhenNoEventsAreEmitted()
        {
            var id = Guid.NewGuid().ToString();

            Emit<RootA>(id, new EventA1());

            When(new CommandWithNoResult());

            Should.Throw<AssertionException>(() => Then<RootA>(new EventA2()));
        }

        [Test]
        public void ThenWhenExpectingTwoButGotOne()
        {
            var id = Guid.NewGuid().ToString();

            Emit<RootA>(id, new EventA1());

            When(new CommandResultingInOneEvent { Id = id });

            Then<RootA>(new EventA2());
            Should.Throw<AssertionException>(() => Then<RootA>(new EventA2()));
        }

        [Test]
        public void ThenWhenExpectingTwoAndGotTwo()
        {
            var id = Guid.NewGuid().ToString();

            Emit<RootA>(id, new EventA1());

            When(new CommandResultingInTwoEvents() { Id = id });

            Then<RootA>(new EventA2());
            Then<RootA>(new EventA2());
        }

        [Test]
        public void IdForDerivedClassIsNotApplicableToBaseClass()
        {
            Emit(NewId<RootAExtended>(), new EventA1());
            
            Assert.Throws<IndexOutOfRangeException>(() => Id<RootA>())
                .Message.ShouldBe("Could not find Id<RootA> with index 1");
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
            BeforeEmit += x => flag = true;

            var id = Guid.NewGuid().ToString();
            Emit<RootA>(id, new EventA1());

            Assert.IsTrue(flag);
        }

        [Test]
        public void WhenWithHook()
        {
            var flag = false;
            BeforeExecute += x => flag = true;

            var id = Guid.NewGuid().ToString();
            Emit<RootA>(id, new EventA1());

            When(new CommandResultingInOneEvent { Id = id });

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
                    var context = new Dictionary<string, object>
                    {
                        { "Tag", new Tag() }
                    };

                    d.UseViewManagerEventDispatcher(a).WithViewContext(context);
                    d.UseDependentViewManagerEventDispatcher(b).DependentOn(a).WithViewContext(context);
                });
            });

            Emit(NewId<RootA>(), new EventA1());

            b.Load(GlobalInstanceLocator.GetViewInstanceId())
                .Tag.ShouldBe("Greetings from ViewA;Greetings from ViewB");
        }

        [Test]
        public void CanUpdateViewSynchronously()
        {
            var view = new InMemoryViewManager<ViewA>();

            Configure(x => x.EventDispatcher(d => d
                .UseSynchronousViewManangerEventDispatcher(view)
                .WithViewContext(new Dictionary<string, object>
                {
                    {"Tag", new Tag()}
                })));

            Emit(NewId<RootA>(), new EventA1());
            Emit(NewId<RootA>(), new EventA1());

            view.GetPosition().Result.ShouldBe(1); // position starts at -1
        }

        [Test]
        public void CanUpdateMultipleViewsSynchronously()
        {
            var a = new InMemoryViewManager<ViewA>();
            var b = new InMemoryViewManager<ViewB>();

            Configure(x => x.EventDispatcher(d => d
                .UseSynchronousViewManangerEventDispatcher(a, b)
                .WithViewContext(new Dictionary<string, object>
                {
                    {"Tag", new Tag()}
                })));

            Emit(NewId<RootA>(), new EventA1());

            b.Load(GlobalInstanceLocator.GetViewInstanceId())
                .Tag.ShouldBe("Greetings from ViewA;Greetings from ViewB");
        }

        [Test]
        public void ExceptionsInViewsBubblesToSurface()
        {
            var view = new InMemoryViewManager<ThrowingView>();

            Configure(x => x.EventDispatcher(d => d.UseSynchronousViewManangerEventDispatcher(view)));

            Should.Throw<ApplicationException>(() => Emit(NewId<RootA>(), new EventA1()))
                .InnerException.ShouldBeOfType<InvalidOperationException>()
                .Message.ShouldBe("hej");
        }

        [Test]
        public void EmitToAnyStream()
        {
            KeyFormat.For<object>("stream-*");

            Emit<object>("stream-id", new EventWithNoRoot());

            var @event = Context.History.Single();
            @event.ShouldBeOfType<EventWithNoRoot>();
            @event.Meta[DomainEvent.MetadataKeys.AggregateRootId].ShouldBe("stream-id");
            @event.Meta[DomainEvent.MetadataKeys.Owner].ShouldBe("System.Object, mscorlib");
        }

        public class RootA : AggregateRoot, IEmit<EventA1>, IEmit<EventA2>, IEmit<EventA3>
        {
            public void DoA1()
            {
                Emit(new EventA1());
            }

            public void DoA2()
            {
                Emit(new EventA2());
            }

            public void DoA3()
            {
                Emit(new EventA3());
            }

            public void Apply(EventA1 e)
            {

            }

            public void Apply(EventA2 e)
            {

            }

            public void Apply(EventA3 e)
            {
                
            }
        }

        public class RootAExtended : RootA
        {
        }

        public class EventA1 : DomainEvent<RootA>
        {
        }

        public class EventA2 : DomainEvent<RootA>
        {
        }

        public class EventA3 : DomainEvent<RootA>
        {
            public string Content { get; set; }
        }

        public class EventWithNoRoot : DomainEvent
        {
            
        }

        public class CommandResultingInOneEvent : ExecutableCommand
        {
            public string Id { get; set; }

            public override void Execute(ICommandContext context)
            {
                var rootA = context.Load<RootA>(Id);
                rootA.DoA2();
            }
        }

        public class CommandWithNoResult : ExecutableCommand
        {
            public override void Execute(ICommandContext context) {}
        }

        public class CommandResultingInTwoEvents : ExecutableCommand
        {
            public string Id { get; set; }

            public override void Execute(ICommandContext context)
            {
                var rootA = context.Load<RootA>(Id);
                rootA.DoA2();
                rootA.DoA2();
            }
        }

        public class CommandResultingInA3 : ExecutableCommand
        {
            public string Id { get; set; }

            public override void Execute(ICommandContext context)
            {
                context.Load<RootA>(Id).DoA3();
            }
        }

        public class ViewA : IViewInstance<GlobalInstanceLocator>, ISubscribeTo<EventA1>
        {
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }

            public void Handle(IViewContext context, EventA1 domainEvent)
            {
                ((Tag)context.Items["Tag"]).Text = "Greetings from ViewA";
            }
        }

        public class ViewB : IViewInstance<GlobalInstanceLocator>, ISubscribeTo<EventA1>
        {
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }
            public string Tag { get; set; }

            public void Handle(IViewContext context, EventA1 domainEvent)
            {
                Tag = ((Tag)context.Items["Tag"]).Text + ";Greetings from ViewB";
            }
        }

        public class Tag
        {
            public string Text { get; set; }
        }

        public class ThrowingView : IViewInstance<GlobalInstanceLocator>, ISubscribeTo<EventA1>
        {
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }

            public void Handle(IViewContext context, EventA1 domainEvent)
            {
                throw new InvalidOperationException("hej");
            }
        }
    }
}