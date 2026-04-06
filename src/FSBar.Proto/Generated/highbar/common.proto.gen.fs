namespace rec Highbar
open FsGrpc.Protobuf
open Google.Protobuf
#nowarn "40"
#nowarn "1182"


[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Vector3 =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable X: float32 // (1)
            val mutable Y: float32 // (2)
            val mutable Z: float32 // (3)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.X <- ValueCodec.Float.ReadValue reader
            | 2 -> x.Y <- ValueCodec.Float.ReadValue reader
            | 3 -> x.Z <- ValueCodec.Float.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.Vector3 = {
            X = x.X
            Y = x.Y
            Z = x.Z
            }

/// <summary>Shared 3D position vector</summary>
type private _Vector3 = Vector3
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type Vector3 = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("x")>] X: float32 // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("y")>] Y: float32 // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("z")>] Z: float32 // (3)
    }
    with
    static member Proto : Lazy<ProtoDef<Vector3>> =
        lazy
        // Field Definitions
        let X = FieldCodec.Primitive ValueCodec.Float (1, "x")
        let Y = FieldCodec.Primitive ValueCodec.Float (2, "y")
        let Z = FieldCodec.Primitive ValueCodec.Float (3, "z")
        // Proto Definition Implementation
        { // ProtoDef<Vector3>
            Name = "Vector3"
            Empty = {
                X = X.GetDefault()
                Y = Y.GetDefault()
                Z = Z.GetDefault()
                }
            Size = fun (m: Vector3) ->
                0
                + X.CalcFieldSize m.X
                + Y.CalcFieldSize m.Y
                + Z.CalcFieldSize m.Z
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: Vector3) ->
                X.WriteField w m.X
                Y.WriteField w m.Y
                Z.WriteField w m.Z
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.Vector3.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeX = X.WriteJsonField o
                let writeY = Y.WriteJsonField o
                let writeZ = Z.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: Vector3) =
                    writeX w m.X
                    writeY w m.Y
                    writeZ w m.Z
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : Vector3 =
                    match kvPair.Key with
                    | "x" -> { value with X = X.ReadJsonField kvPair.Value }
                    | "y" -> { value with Y = Y.ReadJsonField kvPair.Value }
                    | "z" -> { value with Z = Z.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _Vector3.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._Vector3.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module UnitRef =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.UnitRef = {
            UnitId = x.UnitId
            }

/// <summary>Reference to a specific unit by engine ID</summary>
type private _UnitRef = UnitRef
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type UnitRef = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<UnitRef>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        // Proto Definition Implementation
        { // ProtoDef<UnitRef>
            Name = "UnitRef"
            Empty = {
                UnitId = UnitId.GetDefault()
                }
            Size = fun (m: UnitRef) ->
                0
                + UnitId.CalcFieldSize m.UnitId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: UnitRef) ->
                UnitId.WriteField w m.UnitId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.UnitRef.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: UnitRef) =
                    writeUnitId w m.UnitId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : UnitRef =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _UnitRef.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._UnitRef.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module CommandOptions =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Bitfield: uint32 // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Bitfield <- ValueCodec.UInt32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.CommandOptions = {
            Bitfield = x.Bitfield
            }

/// <summary>
/// Command options bitfield
/// Bits: SHIFT_KEY=1, CTRL_KEY=2, ALT_KEY=4, META_KEY=8,
///       INTERNAL_ORDER=16, RIGHT_MOUSE_KEY=32
/// </summary>
type private _CommandOptions = CommandOptions
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type CommandOptions = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("bitfield")>] Bitfield: uint32 // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<CommandOptions>> =
        lazy
        // Field Definitions
        let Bitfield = FieldCodec.Primitive ValueCodec.UInt32 (1, "bitfield")
        // Proto Definition Implementation
        { // ProtoDef<CommandOptions>
            Name = "CommandOptions"
            Empty = {
                Bitfield = Bitfield.GetDefault()
                }
            Size = fun (m: CommandOptions) ->
                0
                + Bitfield.CalcFieldSize m.Bitfield
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: CommandOptions) ->
                Bitfield.WriteField w m.Bitfield
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.CommandOptions.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeBitfield = Bitfield.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: CommandOptions) =
                    writeBitfield w m.Bitfield
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : CommandOptions =
                    match kvPair.Key with
                    | "bitfield" -> { value with Bitfield = Bitfield.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _CommandOptions.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._CommandOptions.Proto.Value.Empty

