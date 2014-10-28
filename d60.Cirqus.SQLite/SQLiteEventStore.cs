using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Exceptions;
using d60.Cirqus.Extensions;
using d60.Cirqus.Numbers;
using d60.Cirqus.Serialization;

namespace d60.Cirqus.SQLite
{
    /// <summary>
    /// Implementation of <see cref="IEventStore"/> that uses SQLite to store its events. Please remember to somehow copy the native
    /// sqlite3.dll to the diretory in which your application will be running.
    /// </summary>
    public class SQLiteEventStore : IEventStore, IDisposable
    {
        readonly DomainEventSerializer _domainEventSerializer = new DomainEventSerializer("<events>");
        readonly SQLiteConnection _connection;

        public SQLiteEventStore(string databasePath)
        {
            _connection = EstablishConnection(databasePath);
            _connection.CreateTable<Event>();
        }

        static SQLiteConnection EstablishConnection(string databasePath)
        {
            try
            {
                const SQLiteOpenFlags flags = SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite;

                return new SQLiteConnection(databasePath, flags, storeDateTimeAsTicks: true);
            }
            catch (BadImageFormatException e)
            {
                throw new ApplicationException("Could not initialize SQLite connection - this is probably a sign that you've included sqlite3.dll for the wrong platform. Figure out whether your process is running as x86 or x64 and be sure to include a version of sqlite3.dll that is compiled for that platform", e);
            }
            catch (DllNotFoundException e)
            {
                throw new ApplicationException(string.Format("Unable to load sqlite3.dll - you need to make sure that the sqlite3.dll is present in the runtime directory ({0}) of your program. Figure out whether your process is running as x86 or x64 and be sure to include a version of sqlite3.dll that is compiled for that platform", AppDomain.CurrentDomain.BaseDirectory), e);
            }
            catch (Exception e)
            {
                throw new ApplicationException(string.Format("An error occurred when attempting to initialize SQLite database {0}", databasePath), e);
            }
        }

        class Event
        {
            [PrimaryKey]
            public long GlobalSequenceNumber { get; set; }

            public Guid BatchId { get; set; }

            [Indexed(Name = "aggregateRootIndex", Order = 1, Unique = true)]
            public Guid AggregateRootId { get; set; }

            [Indexed(Name = "aggregateRootIndex", Order = 2, Unique = true)]
            public long SequenceNumber { get; set; }

            public string EventData { get; set; }
        }

        public void Save(Guid batchId, IEnumerable<DomainEvent> batch)
        {
            var nextGlobalSequenceNumber = GetNextGlobalSequenceNumber();

            var domainEventsList = batch.ToList();

            foreach (var domainEvent in domainEventsList)
            {
                domainEvent.Meta[DomainEvent.MetadataKeys.GlobalSequenceNumber] = (nextGlobalSequenceNumber++).ToString(Metadata.NumberCulture);
                domainEvent.Meta[DomainEvent.MetadataKeys.BatchId] = batchId.ToString();
            }

            EventValidation.ValidateBatchIntegrity(batchId, domainEventsList);

            var eventList = domainEventsList
                .Select(e => new Event
                {
                    GlobalSequenceNumber = e.GetGlobalSequenceNumber(),
                    AggregateRootId = e.GetAggregateRootId(),
                    BatchId = batchId,
                    SequenceNumber = e.GetSequenceNumber(),
                    EventData = _domainEventSerializer.Serialize(e)
                })
                .ToList();

            _connection.BeginTransaction();

            try
            {
                foreach (var e in eventList)
                {
                    _connection.Insert(e);
                }

                _connection.Commit();
            }
            catch (SQLiteException sqLiteException)
            {
                _connection.Rollback();

                if (sqLiteException.Result == SQLite3.Result.Constraint)
                {
                    throw new ConcurrencyException(batchId, domainEventsList, sqLiteException);
                }
            }
            catch (Exception)
            {
                _connection.Rollback();
                throw;
            }
            finally
            {
            }
        }

        public IEnumerable<DomainEvent> Load(Guid aggregateRootId, long firstSeq = 0)
        {
            // must be foreach here - SQLite's LINQ thingie does not play well with _domainEventSerializer.Deserialize(e.EventData)
            foreach (var e in _connection.Table<Event>()
                .Where(e => e.AggregateRootId == aggregateRootId)
                .Where(e => e.SequenceNumber >= firstSeq))
            {
                yield return _domainEventSerializer.Deserialize(e.EventData);
            }
        }

        public IEnumerable<DomainEvent> Stream(long globalSequenceNumber = 0)
        {
            // must be foreach here - SQLite's LINQ thingie does not play well with _domainEventSerializer.Deserialize(e.EventData)
            foreach (var e in _connection.Table<Event>()
                .Where(e => e.GlobalSequenceNumber >= globalSequenceNumber))
            {
                yield return _domainEventSerializer.Deserialize(e.EventData);
            }
        }

        public long GetNextGlobalSequenceNumber()
        {
            var eventsTable = _connection.Table<Event>();

            var nextGlobalSequenceNumber = eventsTable.Any()
                ? eventsTable.Max(e => e.GlobalSequenceNumber) + 1
                : 0;

            return nextGlobalSequenceNumber;
        }

        public void Save(Guid batchId, IEnumerable<Events.Event> events)
        {
        }

        public IEnumerable<Events.Event> LoadNew(Guid aggregateRootId, long firstSeq = 0)
        {
            return Enumerable.Empty<Events.Event>();
        }

        public IEnumerable<Events.Event> StreamNew(long globalSequenceNumber = 0)
        {
            return Enumerable.Empty<Events.Event>();
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
