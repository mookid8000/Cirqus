# d60 Cirqus

## 0.0.5

* Bam!

## 0.0.6

* Changed default behavior of `Load` from within an aggregate root to throw an exception if a root with the specified type/ID does not exist. The behavior can be overridden by setting `createIfNotExists = true` when loading.
* Implemented proper MongoDB-based catch-up view.

## 0.0.15

* Implemented simple SQL server row-based view
* Views get an `IViewContext` now that they can use to load aggregate roots (including the ability to specify which version to load) 

## 0.0.16

* Gave event dispatcher the ability to initialize itself

## 0.0.17

* Extended `TestContext` with the ability to dispatch events to views
* Made not-intended-for-others-to-use in-mem versions of some stuff internal

## 0.0.18

* Added serializability check to test context

## 0.0.19

* Added serializability check to all current event stores

## 0.0.20

* Fixed it so that loading an aggregate root during event application will result in loading the correct version of that aggregate root

## 0.0.21

* Made an explicit divide (made it possible, at least) between catch-up and direct-dispatch view managers
* Added LINQ capabilities to MongoDB view manager

## 0.0.22

* Introduced `Created` hook that can be overridden on aggregate roots, e.g. to emit the infamous `YayIWasCreated` event.

## 0.0.23

* Made Entity Framework view manager support LINQ as well and removed the need for that silly global sequence numbers table
* Entity Framework view manager automatically adds index to global sequence number column

## 0.0.27

* Did some repair work and did some more stuff as well.

## 0.0.30

* `Load<TAggregate>` on `AggregateRoot` now loads & caches in the current unit of work, loading & caching the right versions of aggregate roots as well.

## 0.0.33

* Renamed view managers

## 0.0.34

* Divided view manager into push/pull view managers
* Introduced composite command

## 0.0.35

* Moved event dispatch out of retry loop

## 0.1.0

* Renamed to "Circus" ;) - because when you say "CQRS" fast enough, that's what it sounds like

## 0.1.1

* Renamed existing event dispatcher to `ViewManagerEventDispatcher` because it can dispatch events to view managers - that's what makes it special :)

## 0.1.2

* Added Azure Service Bus event dispatcher + nuspec

## 0.1.3

* Fixed bug in view managers that could "forget" to update `LastGlobalSequenceNumber` on a view - not that will be automatically done by the `ViewDispatcherHelper`

## 0.2.0

* Renamed to "Cirqus" - the name that just gets better and better!

## 0.2.1

* Introduced logging + added `ProcessCommand` method to `TestContext`

## 0.2.2

* Some more logging

## 0.2.3

* Added NLog integration and added configuration option on `Options` to allow for configuring logging

## 0.2.4

* Added asynchronous event dispatcher - can be configured by going `.Asynchronous()` on any ordinary dispatcher

## 0.2.5

* Improved async event dispatcher to use one worker thread per inner dispatcher

## 0.3.0

* Improved `IViewContext` by adding some more context to it (+ ability to load "current" version of an aggregate root - i.e. the global sequence number "roof" is automatically deducted from the domain event currently being handled)

## 0.4.0

* Changed `TestContext` to provide a more explicit model for simulating a proper unit of work - can now be accessed by going `BeginUnitOfWork` and going all `Commit` and stuff

## 0.4.1

* Fixed bug that would result in "forgetting" to invoke the `Created` hook on a new aggregate root when running with the real command processor

## 0.4.2

* Added MongoDB logger factory

## 0.4.3

* Added method to the test context that can print the accumulated event history and the emitted events to a text writter, formatted as plain old JSON objects

## 0.4.4

* MIT licensed everything.

## 0.5.0

* Fixed bug that would result in not getting a cache hit on 2nd load of the same root from unit of work

## 0.6.0

* Fixed potential odd behavior by having in-mem event store save cloned events instead of the original objects.

## 0.6.1

* Make event stores automatically add event batch ID as a header on all events

## 0.7.0

* Changed format of timestamp metadata to be strings in order to ensure consistent behavior across all event stores + introduced extension method for extracting them

## 0.7.1

* Corrected spelling in an error message

## 0.8.0

* Changed initialization of async event dispatcher to be async as well

## 0.9.0

* Added PostgreSQL event store

## 0.10.0

* Changed `TestContext` API so that it can return fully hydrated aggregate roots

## 0.11.0

* Made `ProcessCommand` method on `TestContext` return events that were emitted as a result of that command

## 0.12.0

* Removed a lot of generics and reflection stuff and made it possible to use the base `Command` to execute logic on arbitrary aggregate roots.

## 0.12.1

* Added `string[]` as a supported property type on `MsSqlViewManager`.

## 0.12.2

* Allow properties of type `DateTime`, `DateTimeOffset` and `TimeSpan` on `MsSqlViewManager`-managed views

## 0.12.3

* Introduced a fluent configuration API that will make it easier to discover configuration options + make it harder to end up with e.g. an un-initialized command processor

## 0.13.0

* Changed logger API to include overloads for `Warn` and `Error` that include a real `exception` field
* Added Serilog integration package

## 0.14.0

* Removed superfluous methods from `ICommandProcessor` interface - it's only about processing commands now!

## 0.14.1

* Added experimental caching aggregate root repository with a simple in-mem snapshot cache (warning: beta!)

## 0.15.0

* Added experimental async-by-default managed views as an alternative to the initial view managers

## 0.15.1

* Support for composite event dispatchers in the configuration API

## 0.15.2

* Added configuration options to Serilog integration

## 0.16.0

* Ability for new SQL views to have certain propoerties JSON-serialized - just use the `[Json]` attribute on them :)
* Can now pass `ViewManagerWaitHandle` to the new view manager event dispatcher to allow for blocking until certain views have updated

## 0.17.0

* Made `CommandProcessor` and `TestContext` disposable in the hope that someone will dispose them and stop their threads

## 0.17.1

* Comments + more.

## 0.17.2

* Exposed Serilog options on config API

## 0.18.0

* Allow for specifying that certain columns can be `[NotNull]` with the new MsSql view manager

## 0.19.0

* Changed `ViewLocator` API to pass the view context, allowing for loading roots during view location

## 0.20.0

* JSON.NET is now merged into d60.Cirqus, making for an effectively dependency-less core assembly - just how it's supposed to be

## 0.20.1

* Made `TestContext` return `CommandProcessingResult` when calling `Save`, so that async views can be blocked until the results are visible


## 0.20.2

* Fixed bug in `TestContext` that did not correctly serialize the UTC time 

## 0.20.3

* `NewMsSqlViewManager` can automagically drop & recreate the table when necessary

## 0.20.4

* Added `HandlerViewLocator` that allows for implementing `IGetViewIdsFor<TDomainEvent>` where `TDomainEvent` is a domain event or an interface - makes view ID mapping really neat in some situations.

## 0.21.0

This is a big update that completes the transition to the new, vastly improved view managers and makes a load of stuff much more consistent.

* All the old view manager stuff has now been completely replaced by the new view manager.
* `TestContext` now has an `Asynchronous` property that can be used to specify that it is to work asynchronously (more realistic with regards to event dispatch).
* Replaced the `ProcessCommand` method on `TestContext` with one that matched the one on `ICommandProcessor`

## 0.22.0

* Added `InMemoryViewEventDispatcher` which a special in-mem view manager that has events dispatched to it directly - suitable for in-mem, in-process views only (but they're very fast...)

## 0.22.1

* Fixed bug where loading a nonexistent aggregate root from a view did not throw an exception

## 0.22.2

* Better `ToString` on `CommandProcessingResult` 

## 0.23.0

* Added ability for `MsSqlViewManager`-views to "find rest", which is crucial when you want to support blocking on a view - a separate table is used to implement this feature

## 0.24.0

* Store current position in separate collection for `MongoDbViewManager` to avoid having to deal with the special position document popping up in query results

## 0.24.1

* Made `InMemoryEventStore` reentrant by serializing access to the inner list of committed event batches

## 0.24.2

* Added experimental TypeScript code generator

## 0.24.3

* Silly `Assembly.LoadFile` must always be called with an absolute path

## 0.24.4

* Map `object` to `any`

## 0.25.0

* Added initial version of an `EventReplicator` - can probably be brought to do all kinds of interesting things :)
* Added `Updated` event to the typed view manager and made all the existing view managers raise the event at the right time.
* Fixed usage of `Nullable<>` on data types like `Guid`, `int`, etc. on `MsSqlViewManager`


## 0.25.1

* Re-introduced the Entity Framework-based view manager - be warned though: it leaves dereferenced child objects in the database with NULL foreign keys

## 0.26.0

* Changed view dispatcher to support polymorphic dispatch - i.e. views can now implement e.g. `ISubscribeTo<DomainEvent<SomeParticularRoot>>` in order to get everything that happens on `SomeParticularRoot` or `ISubscribeTo<DomainEvent>` to get everything

## 0.26.1

* Changed Entity Framework view manager to use the _sloooow_ OR-mapper-way of purging data - it's slow, but it cascades to tables with FK constraints and whatnot

## 0.26.2

* Removed annoying log line from `ViewManagerEventDispatcher`

## 0.26.3

* Re-publishing because silly NuGet.org failed in the middle of uploading 0.26.2

## 0.27.0

* Moved `TestContext` into core because it's just easier that way

## 0.28.0

* Removed JSON.NET dependency from MongoDB stuff by merging it in


## 0.29.0

* Validate that collection properties of Entity Framework views are declared as virtual (otherwise the view might leave a trail of non-disconnected should-have-been-orphans in the database)

## 0.30.0

* Added file system-based event store implementation - thanks [asgerhallas]
* Added SQLite-based event store implementation
* Include view positions in timeout exception when `TestContext` is waiting for views to catch up

## 0.31.0

* Removed MongoDB dep from nuspec (it didn't actually depend on it anyway, since it was merge in)

## 0.32.0

* Added crude MongoDB JSON serialization/deserialization mutator hooks

## 0.33.0

* Added simple profiler that can be used to record time spent doing various core operations

## 0.34.0

* Removed aggregate root repository reference from aggregate root because it would accidentally avoid decorators and this bypass caching
* Added event dispatch timing to `IProfiler` 

## 0.35.0

* Moved file-based event store into core because it has no dependencies - thanks [asgerhallas]
* Moved SQL Server event store and view manager into core because they too have no dependencies

## 0.36.0

* Added an Azure Service Bus Relay-based event store proxy and an `IEventDispatcher` implementation that is an event store (readonly-)host

## 0.36.1

* Added ability for `EventReplicator` to wait a configurable amount of time in the event that an error occurs (chill down, don't spam the logs...)

## 0.36.2

* Fixed bug in configuration API that would always register Azure event dispatchers as primary

## 0.36.3

* Fixed max message size in ASB relay-based event store proxy

## 0.36.4

* Made `NetTcpRelayBinding` configurable from the outside on ASB relay event store proxy thingie

## 0.40.0

* Huge BREAKING change: Event store abstraction does not care about serialization now. It may, however, provide special support for various serialization formats if that makes sense (JSON comes to mind, in MongoDB or Postgres).
* Allow for defaulting to `NullEventDispatcher` if no event dispatcher is configured.

## 0.41.0

* Added ability to configure a custom serializer
* Added standard binary formatter-based binary .NET serializer

## 0.41.1

* Better error messages when JSON serialization/deserialization fails
* Fixed some things around the new `Event` class

## 0.42.0

* Add event type to metadata of events. I sincerely hope that this will be the last change to the persistence format of the events.

## 0.43.0

* Changed aggregate root ID to be `string`s instead of `Guid`s - thanks [asgerhallas]

## 0.44.0

* Added `IsNew` property to aggregate root which allows for determining whether an instance is new or not
* Fixed `Created` that would not fire when creating aggregate roots from within another aggregate root - thanks [asgerhallas]
* Removed the `Guid` ctor on `Command<>` because it's in the way when using R# to generate ctor in subclasses

## 0.44.1

* Fixed TS Client generator to generate a command processor proxy without the "process" in the names - i.e. a command `DoWhatever` will now yield a `doWhatever` method on the command processor proxy

## 0.44.2

* Removed some accidental `Console.WriteLine` in `ViewManagerEventDispatcher` and `HandlerViewLocator`

## 0.45.0

* Re-introduced the command mapper concept, making `Command` the ultimate base class of all commands (which must be explicitly mapped using the command mapper API) - use either `ExecutableCommand` or the generic `Command<TAggregateRoot>` to supply the command action as part of the command
* Prettified some code - thanks [ssboisen]



[asgerhallas]: https://github.com/asgerhallas
[ssboisen]: https://github.com/ssboisen

