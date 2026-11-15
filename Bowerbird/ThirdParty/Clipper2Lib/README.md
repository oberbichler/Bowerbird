# Clipper2 (vendored)

Source: https://github.com/AngusJohnson/Clipper2, tag `Clipper2_2.0.1`, directory `CSharp/Clipper2Lib`.
License: Boost Software License 1.0 (see `LICENSE.txt`).

Compiled directly into Bowerbird instead of being referenced as the `Clipper2` NuGet package,
so Bowerbird no longer ships `Clipper2Lib.dll` and cannot conflict with other Grasshopper plugins
that load a different Clipper2 version (see issue #95).

Only modification: namespace `Clipper2Lib` renamed to `Bowerbird.Clipper2Lib`.
