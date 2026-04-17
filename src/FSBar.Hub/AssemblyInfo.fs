module FSBar.Hub.AssemblyInfo

open System.Runtime.CompilerServices

// Internal test helpers on `FSBar.Hub.ScriptingHub.ScriptingService`
// (PushTestFrame / AttachTestClient / DetachTestClient / DropCountFor)
// drive the fan-out pump without needing a live gRPC host or BarClient.
// The tests live in a separate assembly, so we open the internal
// surface to it explicitly.
[<assembly: InternalsVisibleTo("FSBar.Hub.Tests")>]
do ()
