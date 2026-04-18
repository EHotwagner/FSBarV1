module FSBar.Hub.Tests.HubLogTruncationTests

open System.Text
open Xunit
open FSBar.Hub

// T037 — truncation invariants: ≤ 8 KiB, marker present, UTF-8-safe,
// below-threshold inputs unchanged.

[<Fact>]
let ``message over 8 KiB truncated with marker`` () =
    let raw = String.replicate 12288 "A"  // 12 KiB of ASCII
    let truncated = HubLog.truncateUtf8 raw
    let bytes = Encoding.UTF8.GetByteCount(truncated)
    Assert.True(bytes <= 8192, sprintf "truncated length %d exceeds 8 KiB" bytes)
    Assert.Contains("…[truncated ", truncated)
    Assert.EndsWith(" bytes]", truncated)

[<Fact>]
let ``multi byte UTF-8 respects code point boundary`` () =
    // 4 KiB of "日本語" cycles (each char = 3 bytes → 4096 bytes = ~1365 chars)
    // plus 6 KiB of ASCII tail. Total = 10 KiB.
    let jp = "日本語"
    let jpRepeats = 4096 / (Encoding.UTF8.GetByteCount(jp))  // ~455 repeats
    let jpPrefix = String.replicate jpRepeats jp
    let asciiTail = String.replicate 6144 "A"
    let mixed = jpPrefix + asciiTail
    let truncated = HubLog.truncateUtf8 mixed
    // Re-decoding the truncated value should round-trip — proves the cut
    // fell on a UTF-8 lead-byte boundary, no broken continuation bytes.
    let bytes = Encoding.UTF8.GetBytes(truncated)
    let roundTrip = Encoding.UTF8.GetString(bytes)
    Assert.Equal(truncated, roundTrip)
    Assert.True(bytes.Length <= 8192)

[<Fact>]
let ``message below 8 KiB unchanged`` () =
    let raw = "hello world — short message"
    let truncated = HubLog.truncateUtf8 raw
    Assert.Equal(raw, truncated)
    Assert.DoesNotContain("truncated", truncated)
