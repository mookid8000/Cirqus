using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Identity;
using EnergyProjects.Tests.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace d60.Cirqus.Testing
{
    public abstract class CirqusTestsHarness
    {
        protected const string checkmark = "\u221A";
        protected const string cross = "\u2717";

        Stack<Tuple<Type, string>> ids;
        TextFormatter formatter;

        IOptionalConfiguration<TestContext> configuration;
        IEnumerable<DomainEvent> results;

        protected TestContext Context { get; private set; }
        protected Action<DomainEvent> BeforeEmit = x => { };
        protected Action<DomainEvent> AfterEmit = x => { };
        protected Action<Command> BeforeExecute = x => { };

        protected void Begin(IWriter writer)
        {
            ids = new Stack<Tuple<Type, string>>();

            formatter = new TextFormatter(writer);

            configuration = TestContext.With();

            Context = null;
        }

        protected void End(bool isInExceptionalState)
        {
            // only if we are _not_ in an exceptional state
            if (!isInExceptionalState)
            {
                AssertAllEventsExpected();
            }
        }

        protected void Configure(Action<IOptionalConfiguration<TestContext>> configurator)
        {
            if (Context != null)
            {
                throw new InvalidOperationException("You must call configure before first invocation of Emit() og When().");
            }

            configurator(configuration);
        }

        void EnsureContext()
        {
            if (Context != null) return;

            Context = configuration.Create();
        }

        protected abstract void Fail();

        protected void Emit<T>(params DomainEvent[] events) where T : AggregateRoot
        {
            Emit(Latest<T>(), events);
        }

        protected void Emit<T>(Id<T> id, params DomainEvent[] events)
        {
            Emit<T>((string)id, events);
        }

        protected void Emit<T>(string id, params DomainEvent[] events)
        {
            foreach (var @event in events)
            {
                Emit<T>(id, @event);
            }
        }

        void Emit<T>(string id, DomainEvent @event)
        {
            EnsureContext();

            @event.Meta[DomainEvent.MetadataKeys.AggregateRootId] = id;

            BeforeEmit(@event);

            TryRegisterId<T>(id);

            Context.Save(typeof(T), @event);

            AfterEmit(@event);

            formatter
                .Block("Given that:")
                .Write(@event, new EventFormatter(formatter))
                .NewLine()
                .NewLine();
        }

        protected void When(ExecutableCommand command)
        {
            EnsureContext();

            BeforeExecute(command);

            formatter
                .Block("When users:")
                .Write(command, new EventFormatter(formatter));

            results = Context.ProcessCommand(command);
        }

        // ReSharper disable once InconsistentNaming
        protected void Throws<T>(ExecutableCommand When) where T : Exception
        {
            Exception exceptionThrown = null;
            try
            {
                this.When(When);
            }
            catch (Exception e)
            {
                exceptionThrown = e;
            }

            formatter.Block("Then:");

            Assert(
                exceptionThrown is T,
                () => formatter.Write("It throws " + typeof(T).Name).NewLine(),
                () =>
                {
                    if (exceptionThrown == null)
                    {
                        formatter.Write("But it did not.");
                        return;
                    }

                    formatter.Write("But got " + exceptionThrown.GetType().Name).NewLine();
                });


            // consume all events
            results = Enumerable.Empty<DomainEvent>();
        }

        // ReSharper disable once InconsistentNaming
        protected void Throws<T>(string message, ExecutableCommand When) where T : Exception
        {
            Exception exceptionThrown = null;

            try
            {
                this.When(When);
            }
            catch (Exception e)
            {
                exceptionThrown = e;
            }

            formatter.Block("Then:");

            Assert(
                exceptionThrown is T && exceptionThrown.Message == message,
                () => formatter
                    .Write("It throws " + typeof(T).Name).NewLine()
                    .Indent().Write("Message: \"" + message + "\"").Unindent()
                    .NewLine(),
                () =>
                {
                    if (exceptionThrown == null)
                    {
                        formatter.Write("But it did not.");
                        return;
                    }

                    formatter.Write("But got " + exceptionThrown.GetType().Name).NewLine()
                        .Indent().Write("Message: \"" + exceptionThrown.Message + "\"").Unindent();
                });

            // consume all events
            results = Enumerable.Empty<DomainEvent>();
        }

        protected void Then<T>() where T : DomainEvent
        {
            var next = results.FirstOrDefault();

            formatter.Block("Then:");

            Assert(
                next is T,
                () => formatter.Write(typeof(T).Name).NewLine(),
                () => formatter.Write("But we got " + next.GetType().Name).NewLine());

            // consume one event
            results = results.Skip(1);
        }

        protected void Then<T>(params DomainEvent[] events) where T : AggregateRoot
        {
            Then(Latest<T>(), events);
        }

        protected void Then<T>(string id, params DomainEvent[] events) where T : AggregateRoot
        {
            Then(Identity.Id<T>.Parse(id), events);
        }

        protected void Then<T>(Id<T> id, params DomainEvent[] events) where T : AggregateRoot
        {
            if (events.Length == 0) return;

            formatter.Block("Then:");

            foreach (var expected in events)
            {
                var actual = results.FirstOrDefault();

                if (actual == null)
                {
                    Assert(false,
                        () => formatter.Write(expected, new EventFormatter(formatter)).NewLine(),
                        () => formatter.Block("But we got nothing."));

                    return;
                }

                expected.Meta[DomainEvent.MetadataKeys.AggregateRootId] = id;

                var jActual = Context.EventSerializer.Serialize(actual);
                var jExpected = Context.EventSerializer.Serialize(expected);

                Assert(
                    actual.GetAggregateRootId().Equals(id) && jActual.Data.SequenceEqual(jExpected.Data),
                    () => formatter.Write(expected, new EventFormatter(formatter)).NewLine(),
                    () =>
                    {
                        formatter.Block("But we got this:")
                            .Indent().Write(actual, new EventFormatter(formatter)).Unindent()
                            .EndBlock();

                        if (!jActual.IsJson() || !jExpected.IsJson()) return;

                        var differ = new Differ();
                        var diffs = differ.LineByLine(
                            Encoding.UTF8.GetString(jActual.Data), 
                            Encoding.UTF8.GetString(jExpected.Data));

                        var diff = differ.PrettyLineByLine(diffs);

                        formatter
                            .NewLine().NewLine()
                            .Write("Diff:").NewLine()
                            .Write(diff);
                    });

                // consume events
                results = results.Skip(1);
            }
        }

        protected void ThenNo<T>() where T : DomainEvent
        {
            formatter.Block("Then:");

            var eventsOfType = results.OfType<T>().ToList();

            Assert(
                !eventsOfType.Any(),
                () => formatter.Write(string.Format("No {0} is emitted", typeof(T).Name)),
                () =>
                {
                    formatter.Block("But we got this:");
                    foreach (var @event in eventsOfType)
                    {
                        formatter.Write(@event, new EventFormatter(formatter)).NewLine();
                    }
                    formatter.EndBlock();
                });

            // consume all events
            results = Enumerable.Empty<DomainEvent>();
        }

        protected Id<T> NewId<T>(params object[] args) where T : class
        {
            var id = GenerateId<T>(args);
            TryRegisterId<T>(id);
            return id;
        }

        protected Id<T> Id<T>() where T : class
        {
            return Id<T>(1);
        }

        protected Id<T> Id<T>(int index) where T : class
        {
            var array = ids.Where(x => x.Item1 == typeof(T)).Reverse().ToArray();
            if (array.Length < index)
            {
                throw new IndexOutOfRangeException(String.Format("Could not find Id<{0}> with index {1}", typeof(T).Name, index));
            }

            return Identity.Id<T>.Parse(array[index - 1].Item2);
        }

        protected Id<T> Latest<T>() where T : class
        {
            Id<T> id;
            if (!TryGetLatest(out id))
                throw new InvalidOperationException(string.Format("Can not get latest {0} id, since none exists.", typeof(T).Name));

            return id;
        }

        protected bool TryGetLatest<T>(out Id<T> latest) where T : class
        {
            var lastestOfType = ids.FirstOrDefault(x => x.Item1 == typeof(T));

            if (lastestOfType == null)
            {
                latest = default(Id<T>);
                return false;
            }
                
            latest = Identity.Id<T>.Parse(lastestOfType.Item2);
            return true;
        }

        protected virtual Id<T> GenerateId<T>(params object[] args) where T : class
        {
            return Identity.Id<T>.New(args);
        }

        void TryRegisterId<T>(string id)
        {
            var candidate = ids.SingleOrDefault(x => x.Item1.IsAssignableFrom(typeof(T)) && x.Item2 == id);

            var newId = Tuple.Create(typeof(T), id);

            if (candidate == null)
            {
                ids.Push(newId);
                return;
            }

            if (!newId.Item1.IsAssignableFrom(candidate.Item1))
            {
                throw new InvalidOperationException(string.Format(
                    "You tried to register a new id '{0}' for type '{1}', but the id already exist and is for non-compatible type '{2}'", 
                    id, newId.Item1, candidate.Item1));
            }
        }

        void AssertAllEventsExpected()
        {
            if (results != null && results.Any())
            {
                Assert(false, () => formatter.Write("Expects no more events").NewLine(), () =>
                {
                    formatter.Write("But found:").NewLine().Indent();
                    foreach (var @event in results)
                    {
                        formatter.Write(@event, new EventFormatter(formatter));
                    }
                    formatter.Unindent();
                });
            }
        }

        void Assert(bool condition, Action writeExpected, Action onFail)
        {
            if (condition)
            {
                formatter.Write(checkmark + " ").Indent();
                writeExpected();
                formatter.Unindent().NewLine();
            }
            else
            {
                formatter.Write(cross + " ").Indent();
                writeExpected();
                formatter.Unindent().NewLine();

                onFail();

                Fail();
            }
        }
    }
}
