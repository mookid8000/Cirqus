using System;
using System.Collections.Generic;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.Projections.Views.ViewManagers;
using d60.Cirqus.Tests.Contracts.Views.Factories;
using NUnit.Framework;
using TestContext = d60.Cirqus.TestHelpers.TestContext;

namespace d60.Cirqus.Tests.Contracts.Views
{
    [TestFixture(typeof(MongoDbManagedViewFactory), Category = TestCategories.MongoDb)]
    [TestFixture(typeof(MsSqlManagedViewFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(InMemoryManagedViewFactory))]
    public class LoadingStuffDuringViewLocation<TFactory> : FixtureBase where TFactory : AbstractManagedViewFactory, new()
    {
        TFactory _factory;
        TestContext _context;

        protected override void DoSetUp()
        {
            CirqusLoggerFactory.Current = new ConsoleLoggerFactory(minLevel: Logger.Level.Debug);

            _factory = new TFactory();

            _context = RegisterForDisposal(new TestContext());
        }

        [Test]
        public void CanLoadRootsDuringViewLocation()
        {
            _context.AddViewManager(_factory.GetManagedView<CountTheNodes>());

            // arrange
            var rootNodeId = Guid.NewGuid();
            using (var uow = _context.BeginUnitOfWork())
            {
                var node = uow.Get<Node>(rootNodeId);

                var child1 = uow.Get<Node>(Guid.NewGuid());
                var child2 = uow.Get<Node>(Guid.NewGuid());

                child1.AttachTo(node);
                child2.AttachTo(node);
            
                var subChild1 = uow.Get<Node>(Guid.NewGuid());
                var subChild2 = uow.Get<Node>(Guid.NewGuid());
                var subChild3 = uow.Get<Node>(Guid.NewGuid());

                subChild1.AttachTo(child1);
                subChild2.AttachTo(child1);
                subChild3.AttachTo(child2);

                // act
                uow.Commit();
            }

            _context.WaitForViewToCatchUp<CountTheNodes>();

            // assert
            var view = _factory.Load<CountTheNodes>(rootNodeId.ToString());
            Assert.That(view.Nodes, Is.EqualTo(5));
        }

        public class CountTheNodes : IViewInstance<InstancePerRootNodeViewLocator>,
            ISubscribeTo<NodeAttachedToParentNode>
        {
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }
            public int Nodes { get; set; }
            public void Handle(IViewContext context, NodeAttachedToParentNode domainEvent)
            {
                Nodes++;
            }
        }

        public class InstancePerRootNodeViewLocator : ViewLocator
        {
            protected override IEnumerable<string> GetViewIds(IViewContext context, DomainEvent e)
            {
                if (!(e is NodeAttachedToParentNode)) throw new ArgumentException(string.Format("Can't handle {0}", e));

                var node = context.Load<Node>(e.GetAggregateRootId());

                while (node.ParentNodeId != Guid.Empty)
                {
                    node = context.Load<Node>(node.ParentNodeId);
                }

                return new[] {node.Id.ToString()};
            }
        }

        public class Node : AggregateRoot, IEmit<NodeAttachedToParentNode>, IEmit<NodeCreated>
        {
            public Guid ParentNodeId { get; private set; }

            public void AttachTo(Node parentNode)
            {
                if (ParentNodeId != Guid.Empty)
                {
                    throw new InvalidOperationException(string.Format("Cannot attach node {0} to {1} because it's already attached to {2}",
                        Id, parentNode.Id, ParentNodeId));
                }
                Emit(new NodeAttachedToParentNode { ParentNodeId = parentNode.Id });
            }

            public void Apply(NodeAttachedToParentNode e)
            {
                ParentNodeId = e.ParentNodeId;
            }

            protected override void Created()
            {
                Emit(new NodeCreated());
            }

            public void Apply(NodeCreated e)
            {
            }
        }

        public class NodeAttachedToParentNode : DomainEvent<Node>
        {
            public Guid ParentNodeId { get; set; }
        }

        public class NodeCreated : DomainEvent<Node> { }
    }
}