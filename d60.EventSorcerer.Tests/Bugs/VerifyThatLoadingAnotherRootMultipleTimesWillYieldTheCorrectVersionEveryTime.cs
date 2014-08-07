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
        public void ItWorks()
        {
            var root1Id = Guid.NewGuid();
            var root2Id = Guid.NewGuid();

            _context.Get<Root>(root1Id);
            _context.Get<Root>(root2Id);
            _context.Commit();

            // expected grabbing: "N/A"
            _context.Get<Root>(root1Id).GrabInformationFrom(root2Id);
            _context.Commit();

            _context.Get<Root>(root2Id).SetName("I now have a name!");
            _context.Commit();

            // expected grabbing: "I now have a name!"
            _context.Get<Root>(root1Id).GrabInformationFrom(root2Id);
            _context.Commit();


            // expected grabbing: "I now have a NEW name!"
            _context.Get<Root>(root2Id).SetName("I now have a NEW name!");
            _context.Get<Root>(root1Id).GrabInformationFrom(root2Id);
            _context.Commit();

            var rootWithGrabbings = _context.Get<Root>(root1Id);
            var grabbedNames = rootWithGrabbings.InformationGrabbings.Select(g => g.Item2).ToArray();

            Assert.That(grabbedNames, Is.EqualTo(new[] {"N/A", "I now have a name!", "I now have a new name!"}));
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
                _informationGrabbings.Add(Tuple.Create(e.OtherRootId, Load<Root>(e.OtherRootId).Name));
            }

            public void Apply(RootNamed e)
            {
                _name = e.Name;
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