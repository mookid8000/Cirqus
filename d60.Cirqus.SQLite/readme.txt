Sorry about the readme - but when you're using the SQLite event store with Cirqus, you need to
go and make sure that the appropriate sqlite3.dll is copied to the output directory of your
application.

Here's how to do it:

 * "Add existing item" to the root of your project

 * Browse to where the d60.Cirqus.SQLite NuGet package was included (e.g.)
   <your-solution-folder>/packages/d60.Cirqus.SQLite.<version>

 * Find appropriate sqlite3.dll depending on your platform - can be found either
   in lib/x64 or lib/x86 - and include it in your project

 * Make sure that the build action for sqlite3.dll is "Content", and "Copy to output
   directory" is set to "Copy if newer"

That should be it :)

PS: You don't HAVE to use the include sqlite3.dll - you're free to go to http://sqlite.org
and download a newer version if you like.
