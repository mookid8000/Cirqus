using System;
using System.Collections.Generic;
using System.Linq;
using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.Events;
using NUnit.Framework;
using TestContext = d60.EventSorcerer.TestHelpers.TestContext;

namespace d60.EventSorcerer.Tests.Bugs
{
    [TestFixture]
    public class VerifyThatLoadingAnotherRootMultipleTimesWillYieldTheCorrectVersionEveryTime : FixtureBase
    {
        TestContext _context;

        protected override void DoSetUp()
        {
            _context = new TestContext();
        }

        [Test]
        public void ItWorksInTheSameUnitOfWork()
        {
            var root1Id = Guid.NewGuid();
            var root2Id = Guid.NewGuid();

            Console.WriteLine("** Creating two aggregate roots **");
            _context.Get<Root>(root1Id);
            _context.Get<Root>(root2Id);
            Commit();

            Console.WriteLine("** Making 1 grab info from 2 **");
            // expected grabbing: "N/A"
            _context.Get<Root>(root1Id).GrabInformationFrom(root2Id);
            Commit();

            Console.WriteLine("** Making 1 grab info from 2 **");
            Console.WriteLine("** Setting name of 2 to 'I now have a NEW name!' **");
            Console.WriteLine("** Making 1 grab info from 2 **");
            // expected grabbing: "I now have a NEW name!"
            _context.Get<Root>(root1Id).GrabInformationFrom(root2Id);
            _context.Get<Root>(root2Id).SetName("I now have a NEW name!");
            _context.Get<Root>(root1Id).GrabInformationFrom(root2Id);
            Commit();

            var rootWithGrabbings = _context.Get<Root>(root1Id);
            var grabbedNames = rootWithGrabbings.InformationGrabbings.Select(g => g.Item2).ToArray();
            var expectedNames = new[] {"N/A", "N/A", "I now have a NEW name!"};

            Assert(grabbedNames, expectedNames);
        }

        [Test]
        public void ItWorksAcrossUnitsOfWork()
        {
            var root1Id = Guid.NewGuid();
            var root2Id = Guid.NewGuid();

            Console.WriteLine("** Creating two aggregate roots **");
            _context.Get<Root>(root1Id);
            _context.Get<Root>(root2Id);
            Commit();

            Console.WriteLine("** Making 1 grab info from 2 **");
            // expected grabbing: "N/A"
            _context.Get<Root>(root1Id).GrabInformationFrom(root2Id);
            Commit();

            Console.WriteLine("** Setting name of 2 to 'I now have a name!' **");
            _context.Get<Root>(root2Id).SetName("I now have a name!");
            Commit();

            Console.WriteLine("** Making 1 grab info from 2 **");
            // expected grabbing: "I now have a name!"
            _context.Get<Root>(root1Id).GrabInformationFrom(root2Id);
            Commit();

            var rootWithGrabbings = _context.Get<Root>(root1Id);
            var grabbedNames = rootWithGrabbings.InformationGrabbings.Select(g => g.Item2).ToArray();
            var expectedNames = new[] {"N/A", "I now have a name!"};

            Assert(grabbedNames, expectedNames);
        }

        static void Assert(string[] grabbedNames, string[] expectedNames)
        {
            NUnit.Framework.Assert.That(grabbedNames, Is.EqualTo(expectedNames), @"
Expected:

    {0}

Got

    {1}

", string.Join(", ", expectedNames), string.Join(", ", grabbedNames));
        }

        void Commit()
        {
            _context.Commit();
            Console.WriteLine(" - committed - ");
            Console.WriteLine();
        }


        public class Root : AggregateRoot, IEmit<RootCreated>, IEmit<InformationGrabbedFrom>, IEmit<RootNamed>
        {
            readonly List<Tuple<Guid, string>> _informationGrabbings = new List<Tuple<Guid, string>>();
            string _name;

            public string Name
            {
                get { return _name; }
            }

            public List<Tuple<Guid, string>> InformationGrabbings
            {
                get { return _informationGrabbings; }
            }

            protected override void Created()
            {
                Emit(new RootCreated());
            }

            public void GrabInformationFrom(Guid otherRootId)
            {
                Emit(new InformationGrabbedFrom
                {
                    OtherRootId = otherRootId
                });
            }

            public void SetName(string name)
            {
                Emit(new RootNamed { Name = name });
            }

            public void Apply(RootCreated e)
            {
                _name = "N/A";
            }

            public void Apply(InformationGrabbedFrom e)
            {
                var name = Load<Root>(e.OtherRootId).Name;
                Console.WriteLine("This is what I grabbed: {0} ({1})", name, ReplayState);
                _informationGrabbings.Add(Tuple.Create(e.OtherRootId, name));
            }

            public void Apply(RootNamed e)
            {
                _name = e.Name;
            }

            protected internal override void EventEmitted(DomainEvent e)
            {
                if (e is RootNamed)
                {
                    var named = (RootNamed)e;
                    Console.WriteLine("> RootNamed({0})", named.Name);
                    return;
                }

                if (e is InformationGrabbedFrom)
                {
                    var info = (InformationGrabbedFrom) e;
                    Console.WriteLine("> InformationGrabbedFrom({0})", info.OtherRootId);
                    return;
                }
                Console.WriteLine("> {0}", e.GetType().Name);
            }
        }

        public class InformationGrabbedFrom : DomainEvent<Root>
        {
            public Guid OtherRootId { get; set; }
        }

        public class RootCreated : DomainEvent<Root> { }
        public class RootNamed : DomainEvent<Root>
        {
            public string Name { get; set; }
        }
    }
}