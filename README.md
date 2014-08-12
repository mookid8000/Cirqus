# d60 Circus

Simple but powerful event sourcing + CQRS kit.

### How simple?

You do this:

    var processor = new CommandProcessor(...);

(of course satisfying the config's few dependencies), and then you can do this:

    processor.ProcessCommand(myCommand);

and then everything will flow from there.

_more docs will follow ;)_
