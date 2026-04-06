# Contract Delta: proto/highbar/callbacks.proto

**Date**: 2026-04-06

## New Enum Value

```protobuf
enum CallbackId {
  // ... existing values ...

  // Map (new)
  CALLBACK_MAP_GET_CORNERS_HEIGHT_MAP = 59;
}
```

Added after `CALLBACK_MAP_GET_METAL_SPOTS = 58`. Value 59 matches the HighBar V2 proxy (commit 026).
