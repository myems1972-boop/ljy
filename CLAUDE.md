# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build

This is a C# WinForms application targeting .NET Framework 4.8. Build from Visual Studio (VS2017+) by opening `测量2026.sln` or the `.csproj` directly. No command-line build tooling is configured.

**Before rebuilding**, close any running instance of `测量2026.exe` — MSBuild cannot overwrite the output exe while it's running.

**Dependencies**: Halcon 17.12 (`halcondotnet.dll`), Hikvision MvCameraControl, `System.Web.Extensions` (JSON), `System.Configuration` (app.config), `System.Data.SqlClient` (MSSQL), **ACadSharp 3.5.7** (NuGet, DWG reading).

## Project structure

- **`Form1`** — Main window. Camera enumeration, open/close, continuous grabbing on background thread. Exposure/gain controls. Template matching and real-time blob analysis run per-frame. FPS counter in title bar.
- **`FormTemplate`** — Template creation popup. Interactive ROI → scaled shape model → `.shm` save.
- **`FormBlob`** — Blob analysis popup. Grab image → 16-step pipeline → encode features → open matching form. Buttons: 抓取图像, 保存图像, Blob分析, 编码, 匹配.
- **`FormMatch`** — Matching form. Dual-mode: **file mode** (load JSON folder or import DWG) and **database mode** (connect MSSQL). Runs 4-stage matching, displays ranked results in DataGridView. Click result row to view CAD encoding. "导入DWG" button extracts FeatureEncoding from .dwg files.
- **`FeatureEncoding.cs`** — Data model (`L`, `W`, `List<FeaturePoint>`). Uses `JavaScriptSerializer` for JSON. `FilePath` field (`[ScriptIgnore]`) tracks origin file.
- **`Matcher.cs`** — `MatchResult` class + `Matcher` class with Hungarian algorithm and 4-stage pipeline. All weights/tolerances/thresholds are public fields, tunable at runtime.
- **`CadRepository.cs`** — MSSQL access layer. Table creation, parameterized import (`SqlBulkCopy`), Stage 1+2 SQL filtering, bulk feature loading, full `MatchFromDb()` pipeline. Owns `SqlConnection` lifecycle.
- **`DwgExtractor.cs`** — Static class that extracts `FeatureEncoding` from .dwg CAD files using ACadSharp. Reads DWG entities, identifies board outline (largest closed entity), extracts holes with analytic geometry calculations (area, perimeter, centroid via Green's theorem, circularity, elliptic axes via PCA on boundary samples). Normalizes coordinates to board-local frame matching FormBlob's Halcon formula. Pure geometry — no Halcon dependency.
- **`MVCamera.cs`** — Hikvision MvCameraControl P/Invoke wrapper (namespace `MvCamCtrl.NET`, class `MyCamera`). ~4800 lines.
- **`Program.cs`** — Entry point, runs `Form1`.

## Halcon 17.12 API rules

This project uses **Halcon 17.12 Progress** (`C:\Program Files\MVTec\HALCON-17.12-Progress\bin\dotnet35\halcondotnet.dll`).

### Drawing objects use HTuple handles, NOT HDrawingObject

`HDrawingObject` as a class does not exist in 17.12. All drawing object ops are `HOperatorSet` static methods with `HTuple` handles:
```csharp
HTuple hv_DrawObj;
HOperatorSet.CreateDrawingObjectRectangle1(r1, c1, r2, c2, out hv_DrawObj);
HOperatorSet.SetDrawingObjectParams(hv_DrawObj, "color", "red");
HOperatorSet.AttachDrawingObjectToWindow(hwindow, hv_DrawObj);
// Read back
HTuple pn = new HTuple("row1", "column1", "row2", "column2"), pv;
HOperatorSet.GetDrawingObjectParams(hv_DrawObj, pn, out pv);
// Cleanup
HOperatorSet.DetachDrawingObjectFromWindow(hwindow, hv_DrawObj);
HOperatorSet.ClearDrawingObject(hv_DrawObj);
```
- Check validity: `hv_DrawObj.Length > 0`, never `null`.
- `ClearDrawingObject()` not `Dispose()`.

### General Halcon 17.12 conventions

- `GenEmptyObj(out ho)` before first use of any `HObject`.
- Image creation: `GenImage1Extern()` (mono), `GenImageInterleaved()` (color RGB).
- Always `Dispose()` temporaries in a `finally` block.
- Overlay drawing: `HOperatorSet.SetColor(hwindow, ...)` + `HOperatorSet.DispObj(...)`.
- Text overlay: `DispText(hwindow, text, coordSystem, row, col, color, box, shadow)`.
- Region features: `AreaCenter`, `Circularity`, `EllipticAxis`.
- `ZoomImageSize` requires 5th param `interpolation` (e.g. `"constant"`).
- `ReduceDomain` keeps full matrix; `CropDomain` physically trims.
- `CountChannels` → branch `GetImagePointer1` vs `GetImagePointer3` for Bitmap conversion.

## Camera integration (Hikvision SDK)

- `MV_CC_EnumDevices_NET(MV_GIGE_DEVICE | MV_USB_DEVICE)` enumeration.
- After open: `AcquisitionMode=2` (continuous), `TriggerMode=0` (off).
- `ReceiveImageWorkThread` on background thread. Live image: `Form1.Hobj`.
- `Form1.GetCurrentImage()` returns `Hobj.Clone()` — caller owns disposal.
- Pixel conversion: Mono8 direct; others via `MV_CC_ConvertPixelType_NET`.
- `MouseWheel` needs screen-to-control coordinate translation before forwarding.

## Template matching

1. `FormTemplate`: `CreateScaledShapeModel` → `GetShapeModelContours` → `WriteShapeModel` to `%TEMP%\测量2026_template.shm`.
2. `Form1.LoadMatchModel()` loads copy (`hv_MatchModelID`, `ho_MatchContours`).
3. Per-frame: `FindScaledShapeModel` (0-360°, scale 0.6-1.4, min score 0.5).
4. Transformed contours drawn in yellow via `VectorAngleToRigid` + `HomMat2dScale`.

## Blob analysis and encoding

### Pipeline (FormBlob.cs & Form1.cs RunBlobAnalysis)

1. `MedianImage` (circle 1) → 2. `Threshold` (100-255) → 3. `ClosingCircle` (3) → 4. `Connection` → 5. `FillUp` → 6. `SelectShape` (area 200k-999k) → 7. `SmallestRectangle2` → 8. `GenRectangle2ContourXld` (**green**) → 9. `GenRegionContourXld` (filled) → 10. `ReduceDomain` → 11. `Threshold` (0-40) → 12. `OpeningCircle` (7) → 13. `Connection` → 14. `SelectShape` (area 25k-100k) → 15. `SelectShape` circularity 0.8-1.0 (**red**) → 16. `SelectShape` rectangularity 0.9-1.0 (**blue**)

Form1 also displays board L/W in top-left via `DispText`.

### Encoding ("编码" button in FormBlob)

Extracts ALL holes from `ho_bigRegions` (step 14, before shape split — captures ellipses, semi-circles, etc.):

| Operator | Output |
|----------|--------|
| `AreaCenter` | area, row, col |
| `Circularity` | circularity [0,1] |
| `EllipticAxis` | Ra, Rb, Phi |

Computes: normalized coords `(cx,cy)` in board local frame (origin=center, axes along Length1/Length2, range [-0.5,0.5]), anisometry=Ra/Rb, bulkiness=area/(π×Ra×Rb). Output: JSON to clipboard + `FeatureEncoding` stored in `m_LastEncoding`.

## Matching system (FormMatch + Matcher + CadRepository)

### 4-stage pipeline (Matcher.Match)

| Stage | Filter | ~Cards |
|-------|--------|--------|
| 1 | L ± max(2mm, 0.5%) AND W ± max(2mm, 0.5%) | 3000→40 |
| 2 | Feature count ±1 | 40→8 |
| 3 | Hungarian assignment on weighted cost matrix | per-candidate |
| 4 | Sort by total score + threshold T_MATCH=0.50 | best match |

**Cost per hole pair**: `W_POS×d_pos + W_SIZE×d_size + W_ANIS×d_anis + W_CIRC×d_circ`

| Weight | Default | Rationale |
|--------|---------|-----------|
| W_POS | 0.50 | Normalized coords, least affected by lighting |
| W_SIZE | 0.25 | Elliptic axes Ra/Rb + area |
| W_ANIS | 0.15 | Ra/Rb ratio, shape-stable |
| W_CIRC | 0.10 | Circularity, illumination-sensitive |

All weights/tolerances/thresholds are public fields on `Matcher`.

### Dual-mode matching (FormMatch)

**File mode** (`rbFile`): loads JSON folder via `Matcher.LoadCADLibrary()`, runs in-memory match.

**Database mode** (`rbDb`): connects to MSSQL via `CadRepository`. Connection string from `tbConnStr` (defaults to `app.config` `CadDb` key). Workflow:

```
连接数据库 → CreateTablesIfNotExist → 显示图纸数
开始匹配 →
  FilterCandidates(L,W,featCount)  -- 1 query, Stage 1+2
  LoadFeatures(ids)                -- 1 query, Stage 3
  Matcher.Match(image, cadLib)     -- Hungarian in C#, Stage 4
```

### MSSQL schema (CadRepository.CreateTablesIfNotExist)

```sql
cad_drawings: id, drawing_no, file_path, L, W, feat_count, created_at
  INDEX: L, W, feat_count

cad_features: id, drawing_id(FK), seq, cx, cy, area, circularity,
              anisometry, bulkiness, ra, rb
  INDEX: drawing_id

2次查询完成匹配: FilterCandidates → LoadFeatures
批量导入使用 SqlBulkCopy + 事务
```

### Import methods

- `ImportEncoding(enc, drawingNo)` — single insert (parameterized SQL + SqlBulkCopy for features)
- `ImportBatch(List<Tuple<FeatureEncoding, string, string>>)` — transactional batch

## Form lifecycle

- `Form1.FormClosing` → stop grab, close device, dispose model.
- `FormTemplate.FormClosing` → detach drawing object, clear model, dispose HObjects.
- `FormBlob.FormClosing` → dispose `ho_Image`.
- `FormMatch.OnFormClosing` → dispose `CadRepository`.
- `SetCtrlWhenOpen()` / `SetCtrlWhenClose()` on Form1 — always add new buttons to both.

## Button toggle pattern (Form1)

```csharp
m_BlobEnabled = !m_BlobEnabled;
bnBlobLive.Text = m_BlobEnabled ? "停止Blob" : "实时Blob";
bnBlobLive.BackColor = m_BlobEnabled ? Color.LightGreen : SystemColors.Control;
```
State reset in `SetCtrlWhenClose()`.

## DWG extraction (DwgExtractor)

- `DwgExtractor.Extract(dwgPath)` → `FeatureEncoding`. Uses **ACadSharp 3.5.7** (MIT, zero-dependency NuGet package).
- Supports R14 through AutoCAD 2022+ DWG files.
- **Board detection**: largest closed entity (LwPolyline, Circle, Ellipse) by area.
- **OBB**: PCA on ~200 boundary samples → centroid, Phi, L, W.
- **Hole features** — computed purely from geometry, no Halcon:
  - Area/Perimeter: analytic (shoelace + bulge arc correction, circle formula, Ramanujan for ellipses).
  - Centroid: Green's theorem (+ arc centroid adjustment for bulges).
  - Circularity: `4πA/P²`.
  - Elliptic axes (Ra, Rb): covariance matrix of ~200 boundary samples → eigenvalues → sqrt.
  - Coordinates normalized to board frame (same formula as FormBlob `bnEncode_Click`).
- **Bulge math**: DXF bulge `b = tan(θ/4)`, arc radius `r = c(1+b²)/(4|b|)`, arc center offset from chord midpoint.

## Git versioning

- Repo: `D:\claudepj\测量2026\.git`. Tags: `v1.10`, `v1.11`, `v1.20` (current).
- **Rollback:** `git checkout v1.20`
