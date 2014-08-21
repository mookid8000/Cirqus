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