# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build

This is a C# WinForms application targeting .NET Framework 4.6.1. Build from Visual Studio (VS2017+) by opening `测量2026.sln` or the `.csproj` directly. No command-line build tooling is configured.

**Before rebuilding**, close any running instance of `测量2026.exe` — MSBuild cannot overwrite the output exe while it's running.

**Dependencies** (beyond standard .NET): Halcon 17.12 (`halcondotnet.dll`), Hikvision MvCameraControl, `System.Web.Extensions` (for JSON serialization in FeatureEncoding).

## Project structure

- **`Form1`** — Main window. Camera device enumeration, open/close, continuous grabbing with display on `HSmartWindowControl`. Exposure/gain controls. Template matching (`FindScaledShapeModel`) and real-time blob analysis (`RunBlobAnalysis`) run per-frame in the grab thread. Buttons: `bnTemplate`, `bnBlob`, `bnBlobLive`. FPS counter in title bar.
- **`FormTemplate`** — Template acquisition popup. Grabs current image from Form1, interactive rectangular ROI via Halcon drawing object, creates scaled shape model (`CreateScaledShapeModel`), saves as `.shm`. Shows thumbnail on Form1's `pbTemplate` via `GenerateThumbnailFromWindow`.
- **`FormBlob`** — Blob analysis popup. Grabs current image, runs 16-step pipeline, encodes features as JSON, and opens the matching form. Buttons: 抓取图像, 保存图像, Blob分析, 编码, 匹配.
- **`FormMatch`** — CAD matching form. Shows image encoding JSON (left panel), loads a folder of CAD JSON encodings, runs 4-stage matching pipeline, displays ranked results in a DataGridView (right panel). Click a result row to view that CAD file's encoding.
- **`FeatureEncoding.cs`** — Data model for the encoding system (`L`, `W`, list of `FeaturePoint`). Uses `JavaScriptSerializer` for JSON. `FilePath` field (ScriptIgnored) stores origin file path for CAD encodings.
- **`Matcher.cs`** — Matching algorithm. `MatchResult` class stores per-file scores. `Matcher` class holds all tunable weights/tolerances/thresholds, the Hungarian algorithm, and the 4-stage `Match()` pipeline.
- **`MVCamera.cs`** — Hikvision MvCameraControl.dll P/Invoke wrapper (namespace `MvCamCtrl.NET`, class `MyCamera`). ~4800 lines.
- **`Program.cs`** — Standard WinForms entry point, runs `Form1`.

## Halcon 17.12 API rules

This project uses **Halcon 17.12 Progress** (`C:\Program Files\MVTec\HALCON-17.12-Progress\bin\dotnet35\halcondotnet.dll`).

### Drawing objects use HTuple handles, NOT HDrawingObject instances

`HDrawingObject` as an instantiable class does not exist in 17.12:
```csharp
HTuple hv_DrawObj;
HOperatorSet.CreateDrawingObjectRectangle1(r1, c1, r2, c2, out hv_DrawObj);
HOperatorSet.SetDrawingObjectParams(hv_DrawObj, "color", "red");
HOperatorSet.AttachDrawingObjectToWindow(hwindow, hv_DrawObj);
// Read back
HTuple paramNames = new HTuple("row1", "column1", "row2", "column2");
HTuple paramValues;
HOperatorSet.GetDrawingObjectParams(hv_DrawObj, paramNames, out paramValues);
// Cleanup
HOperatorSet.DetachDrawingObjectFromWindow(hwindow, hv_DrawObj);
HOperatorSet.ClearDrawingObject(hv_DrawObj);
```
- Check handle validity with `hv_DrawObj.Length > 0`, never `null`.
- Use `ClearDrawingObject()` not `Dispose()`.

### General Halcon 17.12 conventions

- `HOperatorSet.GenEmptyObj(out ho_Image)` before first use of any `HObject`.
- Image creation: `GenImage1Extern()` (mono), `GenImageInterleaved()` (color RGB).
- Always `Dispose()` all temporary HObjects in a `finally` block.
- `HOperatorSet.SetColor(hwindow, ...)` + `HOperatorSet.DispObj(...)` for overlay drawing — never instance methods on HWindow.
- `DispText(hwindow, text, coordSystem, row, col, color, box, shadow)` for text overlays.
- `AreaCenter`, `Circularity`, `EllipticAxis` for region feature extraction.
- `ZoomImageSize` requires 5th parameter `interpolation` (e.g. `"constant"`).
- `ReduceDomain` keeps full matrix; use `CropDomain` to physically trim.
- `CountChannels` → branch `GetImagePointer1` (gray) vs `GetImagePointer3` (RGB).

## Camera integration (Hikvision SDK)

- `MV_CC_EnumDevices_NET(MV_GIGE_DEVICE | MV_USB_DEVICE)` for enumeration.
- After open: `AcquisitionMode=2` (continuous), `TriggerMode=0` (off).
- Background thread `ReceiveImageWorkThread` handles grabbing. Live image stored in `Form1.Hobj`.
- `Form1.GetCurrentImage()` returns `Hobj.Clone()` — caller owns disposal.
- Pixel conversion: Mono8 passed directly; other formats converted via `MV_CC_ConvertPixelType_NET`.
- `MouseWheel` events need screen-to-control coordinate translation before forwarding.

## Template matching pipeline

1. `FormTemplate` creates model: `CreateScaledShapeModel` → `GetShapeModelContours` → `WriteShapeModel` to `%TEMP%\测量2026_template.shm`.
2. `Form1.LoadMatchModel()` loads its own copy (`hv_MatchModelID`, `ho_MatchContours`).
3. Per-frame: `FindScaledShapeModel` (0-360°, scale 0.6-1.4, min score 0.5).
4. Transformed contours drawn in yellow via `VectorAngleToRigid` + `HomMat2dScale`.

Thumbnail: `CropDomain` → `ZoomImageSize` → `HImageToBitmap` → `parentForm.SetTemplateThumbnail()`.

## Blob analysis and encoding system

### Pipeline (FormBlob.cs & Form1.cs RunBlobAnalysis)

1. `MedianImage` (circle 1) → 2. `Threshold` (100-255) → 3. `ClosingCircle` (3) → 4. `Connection` → 5. `FillUp` → 6. `SelectShape` (area 200000-999999) → 7. `SmallestRectangle2` → 8. `GenRectangle2ContourXld` (**green**) → 9. `GenRegionContourXld` (filled) → 10. `ReduceDomain` → 11. `Threshold` (0-40) → 12. `OpeningCircle` (7) → 13. `Connection` → 14. `SelectShape` (area 25000-99999) → 15. `SelectShape` circularity 0.8-1.0 (**red**) → 16. `SelectShape` rectangularity 0.9-1.0 (**blue**)

Form1's `RunBlobAnalysis` also displays board length/width in top-left corner via `DispText`.

### Encoding (FormBlob "编码" button)

Extracts ALL holes from `ho_bigRegions` (step 14, before circular/rectangular split — captures ellipses, semi-circles, etc.):

| Operator | Feature |
|----------|---------|
| `AreaCenter` | area, row, column |
| `Circularity` | circularity |
| `EllipticAxis` | Ra, Rb, Phi |

Computes per-feature:
- **Normalized coords**: `(cx, cy)` in board local frame (origin = board center, axes aligned to Length1/Length2, range [-0.5, 0.5])
- **Anisometry**: Ra / Rb
- **Bulkiness**: area / (π × Ra × Rb)

Output: JSON copied to clipboard, `FeatureEncoding` object stored in `m_LastEncoding`.

### Matching (FormMatch — "匹配" button in FormBlob → opens FormMatch)

**CAD library**: folder of `.json` files, each generated from CAD drawings with the same encoding format.

**4-stage pipeline** (`Matcher.Match()`):

| Stage | Filter | Effect |
|-------|--------|--------|
| 1 | L ± max(2mm, 0.5%) AND W ± max(2mm, 0.5%) | ~3000 → ~40 |
| 2 | Feature count ±1 | ~40 → ~8 |
| 3 | Hungarian optimal assignment on weighted cost matrix | per-candidate score |
| 4 | Total score sort + threshold check (T_MATCH=0.50) | best match |

**Feature cost** (per hole pair): `W_POS × d_pos + W_SIZE × d_size + W_ANIS × d_anis + W_CIRC × d_circ`

| Weight | Value | Rationale |
|--------|-------|-----------|
| W_POS | 0.50 | Normalized coords, least affected by illumination |
| W_SIZE | 0.25 | Elliptic axes Ra/Rb + area |
| W_ANIS | 0.15 | Ra/Rb ratio, shape-stable |
| W_CIRC | 0.10 | Circularity, sensitive to lighting |

All weights and tolerances are public fields on `Matcher`, tunable without recompiling the algorithm.

**Hungarian algorithm**: O(n³), handles rectangular cost matrices with dummy padding (unmatched penalty P_UNMATCHED=0.30).

## Form lifecycle

- `Form1.FormClosing` → stop grab, close device, dispose `hv_MatchModelID` and `ho_MatchContours`.
- `FormTemplate.FormClosing` → detach drawing object, clear shape model, dispose HObjects.
- `FormBlob.FormClosing` → dispose `ho_Image`.
- `SetCtrlWhenOpen()` / `SetCtrlWhenClose()` on Form1 manage button enable states — always add new buttons to both.

## Button state toggle pattern (Form1)

Toggle buttons (`bnBlobLive`, etc.) follow this pattern:
```csharp
m_BlobEnabled = !m_BlobEnabled;
if (m_BlobEnabled) { /* green, "停止X" */ }
else { /* gray, "实时X" */ }
```
State reset in `SetCtrlWhenClose()`.

## Git versioning

- Local git repo at `D:\claudepj\测量2026\.git`. Tags `v1.10`, `v1.11`.
- **Rollback:** `git checkout v1.11`
