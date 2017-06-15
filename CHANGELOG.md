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

* `Load<TAggregate>
    ` on `AggregateRoot` now loads & caches in the current unit of work, loading & caching the right versions of aggregate roots as well.

    ## 0.0.33

    * Renamed view managers

    ## 0.0.34

    * Divided view manager into push/pull view managers
    * Introduced composite command

    ## 0.0.35

    * Moved event dispatch out of retry loop

    ## 0.1.0

    * Renamed to "Circus" ;)- because when you
say "CQRS" fast enough, that's what it sounds like
    ## 0.1.1
    * Renamed existing e
    vent dispatcher to `ViewManagerEventDispatcher` because it can dispatch events to view managers - that's what makes it special :)
    


   ## 0.1.2
    * Added Azure Service Bus event dispatcher + nuspec
    ## 0.1.3
    * Fixed bug in view managers that could "forget" to upd
    ate `LastGlobalSequenceNumber` on a view - not that will be automatically done
by the `ViewDispatcherHelper`
    ## 0.2.0
    * Renamed to "Cirqus"
- the name that just gets better and better!
    ## 0.2.1
    * Introduced logging + added `ProcessCommand` method to `TestContext`
    ## 0.2.2
    *
    Somemore logging

    

 ## 0.2.3
    * Added NLog integration and added configuration
    option on `Options` to a
llow for configuring logging
    ## 0.2.4
    * Added asynchron
    ous event dispatcher - can be configured by going `.Asynchronous()` on any ordinary dispatcher

## 0.2.5
    * Improved async event dispatcher to use one worker thread per inner dispatcher
    ## 0.3.0

    * Improved `IViewContext` by adding some more context to it (+ ability to load "current" version of an aggregate root - i.e. the global sequence number "roof" is automatically deducted from the domain event currently being han
dled)
    ## 0.4.0
    * Changed `TestContext` to provide a more explicit model for simulating a proper u
    nit of work - can now be accessed by
 going `BeginUnitOfWork` and going all `Commit` and stuff
    ## 0.4.1
    * Fixed bug that would
 result in "forgetting" to invoke the `Created` hook on a new aggregate root when running with the real command processor
    ## 0.4.2
    * Added MongoDB logger factory
    ## 0.4.3
    * Added method to the test conte
    xt that can print the ac
cumulated event history and the emitted events to a text writter, formatted as plain old JSON objects
    ## 0.4.4
    * MIT licensed everything.
    ## 0.5.0
    * Fixed bug that would result in not
 getting a cache hit on 2nd load of the same root from unit of work
    ## 0.6.0
    * Fixed potential odd behavior by having in-mem even
    t store save cloned events instead of the original objects.

    ## 0.6.1

    * Make event stores automatically add event batch ID as a header on all
events  ##
 0.7.0
    * Changed format
    of timestamp metadata to be strings in order to ensure consistent behavior across all event stores + introduced extension method for extracting them

    ## 0.7.1

    * Corrected
spelling in an error message
    ## 0.8.0
    * Changed initialization of async event dispatcher to be async as well
    ## 0.9.0
    * Added Pos
    tgreSQL event store

    ## 0.10.0
    * Changed `TestCon
    text` API so that it can
 return fully hydrated aggregate roots
    ## 0.11.0
    * Made `ProcessCommand` method on `TestC
    ontext` return events th
at were emitted as a result of that command
    ## 0.12.0
    * Removed a lot of generics and reflection
    stuff and made it possible to use the base `Command` to execute logic on arbitrary aggregate roots.

    ## 0.12.1

    * Added `string[]` as a supported property type on `MsSqlViewManager`.  ##
 0.12.2   *
 Allow properties of type `DateTime`, `DateTimeOffset` and `TimeSpan` on `MsSqlViewManager`-managed views
    ## 0.12.3
    * Introduced a fluent configuration
    APIthat will make it easier to discover configuration options + make it harder to end up with e.g. an un-initialized command proce
ssor
    ## 0.13.0
    * Changed logger API to include overloads
    for`Warn` and `Error` that include a real `exception` field
    * Added Serilog integration package

    

 ## 0.14.
    0

    

 * Removed superfluous methods from `ICommandProcessor` interface - it's only ab
    out processing commands n
ow!
    ## 0.14.1
    * Added experimental caching aggregate root repository with a simple in-mem sna
    pshot cache (warning: bet
a!)
    ## 0.15.0
    * Added experimental async-by-default managed views as an alternative to the initial view managers
    ## 0.15.
    1

    

 * Suppor
    t for composite event dispatchers in the configuration API

    ## 0.15.2

    * Added conf
iguration options to Serilog integration
    ## 0.16.0
    * Abi
    lityfor new SQL views to
 have certain propoerties JSON-serialized - just use the `[Json]` attribute on them :)
    * Can now pass
    `ViewManagerWaitHandle` to the new view manager event dispatcher to allow for blocking until certain views have updat
ed
    ## 0.17.0

    * Made `CommandProcessor` and `TestContext` disposable in the hope that someone will dispose them and stop their threads

    ## 0.17.1

    * Comments + more.

    ## 0.17.2
    * Exposed Serilog options on config API
    ## 0.18.0
    * Allow for specifying that certain columns can be `[NotNull]` with the new MsSql vi
    ew manag
er
    ## 0.19.0 * C
hanged `ViewLocator` API to pass the view context, allowing for loading roots during view location
      ##
 0.20.0
    * JSON.NET is now me
    rged into d60.Cirqus, making for an ef
fectively dependency-less core assembly - just how it's supposed to be
    ## 0.20.1
    * Made `Test
    Context` return `CommandProcessingResult` when calling `Save`, so that async views can be blocked until the results are
 visible


    

 ## 0.20.2
    * Fixed bug in `TestContext` that did not correctly serialize the UTC time
    ## 0.20
    .3

    * `NewMsSqlViewManager` can automagically drop & recreate the table when necessary

    ## 0.20.4* Added `HandlerVie
wLocator` that allows for implementing `IGetViewIdsFor<TDomainEvent>
        ` where `TDomainEven
    t` is a domain event or an interface - makes view ID mapping really neat in some situations.


        ## 0.21.0
        This is a big update that completes
    the transition tothe new, vastly improved view managers and makes a load of stuff much more consi
stent.
    * All the ol
d view manager stuff has now been completely replaced by the new view manager.
        * `TestContext` now has an `
    Asynch
ronous` property that can be used to specify that it is to work asynchronously (more realistic with regards to event dispatch).
    *Replaced the `ProcessCommand` method on `TestContext` with one that matched the one on `ICommandProcessor`

    ## 0.22.0

    * Added `InMemoryViewEventDispatcher` which a special in-mem view manager that has events dispatched to it directly - suitable for in-mem, in-process views only (but they're very fast...)

    ## 0.22.1* Fixed bug where loading a nonexistent aggregate root from a view did not throw an exception

    ## 0.22.2

    * Better `ToString` on `CommandProc
essingResult`
        ## 0.23.0
        * Added ability for `MsSqlViewManager`-views to
    "find rest", which is crucial when you
 want to support blocking on a view - a separate table is used to implement this feature
        #
    # 0.24.0* Store current position in separate collection for `MongoDbViewManager` to avoid having to deal with the special position document popping up in query results

    ## 0.24.1
    * Made `InMemoryEventStore` reentrant by serializing access to the inner list of committed event batches

    ## 0.24.2
  * Added experimental TypeScript code generator
        ## 0.24.3
        * Silly `Assembly.LoadFile` must always be called with an
    absolute p
ath
      ##
 0.24.4
        * Map `object` to `any`
        ## 0.25.0
    * Added initial version of an `EventR
eplicator` - can probably be brought to do all kinds of interesting things :)
    * Added `Updated` event to the typed view manager and made all the existing view managers raise the event a<TDomainEvent>
        rVie
        ` where `TDomainEven
        t` i
s a domain event or an interface - makes view ID mapping really neat in some situations.
        ## 0.21.0
        This is a big update that completes the transition to the new, vastly improved view manage
rs and makes a load of stuff much more consistent.
        * Al
        l the old view manager stuff has now been completely replaced by the new view manager.
        * `TestContext` now has an `Asynchronous` property that can be used to specify that it is to work asynchronously (more realistic with regards to event dispatch
).
        * Replaced the `ProcessCommand` method on `TestContext` with
one that matched the one on `ICommandProcessor`
           #
# 0.22.0       * Added `InMemoryViewEventDispatcher` which a special in-mem view manager that has events dispatched to it dire
ctly - suitable for in-mem, in-process views only (but they're very fast...)
        ## 0.22.1
        * Fixed
         bug
 where loading a nonexistent aggregate root from a view did not throw an exception
        ## 0.22.2
        * Better `ToString` on `CommandProcessingResult`
        

      ## 0.23.0
        * Added ability for `MsSqlViewManager`-views to "find rest", which is crucial when you want to support bloc
        king
 on a view - a separate table is used to implement this feature
        ## 0.24.0
        * Store current
        position in separate collection for `MongoDbViewManager` to avoid having to deal with the special position document popping up in query results
        

      ## 0.24.1
        * Made `InMemoryEventStore` reentrant by serializing access to the inner list of committed event batches
        ## 0.24.2
        * Added experimental TypeSc
        ript
 code generator
        ## 0.24.3
        * Silly `Assembly.LoadFile` must always be called with an absolute path       ## 0.24.4

        


       *
        Map
`object` to `any`## 0.25.0* Added initial version of an `
EventReplicator` - can probably be brought to do all kinds of interesting things :)
        * Ad
        ded`Updated` event to the typed view manager and made all the existing view managers raise the event at the right time.
        * Fixed usageof `Nullable<>` on data types like `Guid`, `int`, etc. on `
MsSqlViewManager`
        ## 0.25.1
        * Re-introduced the Entity Framework-based
        view manager - be warned though: it leaves de
referenced child objects in the database with NULL foreign keys
        ## 0.26.0
        * Changed view dispatcher to support polymorphic dispatch - i.e. views can now imp
        lement e.g. `ISubscribeTo<DomainEvent<SomeParticularRoot>>` in order to get everything that happens on `SomeParticularRoot` or `ISubscribeTo<DomainEvent>` to get everything

        ## 0.26.1

        * Changed Entity Framework view manager touse the _sloooow_ OR-mapper-
way of purging data - it's slow, but it cascades to tables with FK constraints and whatnot
## 0.26.2
* Removed annoying log line from `ViewManagerEventDispat
        cher
`
## 0
        .26.3


* Re-publishing because silly NuGet.org failed in the middle of uploading 0.26.2
## 0.27.0
* Moved `TestContext` into cor
        e because it's just easier that way


## 0.28.
        0

        * Remov
ed JSON.NET dependency from MongoDB stuff by merging it in
## 0.29.0
* Validate that collection prop
        erties of Entity Framework views are declared as virtual (otherwise the view might le
ave a trail of non-disconnected should-have-been-orphans in the database)
## 0.30.0
* Added file system-based event store implementat
        ion - thanks [asgerhallas]
        * Added SQLite-bas
ed event store implementation
* Include view positions in timeout exceptio
        n when `TestContext` is waiting for views to catch up


## 0.31.0
* Removed MongoDB dep from nuspec (it didn't actually depend
        on it anyway, since it was merge in)
        
##
 0.32.0
*
        Added cr
ude MongoDB JSON serialization/deserialization mutator hooks
## 0.33.0
* Added simple profiler that can be used
        to record time spent doing various
 core operations
## 0.34.0
* Removed aggregate root repository reference fro
        m aggregate root because it would accidentally avoid decorators and this bypass caching
* Added event dispatch timing to `IProfiler`
## 0.35.0
* Moved file-based event store into core because it has no depend
        encies - thanks [asge
rhallas]
        * Moved SQL Server event store and view manager into core because they too have no dependencie
s
## 0.36.0
* Added an Azure Service Bus Relay-based event store proxy and an `IE
        ventDispatcher` impleme
ntation that is an event store (readonly-)host
## 0.36.1
* Added ability for `EventRepli
        cator`to wait a configurable a
mount of time in the event that an error occurs (chill down, don't spam the logs...)
## 0.36.2
* Fixed bug in configuration API that would always reg
        ister Azure event dispatchers
 as primary
## 0.36.3
* Fixed max message size in ASB relay-based event store proxy
## 0.36.4
* Made `NetTcpRelayBinding` configurable from the outside on ASB relay event store proxy thingie
## 0.40.0
* Huge BREAKING change: Event store abs
        traction does not care about serialization now. It may, however, provide special support for various serialization <DomainEvent<SomeParticularRoot>>` in order to get everything that happens on `SomeParticularRoot` or `ISubscribeTo<DomainEvent>` to get everything

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
* Introduced `IDomainTypeNameMapper` that allows for customizing names of events and aggregate roots as they go into event metadata
* Split `Load` up into `Create`, `TryLoad`, and `Load` - each with appropriate and more explicit behavior
* Relaxed type constraints on `Load` and `TryLoad` methods, allowing for loading as base classes and interfaces
* Optimized `ViewManagerEventDispatcher` to do direct dispatch of events when possible

## 0.46.0

* Made TestContext's event serializer customizable

## 0.46.1

* Added `IAggregateRootRepository` implementation that allows for letting a factory method create the instance - thanks [kimbirkelund]

## 0.46.2

* Made number of domain events per batch configurable in the config API for `ViewManagerEventDispatcher`

## 0.47.0

* Brought back the aggregate root type in the `IProfiler` interface

## 0.48.0

* Slight change in profiler behavior - actuall aggregate root type is registered if possible, otherwise the queries type is used

## 0.49.0

* Fixed pretty subtle bug in `MongoDbEventStore` that surfaces when caching is introduced
* Finished the simple caching event store with a simple age-based eviction strategy

## 0.50.0

* Better way of skipping the unit of work property when generating aggregate root snapshots with the `Sturdylizer`

## 0.51.0

* Added fluent configuration api for TestContext so all dependencies can be switched out or decorated
* Made shortcuts to registration of services in the configuration api and removed the Registrar-property
* Changed the configuration of view managers to a fluent one instead of the plethora of overloads

## 0.52.0

* Automagically add command type name to emitted events
* Fixed bug where unit of work in some circumstances did not cache aggregate roots under their correct global sequence number, thus leading to bad stuff

## 0.53.0

* Moved the auto-added command type name option to a decorator that can be optionally enabled

## 0.54.0

* Added PostgreSQL view manager
* Added testing tools including xUnit and NUnit integration
* Fixed nuget package for NUnit

## 0.55.0

* Fixed missing file dependency in nuget for xunit and nunit

## 0.55.1

* Fixed internal serializer issue in testing harness

## 0.55.2

* Added implicit ids to Emit/Then methods og the test harness
* Added possibility to configure the test context in the test harness

## 0.56.0

* Metadata from commands now flow to emitted events before they are applied to the aggregate root

## 0.57.0

* Added simple profiler hook to `ViewManagerEventDispatcher` - let's see if it turns out to be useful

## 0.58.0

* Made view manager profiler hook more details - now captures time for individual events

## 0.58.1

* Added `StandardViewManagerProfiler` to make simple profiling of views easy

## 0.58.2

* Added `AggregateRootInfo` that can be used as a helper when you want to implement caching

## 0.59.0

* Changed `StandardViewManagerProfiler` model to include number of events with its trackings

## 0.59.1

* Fixed odd missing delegation to inner unit of work in `DefaultViewContext`

## 0.60.0

* Added `Committed` hook to `IUnitOfWork` - will probably turn out to be useful

## 0.60.1

* Made `Sturdylizer` even more sturdy

## 0.60.2

* Made in-mem event cache simpler and trim itself in the background
* Introduced deserialized domain event serializer cache

## 0.60.3

* Enable batching in `EventReplicator`

## 0.60.4

* Fixed odd behavior that would accidentally replay events agains entire pool of view managers when one of them was behind

## 0.60.5

* Fixed support for inherited aggregate roots - thanks [mhertis]
* Fixed subtle bug that would cause waiting for specific view instances to not work

## 0.60.6

* Added support for overriding id generation in test framework

## 0.60.7

* Added support for manipulating the command before executing it in the test framework

## 0.60.8

* Made test harness generate and store IDs for entities

## 0.60.9

* Fixed storing of IDs for entities

## 0.60.10

* support for manipulating the event before emitting it in the test framework

## 0.61.0

* Added support for dependent views - i.e. views that catch up with other views instead of catching up with the event store

## 0.61.1

* Gradually back off from attempting to catch up in the view manager event dispatchers when an error has occurred

## 0.61.2

* Simplified test context setup to support testing the new dependent view event dispatcher

## 0.61.3

* Changed api for test context setup

## 0.61.4

* Added initial HybridDb view manager support
* Fixed metadata flow bug

## 0.61.5

* Added nuspec for HybridDb view manager

## 0.61.6

* Tiny enhancment of HybridDb view manager configuration

## 0.61.7

* Fixed bug that would result in metadata not being set correctly on events emitted in an overridden `Created` method

## 0.61.8

* Fixing configuration in testing harness - again!

## 0.61.9

* Fixing TestContext to handle dependent view managers

## 0.61.10

* Fixing failing test

## 0.62.0

* Added experimental auto-balacing of views among multiple processes (WARNING: not suitable for production just yet)

## 0.62.1

* More intelligent sign-off in automatic view distributor

## 0.62.2

* Enabled batch dispatch capabilities in MongoDB, Postgres, In-mem, and MSSQL view managers

## 0.62.3

* Added debug logging factory - thanks [SamuelDebruyn]

## 0.62.4

* Made dependent view manager event dispatcher actually use the "max domain events per batch" setting

## 0.62.5

* Fixed bug in `MsSqlViewManager` that caused properties of type `bool` and `bool?` to be stored as `NVARCHAR(MAX)`, leading to invalid cast exceptions on update

## 0.62.6

* Added d60.Cirqus.Identity package and added support for Id<T> in the test tools

## 0.62.7

* Fixed failing test

## 0.62.8

* Fxied a misspell in the nuspec

## 0.62.9

* Build script now supports nuspec with multiple Cirqus-dependencies

## 0.62.10

* Can now add custom view context items to ordinary view manager event dispatcher's context

## 0.62.11

* Can now use batched domain events with entityFramework view manager

## 0.63.0

* Introduced `AggregateRootNotFoundException` to let other exceptions that can potentially occur during hydration bubble up properly when `TryLoad`ing

## 0.63.1

* Fixed a bug where dependantviewdispatcher could be in a state with a invalid default MaxDomainEventsPrBatch - set it to 100

## 0.63.2

* Fixed bug where MSSQL event store could return events for an aggregate root out of order (which was pretty unlikely to happen + aggregate roots guard against thist)

## 0.63.3

* Fixed bug in `PostgreSqlViewManager` that would result in never dispatching the first event to anyone - thanks [pvivera]

## 0.63.4

* Fixed missing line breaks in testing tools output
* Added synchronous event dispatcher for testing views and getting sensible exception output

## 0.63.5

* Changes default ID separator to dash (but with an option for slash) to support using IDs directly in URLs
* Added support for a shorter GUID notation for IDs

## 0.63.6

* Fixed a bug in IDs Get-method that I just introduced in 0.63.5

## 0.63.7

* Fixed bug in `CachingEventStoreDecorator` that could result in having many cache trim operations executed in parallel in the background if the execution time exceeds 30 s

## 0.63.8

* Added support for retrieving the type that an id matches by pre-, in- or postfix
* Added support for a shorter GUID notation by default

## 0.63.9

* Upgraded xunit to 2.1

## 0.63.10

* Same as 0.63.9. There was a disturbance on nuget.org that I thought originated from some deploy gone wrong.

## 0.63.11

* Fixed the xunit output that didn't show up

## 0.63.12

* Makes no sense to have reentrant cache trimming in `CachingDomainEventSerializerDecorator`
* Added view type reflection extension on `IViewManager`

## 0.63.13

* Fixed GetHashCode issue on KeyFormat

## 0.63.14

* Fixed TestingHarness for bug when no event was emitted

## 0.63.15

* Making InMemoryEventStore and InMemoryUnitOfWork testing tools public to ease implementing custom event dispatchers

## 0.64.0

* Fixed `Kind` of `DateTime` retrieved by `MsSqlViewManager` to be UTC because that's what it is. The really short version of the description of this problem is: Use `DateTimeOffset`s instead. They are explicit about the fact that they are just a UTC timestamp.
* Added Sunshine Scenario Aggregate Root Load Caching mechanism in `DefaultViewContext` which can provide a nice acceleration in Sunshine Scenarios.
* Updated internal Newtonsoft JSON serializer to 7.0.1

## 0.64.1

* Clean up of the EventDispatcher interface
* Use the configured DomainEventSerializer in the testing tools

## 0.64.2

* Better logging in the event replicator - will now periodically log stats on how many events have been replicated

## 0.64.3

* Made MongoDB event store less eager to fetch data - basically leaves batching to the driver, while still keeping a tight grip on overall paging of the result set

## 0.64.4

* Added logging in the `Retryer` so it is possible to see how many times commands are retried

## 0.64.5

* Added support for testing events not emitted by an aggregate root

## 0.64.6

* Added IDomainEvent as an interface for DomainEvent to enable subscription by custom interfaces while still having access to Meta and ensuring that we are actually dealing with an event (intersection types ala TypeScript would have solved this so elegantly)
* Added an AfterEmit callback to the test harness and renamed the others to a consistent Before/After naming scheme

## 0.64.7

* fixing api for CirqusTestHarness, so a typed id is not required

## 0.64.8

* fixing error in last change

## 0.64.9

* fixing "then" of testing tools to support testing of non aggregate root streams

## 0.64.10

* Asserting on events without When()-call

## 0.64.11

* Fixing xunit test output to work for xunit 2.0

## 0.64.12

* Asserting on events by type only without When()-call

## 0.64.13

* Exposed event store on test context

## 0.64.14

* Added new experimental aggregate root snapshotting mechanism to MongoDB package (can easily be extended to other storages if it turns out to be good)

## 0.64.15

* Tweaked the new MongoDB snapshotting

## 0.64.16

* Tweaked the new MongoDB snapshotting again

## 0.65.0

* Changed new snapshot configuration API to enable other snapshot storages via the `ISnapshotStore` abstraction

## 0.66.0

* Refined API of `ISnapshotStore` and introduced simple in-mem snapshot store
* Made snapshotter do another snapshot after preparation if the preparation 

## 0.66.1

* Further optimization of fast-track: Avoid serializer roundtrip when possible

## 0.66.2

* Optimization of MSSQL event store with drastically improved write performance + slightly improved read performance
* Better error message when an event can suddenly no longer be applied to an aggregate root

## 0.66.3

* Better error message when MSSQL view instance upsert fails

## 0.66.4

* Access to UnitOfWork from ICommandContext + overload on UnitOfWork to emit events from non-aggregate root stream

## 0.67.0

* Updated HybridDB integration to 0.10.0

## 0.67.1

* Fixed ordering in PostgreSQL when loading events for an aggregate root - thanks [enriquein]

## 0.68.0

* Fixed MSSQL event store - avoid `WITH (NOLOCK)` because it is dangerous and fetch batches of 2000 rows

## 0.68.1

* Defensive hydration by `DefaultViewContext` - always checks if in-mem event bactch has something that needs to be applied, which allows for safely loading aggregate roots local events when running on a replicated event store

## 0.69.0

* Add ability to customize `NpgsqlConnection` after its creation (to e.g. supply certificate validation callback) - thanks [enriquein]


[asgerhallas]: https://github.com/asgerhallas
[enriquein]: https://github.com/enriquein
[kimbirkelund]: https://github.com/kimbirkelund
[mhertis]: https://github.com/mhertis
[pvivera]: https://github.com/pvivera
[SamuelDebruyn]: https://github.com/SamuelDebruyn
[ssboisen]: https://github.com/ssboisen
