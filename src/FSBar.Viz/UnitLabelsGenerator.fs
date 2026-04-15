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

    // --- single-character pool ------------------------------------------------

    // Global order in which single-char labels are handed out when name
    // derivation fails. Letters before digits, upper before lower, Latin
    // before Greek. Greek entries are visually distinct from Latin so
    // there is no confusion at a glance.
    let private oneCharPool : string list =
        [ yield! [ for c in 'A' .. 'Z' -> string c ]
          yield! [ for c in 'a' .. 'z' -> string c ]
          yield! [ for c in '0' .. '9' -> string c ]
          yield! [ "Γ"; "Δ"; "Θ"; "Λ"; "Ξ"; "Π"; "Σ"; "Φ"; "Ψ"; "Ω" ]
          yield! [ "γ"; "δ"; "θ"; "λ"; "ξ"; "π"; "φ"; "ψ"; "ω" ] ]

    // Name-derived single-char candidates in preference order:
    //   1. First consonant (title-cased)
    //   2. Any consonant in position order
    //   3. First letter of any kind
    //   4. Any letter in position order
    //   5. Any digit from the name
    let private oneCharCandidatesFromName (rest: string) : string seq =
        seq {
            let letters = rest |> Seq.filter Char.IsLetter |> Seq.toList
            let consonants = letters |> List.filter isConsonant
            match List.tryHead consonants with
            | Some c -> yield string (Char.ToUpperInvariant c)
            | None -> ()
            for c in consonants do yield string (Char.ToUpperInvariant c)
            match List.tryHead letters with
            | Some c -> yield string (Char.ToUpperInvariant c)
            | None -> ()
            for c in letters do yield string (Char.ToUpperInvariant c)
            for c in rest do
                if Char.IsDigit c then yield string c
        }

    // --- two-character fallback pool ------------------------------------------

    let private titleCase2 (a: char) (b: char) =
        let up = Char.ToUpperInvariant a
        let lo = Char.ToLowerInvariant b
        String([| up; lo |])

    let private twoCharCandidates (rest: string) : string seq =
        seq {
            let n = rest.Length
            if n = 0 then ()
            elif n = 1 then
                yield titleCase2 rest.[0] rest.[0]
            else
                // Name-derived consonant pairs first.
                for i in 0 .. n - 1 do
                    for j in i + 1 .. n - 1 do
                        if isConsonant rest.[i] && isConsonant rest.[j] then
                            yield titleCase2 rest.[i] rest.[j]
                for i in 0 .. n - 1 do
                    for j in i + 1 .. n - 1 do
                        if Char.IsLetter rest.[i] && Char.IsLetter rest.[j] then
                            yield titleCase2 rest.[i] rest.[j]
                for i in 0 .. n - 1 do
                    for j in 0 .. n - 1 do
                        if Char.IsLetter rest.[i] && Char.IsDigit rest.[j] && i <> j then
                            yield titleCase2 rest.[i] rest.[j]
                let firstConsonant =
                    rest |> Seq.tryFind isConsonant
                    |> Option.defaultWith (fun () ->
                        rest |> Seq.tryFind Char.IsLetter
                        |> Option.defaultValue rest.[0])
                for d in '0' .. '9' do
                    yield titleCase2 firstConsonant d
                for i in 0 .. n - 1 do
                    if Char.IsLetter rest.[i] then
                        for d in '0' .. '9' do
                            yield titleCase2 rest.[i] d
                // Exhaustive sweep.
                for a in 'a' .. 'z' do
                    for b in 'a' .. 'z' do
                        yield titleCase2 a b
                for a in 'a' .. 'z' do
                    for b in '0' .. '9' do
                        yield titleCase2 a b
        }

    let private firstUnused (candidates: string seq) (used: Set<string>) : string option =
        candidates |> Seq.tryFind (fun c -> not (Set.contains c used))

    let private pickLabel (name: string) (used: Set<string>) : string =
        let rest = stripPrefix name
        match firstUnused (oneCharCandidatesFromName rest) used with
        | Some c -> c
        | None ->
            match oneCharPool |> List.tryFind (fun c -> not (Set.contains c used)) with
            | Some c -> c
            | None ->
                match firstUnused (twoCharCandidates rest) used with
                | Some c -> c
                | None ->
                    let mutable i = 0
                    let mutable attempt = sprintf "X%d" i
                    while Set.contains attempt used do
                        i <- i + 1
                        attempt <- sprintf "X%d" i
                    attempt

    let generate
        (items: (string * MovementShape * FactionId) seq)
        (previous: Map<string, string> option)
        : Map<string, string> =
        let buckets =
            items
            |> Seq.distinctBy (fun (n, _, _) -> n)
            |> Seq.toList
            |> List.groupBy (fun (_, s, f) -> s, f)
            |> List.sortBy fst
            |> List.map (fun (k, xs) ->
                k, xs |> List.map (fun (n, _, _) -> n) |> List.sort)

        let prev = defaultArg previous Map.empty
        let mutable result : Map<string, string> = Map.empty

        for (_key, names) in buckets do
            let mutable used : Set<string> = Set.empty
            // Pass 1 — preserve labels from `previous` where still unique
            // within this bucket.
            for name in names do
                match Map.tryFind name prev with
                | Some old when not (Set.contains old used) ->
                    used <- Set.add old used
                    result <- Map.add name old result
                | _ -> ()
            // Pass 2 — assign everyone else.
            for name in names do
                if not (Map.containsKey name result) then
                    let code = pickLabel name used
                    used <- Set.add code used
                    result <- Map.add name code result
        result
