# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build

This is a C# WinForms application targeting .NET Framework 4.6.1. Build from Visual Studio (VS2017+) by opening `测量2026.sln` or the `.csproj` directly. No command-line build tooling is configured.

**Before rebuilding**, close any running instance of `测量2026.exe` — MSBuild cannot overwrite the output exe while it's running.

## Project structure

- **`Form1`** — Main window. Camera device enumeration, open/close, continuous grabbing with display on `HSmartWindowControl`. Exposure/gain controls. Button to open the template window.
- **`FormTemplate`** — Template acquisition popup. Grabs the current image from Form1, lets the user draw an interactive rectangular ROI, creates a Halcon scaled shape model (`CreateScaledShapeModel`), and saves it as a `.shm` file.
- **`MVCamera.cs`** — Hikvision MvCameraControl.dll P/Invoke wrapper (namespace `MvCamCtrl.NET`, class `MyCamera`). Copied verbatim from the 2024 measurement project. ~4800 lines.
- **`Program.cs`** — Standard WinForms entry point, runs `Form1`.

## Halcon 17.12 API rules

This project uses **Halcon 17.12 Progress** (`C:\Program Files\MVTec\HALCON-17.12-Progress\bin\dotnet35\halcondotnet.dll`). The API differs from newer Halcon versions in important ways:

### Drawing objects use HTuple handles, NOT HDrawingObject instances

`HDrawingObject` as an instantiable class does not exist in 17.12. All drawing object operations go through `HOperatorSet` static methods with `HTuple` handles:

```csharp
// Create
HTuple hv_DrawObj;
HOperatorSet.CreateDrawingObjectRectangle1(r1, c1, r2, c2, out hv_DrawObj);

// Configure
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

### Other Halcon 17.12 conventions

- `HOperatorSet.GenEmptyObj(out ho_Image)` before first use of any `HObject`.
- Image creation from raw buffers: `HOperatorSet.GenImage1Extern()` (mono) or `HOperatorSet.GenImageInterleaved()` (color RGB).
- Shape model: `HOperatorSet.CreateScaledShapeModel(...)` → `HOperatorSet.GetShapeModelContours(...)` → `HOperatorSet.WriteShapeModel(...)`.
- Cleanup: `HOperatorSet.ClearShapeModel(hv_ModelID)` when discarding a model.

## Camera integration (Hikvision SDK)

- `MyCamera.MV_CC_EnumDevices_NET()` with `MV_GIGE_DEVICE | MV_USB_DEVICE` flags.
- After opening, set `AcquisitionMode=2` (continuous) and `TriggerMode=0` (off).
- Image acquisition runs on a background thread (`ReceiveImageWorkThread`). The live image is stored in `Form1.Hobj` (an `HObject` field).
- `Form1.GetCurrentImage()` returns `Hobj.Clone()` so callers get a snapshot — caller owns disposal.
- Pixel format handling: Mono8 passed directly; other mono formats converted via `MV_CC_ConvertPixelType_NET`; Bayer/RGB/YUV formats converted to RGB8_Packed then handled with `GenImageInterleaved`.
- `MouseWheel` events on both forms need coordinate translation (screen → control) before forwarding to `HSmartWindowControl_MouseWheel`.

## Form lifecycle

- `Form1.FormClosing` calls `bnClose_Click` which stops grabbing and closes the camera device.
- `FormTemplate.FormClosing` detaches any active drawing object, clears the shape model, and disposes all HObjects.
