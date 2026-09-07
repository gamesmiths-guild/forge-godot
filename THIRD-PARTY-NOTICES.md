# Third-Party Notices

Forge for Godot is distributed under the terms of its own [LICENSE](LICENSE). It also includes code from the projects listed below, whose licenses are reproduced here in full because those licenses require it.

Each entry names what was taken and where it lives, so a reader can go from a source file back to the license that covers it.

---

## Godot Engine

**Used in:** `addons/forge/editor/statescript/resolvers/bases/CollisionLayersGrid.cs`

The thirty-two bit collision layer grid in that file is a port of `EditorPropertyLayersGrid` from Godot Engine's `editor/inspector/editor_properties.cpp` — the control that draws the same grid behind `collision_layer` and `collision_mask` in Godot's own inspector. The block packing, hit testing, drag behavior and drawing follow the engine's implementation rather than approximating it.

- Project: <https://godotengine.org>
- Source: <https://github.com/godotengine/godot>
- License: MIT

```
Copyright (c) 2014-present Godot Engine contributors (see AUTHORS.md).
Copyright (c) 2007-2014 Juan Linietsky, Ariel Manzur.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
