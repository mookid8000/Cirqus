# d60 Event Sorcerer

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