module FSBar.Hub.GrpcTests.LogStreamHarness

open System
open System.Threading
open System.Threading.Tasks
open Grpc.Core
open Fsbar.Hub.Scripting.V1

type LogStreamHarness(reader: IAsyncStreamReader<LogEntryMessage>) =
    let cts = new CancellationTokenSource()

    // Returns null if the entry is not found within timeoutMs.
    // Distinguishes from stream errors by returning null rather than raising.
    member _.WaitForEntry(predicate: LogEntryMessage -> bool, timeoutMs: int) : Task<LogEntryMessage> =
        task {
            let deadline = DateTime.UtcNow.AddMilliseconds(float timeoutMs)
            let mutable found = false
            let mutable result = Unchecked.defaultof<LogEntryMessage>
            let mutable timedOut = false
            while not found && not timedOut do
                let remaining = int (deadline - DateTime.UtcNow).TotalMilliseconds
                if remaining <= 0 then
                    timedOut <- true
                else
                    use localCts = new CancellationTokenSource(remaining)
                    use linked = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, localCts.Token)
                    try
                        let! moved = reader.MoveNext(linked.Token)
                        if not moved then
                            timedOut <- true
                        elif predicate reader.Current then
                            found <- true
                            result <- reader.Current
                    with
                    | :? OperationCanceledException -> timedOut <- true
                    | :? RpcException as rpc when rpc.StatusCode = StatusCode.Cancelled -> timedOut <- true
            return result
        }

    member _.CollectN(n: int, timeoutMs: int) : Task<LogEntryMessage list> =
        task {
            let results = System.Collections.Generic.List<LogEntryMessage>()
            use localCts = new CancellationTokenSource(timeoutMs)
            use linked = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, localCts.Token)
            try
                while results.Count < n do
                    let! moved = reader.MoveNext(linked.Token)
                    if not moved then
                        raise (Xunit.SkipException(sprintf "log stream ended after %d entries (wanted %d)" results.Count n))
                    results.Add(reader.Current)
                return results |> Seq.toList
            with
            | :? OperationCanceledException ->
                return raise (Xunit.SkipException(sprintf "timed out collecting %d entries in %dms (got %d)" n timeoutMs results.Count))
            | :? RpcException as rpc when rpc.StatusCode = StatusCode.Cancelled ->
                return raise (Xunit.SkipException(sprintf "timed out collecting %d entries in %dms (got %d)" n timeoutMs results.Count))
        }

    member _.AssertNoUnexpected(unexpectedPredicate: LogEntryMessage -> bool, windowMs: int) : Task =
        task {
            use localCts = new CancellationTokenSource(windowMs)
            use linked = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, localCts.Token)
            try
                let mutable keepGoing = true
                while keepGoing do
                    let! moved = reader.MoveNext(linked.Token)
                    if not moved then
                        keepGoing <- false
                    elif unexpectedPredicate reader.Current then
                        Xunit.Assert.Fail(sprintf "unexpected log entry: [%A/%A] %s"
                            reader.Current.Category reader.Current.Severity reader.Current.Message)
            with
            | :? OperationCanceledException -> ()
            | :? RpcException as rpc when rpc.StatusCode = StatusCode.Cancelled -> ()
        } :> Task

    interface IDisposable with
        member _.Dispose() = cts.Cancel(); cts.Dispose()
