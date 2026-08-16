# Anti-Aliasing and Image Stability

The project uses URP 17.4 in Forward mode. `RenderingQualityManager` is the single runtime authority for camera post-process AA, pipeline MSAA/render scale, anisotropic filtering, LOD bias and image-stability parameters. Profiles are data assets under `Assets/Resources/RenderingQuality`.

## Presets

| Tier | AA | Render scale | LOD bias | Subpixel threshold |
| --- | --- | ---: | ---: | ---: |
| Low | FXAA | 0.8 | 1.0 | 2.5 px |
| Medium | SMAA High | 0.9 | 1.25 | 2.0 px |
| High | SMAA High | 1.0 | 1.6 | 1.5 px |
| Ultra | URP TAA High | 1.0 | 2.0 | 1.0 px |

MSAA is an explicit comparison mode and is never stacked with FXAA, SMAA or TAA. Alpha-to-coverage remains disabled because current procedural foliage is opaque geometry rather than alpha-clipped texture foliage. It can be enabled by a future vegetation profile only when MSAA and compatible cutout materials are both active.

## Stability layers

- Terrain LOD omits micro frequency in distant representations.
- Distant specular response fades before highlights become unstable subpixel flashes.
- Regional proxies are culled when their projected vertical size falls below the active pixel threshold.
- Procedural asset LOD receives the same central LOD bias and subpixel policy.
- Both world and asset LOD use hysteresis.
- TAA mip bias, history weight, variance clamp and sharpening are configured through the supported URP camera API.
- Mip and anisotropic controls are centralized. Current runtime terrain and vegetation use vertex colors, so there are no terrain detail textures to retrofit with mipmaps.

## Debug and profiling

- `PageUp`: cycle Low, Medium, High and Ultra.
- `PageDown`: cycle Off, FXAA, SMAA, MSAA 4x and TAA.
- `Insert`: maximum visibility/clear atmosphere.
- HUD reports AA, MSAA, render scale, resolution, LOD/mip settings and sampled CPU/GPU frame time.

Evaluate while rotating and moving at normal, fast and teleport-like speeds. Clear-atmosphere mode is mandatory when judging distant shimmer.
