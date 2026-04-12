// bots/trainer/helpers/prelude.fsx — #r references and opens shared by the bot and helpers
//
// Loads FSBar.Client and its transitive dependencies from the test project's bin directory,
// which has all of them resolved into one place (unlike src/FSBar.Client/bin/ which only
// carries FSBar.Client.dll + FSBar.Proto.dll). This is the same pattern the FSI MCP server
// uses and is documented in CLAUDE.md → "Loading FSBar assemblies in FSI".
//
// Rebuild with `dotnet build src/FSBar.Client.Tests/FSBar.Client.Tests.fsproj -c Debug`
// before running the bot after any `src/FSBar.Client/*.fs` change.

#r "../../../src/FSBar.Client.Tests/bin/Debug/net10.0/Google.Protobuf.dll"
#r "../../../src/FSBar.Client.Tests/bin/Debug/net10.0/FsGrpc.dll"
#r "../../../src/FSBar.Client.Tests/bin/Debug/net10.0/NodaTime.dll"
#r "../../../src/FSBar.Client.Tests/bin/Debug/net10.0/BarData.dll"
#r "../../../src/FSBar.Client.Tests/bin/Debug/net10.0/FSBar.Proto.dll"
#r "../../../src/FSBar.Client.Tests/bin/Debug/net10.0/FSBar.Client.dll"

open FSBar.Client
open FSBar.Client.Commands
open FSBar.Client.Callbacks
open FSBar.Client.MapQuery

printfn "[trainer] prelude loaded — FSBar.Client dlls referenced"
