<p align="center"><a href="https://oberbichler.github.io/Bowerbird"><img alt="Logo" width="60%" src="https://raw.githubusercontent.com/oberbichler/Bowerbird/main/docs/bowerbird.svg?sanitize=true&max-age=0"></a></p>

<p align="center"><i>Bowerbird is a digital fabrication and computational geometry plugin for Rhino and Grasshopper. It excels at automatic generation of waffle and layer models, advanced curve-on-surface analysis, and fabrication-ready single-line text rendering.</i></p>

<p align="center"><a href="https://github.com/oberbichler/Bowerbird/releases/latest/download/Bowerbird.gha"><img alt="GitHub release (latest by date)" src="https://img.shields.io/github/v/release/oberbichler/Bowerbird?color=blue&label=Download&style=for-the-badge"></a></p>

<p align="center">
  <a href="https://github.com/oberbichler/Bowerbird/actions"><img src="https://github.com/oberbichler/Bowerbird/workflows/CI/badge.svg?branch=main" alt="Actions Status"></a>
  <img src="https://img.shields.io/github/downloads/oberbichler/Bowerbird/total?label=GitHub%20downloads" alt="GitHub downloads">
  <img src="https://img.shields.io/badge/dynamic/json?label=Yak%20version&query=version&url=https%3A%2F%2Fyak.rhino3d.com%2Fpackages%2Fbowerbird" alt="Yak version">
  <img src="https://img.shields.io/badge/dynamic/json?label=Yak%20downloads&query=download_count&url=https%3A%2F%2Fyak.rhino3d.com%2Fpackages%2Fbowerbird" alt="Yak downloads">
</p>

## Requirements

* **Rhino 8** or newer (due to the .NET 8 runtime requirement)

## Installation

The easiest way to install Bowerbird is through the **Rhino Package Manager**:

1. Open Rhino 8 or newer.
2. Execute the `_PackageManager` command.
3. Search for **Bowerbird**.
4. Select the package and click **Install**.
5. Restart Rhino.

### Manual Installation
1. [Download](https://github.com/oberbichler/Bowerbird/releases/latest/download/Bowerbird.gha) `Bowerbird.gha`.
2. Right-click the downloaded file > Properties > Check **Unblock** if available.
3. Drag and drop `Bowerbird.gha` into the Grasshopper window.

## Tutorials

- [Waffle framing for Breps *by IIT DC 2016*](https://youtu.be/H622kgtJ-tw)
- [Waffle example *by Parametric Curiosity*](https://youtu.be/0mhVP3XwDSU)
- [Layer models with Bowerbird and OpenNest (German) *by Team Digital*](https://youtu.be/EFKkGvJXLsE)

## Key Features

| Feature | Description |
| :--- | :--- |
| **Waffle Generation** | Generate structural **orthogonal waffles**, **radial waffles**, and **generic slices** with slits from freeform volumes. <br><br> ![Waffle](https://raw.githubusercontent.com/oberbichler/Bowerbird/main/docs/images/Example_BBWaffle.png?sanitize=true&max-age=0) |
| **Layer Generation** | Generate **layer models** with automatic overlap from freeform volumes. <br><br> ![Layer](https://raw.githubusercontent.com/oberbichler/Bowerbird/main/docs/images/Example_BBLayer.png?sanitize=true&max-age=0) |
| **Curvature Fields & Paths** | Find **asymptotic paths** and **principal curvature paths** on freeform surfaces, measure normal/geodesic curvature and geodesic torsion, and integrate or plot curvature fields. <br><br> ![Normal Curvature](https://raw.githubusercontent.com/oberbichler/Bowerbird/main/docs/normal-curvature.png?sanitize=true&max-age=0) |
| **CNC Single-Line Text** | Render CNC-compliant **single-line fonts** for engraving and fabrication labels. <br><br> ![Text](https://raw.githubusercontent.com/oberbichler/Bowerbird/main/docs/images/Example_BBText.png?sanitize=true&max-age=0) |
| **Boolean Polylines** | Perform efficient **boolean polyline** operations. |

## Reference

If you use Bowerbird, please refer to the official GitHub repository:

```
@misc{Bowerbird,
  author = "Thomas Oberbichler",
  title = "Bowerbird",
  howpublished = "\url{http://github.com/oberbichler/Bowerbird}",
}
```
