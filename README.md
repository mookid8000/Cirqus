# d60 Event Sorcerer

Simple but powerful event sourcing + CQRS kit.

### How simple?

You do this:

    var sorcerer = new EventSorcererConfig(...);

(of course satisfying the config's few dependencies), and then you can do this:

    sorcerer.ProcessCommand(myCommand);

and then everything will flow from there.

_more docs will follow ;)_
