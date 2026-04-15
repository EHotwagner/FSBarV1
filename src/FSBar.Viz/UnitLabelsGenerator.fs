namespace FSBar.Viz

open System

module UnitLabelsGenerator =

    let private factionPrefixes = [ "armada"; "cortex"; "legion"; "scavengers"; "raptors"
                                    "arm"; "cor"; "leg"; "scav"; "rap" ]

    let private stripPrefix (name: string) : string =
        let lower = name.ToLowerInvariant()
        let m =
            factionPrefixes
            |> List.tryFind (fun p -> lower.StartsWith p && lower.Length > p.Length)
        match m with
        | Some p -> name.Substring(p.Length)
        | None -> name

    let private isConsonant (c: char) =
        let cl = Char.ToLowerInvariant c
        Char.IsLetter cl
        && not (cl = 'a' || cl = 'e' || cl = 'i' || cl = 'o' || cl = 'u')

    let private titleCase2 (a: char) (b: char) =
        let up = Char.ToUpperInvariant a
        let lo = Char.ToLowerInvariant b
        String([| up; lo |])

    let private titleCase3 (a: char) (b: char) (c: char) =
        let up = Char.ToUpperInvariant a
        let lo1 = Char.ToLowerInvariant b
        let lo2 = Char.ToLowerInvariant c
        String([| up; lo1; lo2 |])

    // Enumerate candidate 2-char codes for a bare name, in deterministic
    // order. We walk four passes of increasing looseness so the preferred
    // visual form wins wherever the pool allows:
    //   A: consonant letter pairs (e.g. `Pw` from `pawn`)
    //   B: any letter pairs (e.g. `Ao` from `aorta`)
    //   C: letter + digit drawn from the unit name (`P3` from `arm_lightning_3`)
    //   D: letter + sequential digit (`P0`..`P9`) using the first letter.
    // The 676-code letter-letter pool alone is insufficient for the 953-
    // entry `BarData` catalog; adding digit-suffixed codes lifts the pool
    // to 936 and keeps the 2-char rate above the SC-002 threshold.
    let private candidates2 (rest: string) : string seq =
        seq {
            let n = rest.Length
            if n = 0 then
                ()
            elif n = 1 then
                yield titleCase2 rest.[0] rest.[0]
            else
                // Pass A: consonant-first pairs in position order.
                for i in 0 .. n - 1 do
                    for j in i + 1 .. n - 1 do
                        if isConsonant rest.[i] && isConsonant rest.[j] then
                            yield titleCase2 rest.[i] rest.[j]
                // Pass B: any letter pairs (including vowels).
                for i in 0 .. n - 1 do
                    for j in i + 1 .. n - 1 do
                        if Char.IsLetter rest.[i] && Char.IsLetter rest.[j] then
                            yield titleCase2 rest.[i] rest.[j]
                // Pass C: letter + digit pulled from the name.
                for i in 0 .. n - 1 do
                    for j in 0 .. n - 1 do
                        if Char.IsLetter rest.[i] && Char.IsDigit rest.[j] && i <> j then
                            yield titleCase2 rest.[i] rest.[j]
                // Pass D: first consonant + sequential digit.
                let firstConsonant =
                    rest |> Seq.tryFind isConsonant
                    |> Option.defaultWith (fun () ->
                        rest |> Seq.tryFind Char.IsLetter
                        |> Option.defaultValue rest.[0])
                for d in '0' .. '9' do
                    yield titleCase2 firstConsonant d
                // Pass E: any letter + sequential digit (covers degenerate
                // cases where first-consonant + every digit is taken).
                for i in 0 .. n - 1 do
                    if Char.IsLetter rest.[i] then
                        for d in '0' .. '9' do
                            yield titleCase2 rest.[i] d
                // Pass F: exhaustive alphabetical sweep of the global Aa pool
                // and letter-digit pool. Breaks mnemonic readability for the
                // overflow tail but keeps SC-002's 90% 2-char rate attainable
                // when the name-derived candidates are exhausted.
                for a in 'a' .. 'z' do
                    for b in 'a' .. 'z' do
                        yield titleCase2 a b
                for a in 'a' .. 'z' do
                    for b in '0' .. '9' do
                        yield titleCase2 a b
        }

    let private candidates3 (rest: string) : string seq =
        seq {
            let n = rest.Length
            if n >= 3 then
                for i in 0 .. n - 1 do
                    for j in i + 1 .. n - 1 do
                        for k in j + 1 .. n - 1 do
                            if Char.IsLetter rest.[i] && Char.IsLetter rest.[j] && Char.IsLetter rest.[k] then
                                yield titleCase3 rest.[i] rest.[j] rest.[k]
            elif n = 2 then
                yield titleCase3 rest.[0] rest.[1] rest.[1]
            elif n = 1 then
                yield titleCase3 rest.[0] rest.[0] rest.[0]
        }

    let private firstUnused (candidates: string seq) (used: Set<string>) : string option =
        candidates |> Seq.tryFind (fun c -> not (Set.contains c used))

    let private proposeCode (name: string) (used: Set<string>) : string =
        let rest = stripPrefix name
        match firstUnused (candidates2 rest) used with
        | Some c -> c
        | None ->
            match firstUnused (candidates3 rest) used with
            | Some c -> c
            | None ->
                // Last-resort: pad the raw name with digits.
                let mutable i = 0
                let mutable attempt = sprintf "X%02d" i
                while Set.contains attempt used do
                    i <- i + 1
                    attempt <- sprintf "X%02d" i
                attempt

    let generate (names: string seq) (previous: Map<string, string> option) : Map<string, string> =
        let sorted =
            names
            |> Seq.distinct
            |> Seq.sort
            |> Seq.toList

        // Pass 2 stability — preserve previous labels first where still unique.
        let mutable used : Set<string> = Set.empty
        let mutable result : Map<string, string> = Map.empty

        match previous with
        | Some prev ->
            // Walk names in stable order; if this name had a previous label,
            // claim it if not yet used.
            for name in sorted do
                match Map.tryFind name prev with
                | Some old when not (Set.contains old used) ->
                    used <- Set.add old used
                    result <- Map.add name old result
                | _ -> ()
        | None -> ()

        // Pass 1 — assign any remaining names with fresh candidates.
        for name in sorted do
            if not (Map.containsKey name result) then
                let code = proposeCode name used
                used <- Set.add code used
                result <- Map.add name code result

        result
