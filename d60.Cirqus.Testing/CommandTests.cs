using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using d60.Cirqus.Events;
using EnergyProjects.Application.Infrastructure;
using EnergyProjects.Domain.Model;
using EnergyProjects.Domain.Services;
using EnergyProjects.Tests.Utils;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit.Sdk;

namespace EnergyProjects.Tests
{
    public abstract class CommandTests : EventTests, IDisposable
    {
        IEnumerable<DomainEvent> results;

        protected void When(ApplicationCommand command)
        {
            command.Meta["CompanyId"] = Latest<Company>();
            command.IdGenerator = new FakeIdGenerator(this);

            writer
                .Block("When users:")
                .Write(command);

            results = context.ProcessCommand(command);
        }

        // ReSharper disable once InconsistentNaming
        protected void Throws<T>(ApplicationCommand When) where T : Exception
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

            writer.Block("Then:");

            Assert(
                exceptionThrown is T,
                () => writer.Write("It throws " + typeof (T).Name).NewLine(),
                () =>
                {
                    if (exceptionThrown == null)
                    {
                        writer.Write("But it did not.");
                        return;
                    }

                    writer.Write("But got " + exceptionThrown.GetType().Name).NewLine();
                });


            // consume all events
            results = Enumerable.Empty<DomainEvent>();
        }

        // ReSharper disable once InconsistentNaming
        protected void Throws<T>(string message, ApplicationCommand When) where T : Exception
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

            writer.Block("Then:");

            Assert(
                exceptionThrown is T && exceptionThrown.Message == message, 
                () => writer
                    .Write("It throws " + typeof (T).Name).NewLine()
                    .Indent().Write("Message: \"" + message + "\"").Unindent()
                    .NewLine(),
                () =>
                {
                    if (exceptionThrown == null)
                    {
                        writer.Write("But it did not.");
                        return;
                    }

                    writer.Write("But got " + exceptionThrown.GetType().Name).NewLine()
                        .Indent().Write("Message: \"" + exceptionThrown.Message + "\"").Unindent();
                });

            // consume all events
            results = Enumerable.Empty<DomainEvent>();
        }

        protected void Then<T>() where T:DomainEvent
        {
            formatter.Then<T>();

            var next = results.FirstOrDefault();
            next.ShouldBeOfType<T>();

            // consume one event
            results = results.Skip(1);
        }

        protected void Then(params DomainEvent[] events)
        {
            if (events.Length == 0) return;

            writer.Block("Then:");

            foreach (var pair in results.Zip(events, (actual, expected) => new {actual, expected}))
            {
                var actual = pair.actual;
                var expected = pair.expected;

                var jActual = JObject.FromObject(actual, eventSerializer.CreateSerializer());
                var jExpected = JObject.FromObject(expected, eventSerializer.CreateSerializer());

                Assert(
                    JToken.DeepEquals(jActual, jExpected),
                    () => writer.Write(expected).NewLine(),
                    () =>
                    {
                        writer.Block("But we got this:")
                            .Indent().Write(actual).Unindent()
                            .EndBlock();

                        var differ = new Differ();
                        var diffs = differ.LineByLine(jActual.ToString(), jExpected.ToString());
                        var diff = differ.PrettyLineByLine(diffs);

                        writer
                            .NewLine().NewLine()
                            .Write("Diff:").NewLine()
                            .Write(diff);
                    });
            }

            // consume events
            results = results.Skip(events.Length);
        }

        protected void ThenNo<T>() where T:DomainEvent
        {
            writer.Block("Then:");

            var eventsOfType = results.OfType<T>().ToList();

            Assert(
                !eventsOfType.Any(), 
                () => writer.Write(string.Format("No {0} is emitted", typeof (T).Name)),
                () =>
                {
                    writer.Block("But we got this:");
                    foreach (var @event in eventsOfType)
                    {
                        writer.Write(@event).NewLine();
                    }
                    writer.EndBlock();
                });

            // consume all events
            results = Enumerable.Empty<DomainEvent>();
        }


        public void Dispose()
        {
            // only if we are _not_ in an exceptional state
            if (Marshal.GetExceptionCode() == 0)
            {
                AssertAllEventsExpected();
            }
        }

        void AssertAllEventsExpected()
        {
            if (results != null && results.Any())
            {
                Assert(false, () => writer.Write("Expects no more events").NewLine(), () =>
                {
                    writer.Write("But found:").NewLine().Indent();
                    foreach (var @event in results)
                    {
                        writer.Write(@event);
                    }
                    writer.Unindent();
                });
            }
        }

        void Assert(bool condition, Action writeExpected, Action onFail)
        {
            if (condition)
            {
                writer.Write(checkmark + " ").Indent();
                writeExpected();
                writer.Unindent().NewLine();
            }
            else
            {
                writer.Write(cross + " ").Indent();
                writeExpected();
                writer.Unindent().NewLine();

                onFail();

                throw new AssertException("");
            }
        }

        public class FakeIdGenerator : IdGenerator
        {
            readonly CommandTests self;

            public FakeIdGenerator(CommandTests self)
            {
                this.self = self;
            }

            public override Id<T> NewId<T>(params object[] args)
            {
                return self.NewId<T>(args);
            }
        }
    }
}