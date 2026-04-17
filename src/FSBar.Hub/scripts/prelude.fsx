// src/FSBar.Hub/scripts/prelude.fsx
//
// Single `#load`-able entry point for FSBar.Hub scripting sessions
// (constitution §V — every packable library must ship a prelude +
// numbered examples).
//
// Rather than enumerating transitive DLL dependencies (FsGrpc,
// Grpc.Net.Common, Microsoft.Extensions.Logging.Abstractions, ...),
// this prelude points at the bin directory of `FSBar.Hub.App`,
// which reifies every transitive reference — including the gRPC
// client stack used by the `03-launch-and-stream` example — into
// one folder. Build the app first:
//
//     dotnet build src/FSBar.Hub.App/FSBar.Hub.App.fsproj
//
// and then load this prelude from any example:
//
//     #load "/home/developer/projects/FSBarV1/src/FSBar.Hub/scripts/prelude.fsx"
//     open FSBar.Hub
//     open Fsbar.Hub.Scripting.V1

// `#r` in net10 FSI resolves relative to the *invoking* script.
// Absolute paths bypass the `../` arithmetic that would otherwise
// break when this prelude is `#load`-ed from varying nesting depths.
//
// The hub app's bin/ directory carries our first-party DLLs + the
// FsGrpc / BarData / Proto chain. gRPC client + logging abstractions
// come from the NuGet cache because ASP.NET Core's shared framework
// reifies them at runtime rather than copying them into bin/ (so an
// `ls bin/` won't show them).
#I @"/home/developer/projects/FSBarV1/src/FSBar.Hub.App/bin/Debug/net10.0"
#r "BarData.dll"
#r "FsGrpc.dll"
#r "FSBar.Proto.dll"
#r "FSBar.Client.dll"
#r "FSBar.Hub.dll"

#r "/home/developer/.nuget/packages/google.protobuf/3.27.0/lib/net5.0/Google.Protobuf.dll"
#r "/home/developer/.nuget/packages/microsoft.extensions.logging.abstractions/6.0.0/lib/net6.0/Microsoft.Extensions.Logging.Abstractions.dll"
#r "/home/developer/.nuget/packages/grpc.core.api/2.67.0/lib/netstandard2.1/Grpc.Core.Api.dll"
#r "/home/developer/.nuget/packages/grpc.net.common/2.67.0/lib/net6.0/Grpc.Net.Common.dll"
#r "/home/developer/.nuget/packages/grpc.net.client/2.67.0/lib/net6.0/Grpc.Net.Client.dll"

printfn "[fsbar-hub] prelude loaded — FSBar.Hub + FSBar.Proto + Grpc.Net.Client available"
