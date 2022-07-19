<p align="center"><a href="https://oberbichler.github.io/Bowerbird"><img alt="Logo" width="60%" src="https://raw.githubusercontent.com/oberbichler/Bowerbird/master/docs/bowerbird.svg?sanitize=true&max-age=0"></a></p>

<p align="center"><i>Bowerbird is a plugin for Rhino and Grasshopper providing some tools for modeling.</i></p>

<p align="center"><a href="https://github.com/oberbichler/Bowerbird/releases/latest/download/Bowerbird.gha"><img alt="GitHub release (latest by date)" src="https://img.shields.io/github/v/release/oberbichler/Bowerbird?color=blue&label=Download&style=for-the-badge"></a></p>

---

[![Actions Status](https://github.com/oberbichler/Bowerbird/workflows/CI/badge.svg?branch=master)](https://github.com/oberbichler/Bowerbird/actions) ![GitHub all releases](https://img.shields.io/github/downloads/oberbichler/Bowerbird/total?label=GitHub%20downloads) ![Yak version](https://img.shields.io/badge/dynamic/json?label=Yak%20version&query=version&url=https%3A%2F%2Fyak.rhino3d.com%2Fpackages%2Fbowerbird) ![Yak downloads](https://img.shields.io/badge/dynamic/json?label=Yak%20downloads&query=download_count&url=https%3A%2F%2Fyak.rhino3d.com%2Fpackages%2Fbowerbird)

Grasshopper Group: https://www.grasshopper3d.com/group/bowerbird

## Installation

* [Download](https://github.com/oberbichler/Bowerbird/releases/latest/download/Bowerbird.gha) `Bowerbird.gha`
* Right-click the file > Properties > make sure there is no "blocked" text
* Drag `Bowerbird.gha` to the Grasshopper window

## Tutorials
- [Waffle framing for Breps *by IIT DC 2016*](https://youtu.be/H622kgtJ-tw)
- [Waffle example *by Parametric Curiosity*](https://youtu.be/0mhVP3XwDSU)
- [Layer models with Bowerbird and OpenNest (German) *by Team Digital*](https://youtu.be/EFKkGvJXLsE)

## Features

- Find **asymptotic paths** on freeform surfaces.

  ![Normal Curvature](https://raw.githubusercontent.com/oberbichler/Bowerbird/master/docs/normal-curvature.png?sanitize=true&max-age=0)

- Find **principal curvature paths** on freeform surfaces.

  ![Geodesic Torsion](https://raw.githubusercontent.com/oberbichler/Bowerbird/master/docs/geodesic-torsion.png?sanitize=true&max-age=0)

- **Measure normal curvature, geodesic curvature and geodesic torsion** on freeform surfaces.

- **Extrude and unroll** curves on freeform surfaces.

- **Integrate curvature** along curves.

- Plot **curvature fields**.

- Generate **orthogonal waffle** models with slits from freeform volumes.

  ![Waffle](https://raw.githubusercontent.com/oberbichler/Bowerbird/master/docs/images/Example_BBWaffle.png?sanitize=true&max-age=0)

- Generate **radial waffle** models with slits from freeform volumes.

  ![Radial](https://raw.githubusercontent.com/oberbichler/Bowerbird/master/docs/images/Example_BBRadial.png?sanitize=true&max-age=0)

- Generate **layer** models with automatic overlap from freeform volumes.

  ![Layer](https://raw.githubusercontent.com/oberbichler/Bowerbird/master/docs/images/Example_BBLayer.png?sanitize=true&max-age=0)

- Generate **generic slice** models with slits from freeform volumes.
  ![Slice](https://raw.githubusercontent.com/oberbichler/Bowerbird/master/docs/images/Example_BBSection.png?sanitize=true&max-age=0)

- Add labels with a CNC conform **single line fonts**.

  ![Text](https://raw.githubusercontent.com/oberbichler/Bowerbird/master/docs/images/Example_BBText.png?sanitize=true&max-age=0)

- Perform **boolean polyline** operations

## Reference

If you use Bowerbird, please refer to the official GitHub repository:

```
@misc{Bowerbird,
  author = "Thomas Oberbichler",
  title = "Bowerbird",
  howpublished = "\url{http://github.com/oberbichler/Bowerbird}",
}
```
