// 15-list-supported-maps.fsx — Print the canonical map names in
// FSBar.Client.MapCacheFile.supportedMaps, one per line. Used by
// bots/trainer/map-cache/refresh-all.sh as the single source of truth
// for the supported-map set (FR-008).

#load "../prelude.fsx"

open FSBar.Client

// Prefix each line with "MAP:" so bots/trainer/map-cache/refresh-all.sh
// can filter out NuGet restore noise that dotnet fsi emits on stdout.
for m in MapCacheFile.supportedMaps do
    printfn "MAP:%s" m.MapName
