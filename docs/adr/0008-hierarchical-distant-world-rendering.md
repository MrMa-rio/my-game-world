# ADR 0008: Hierarchical distant world rendering foundation

## Status

Accepted for the first functional iteration.

## Context

The sandbox previously generated one finite 5 km zone eagerly. Its terrain chunks were rendering and physics objects at the same time, and terrain detail was not selectable by spatial frequency. Increasing the draw distance would therefore increase full-detail work and would not establish a scalable world model.

## Decision

World identity is represented in global `double` coordinates and integer quadtree cell coordinates. Unity transforms remain local floats and a single `WorldRebased` event is emitted after an origin shift. A `WorldCell` is data; only active visual representations receive pooled Unity objects.

`HeightFieldGeneratorV2` is the shared source of truth for detailed and distant representations. `HierarchicalWorldGenerator` adapts that same source and samples macro, meso and micro terms at identical global coordinates. Representation levels omit imperceptible high-frequency terms but never substitute a different macro world. Generator version participates in the deterministic seed.

`WorldSpatialHierarchy` selects aggregated cells with a quadtree. Close cells subdivide; distant cells remain regional. `WorldRepresentationProfile` centralizes resolution, frequency, asset density, shadows, physics, simulation, VFX and hysteresis. The initial resolutions are 129/129/65/33/17/9 for Simulation/Near/Medium/Far/Distant/Horizon.

`DistantWorldRenderer` owns visual materialization only. Pure mesh data is prepared outside the main thread, then Unity meshes are committed under per-frame CPU and count budgets. Cells are pooled and cached in active/warm states. Selection starts with coarse spatial nodes and incrementally refines them. Terrain borders sample the same exact global coordinates; the quadtree additionally constrains spatial scale by representation. Triangles owned by the detailed terrain are clipped from hierarchical meshes, preventing coplanar duplicate terrain while retaining boundary coverage.

Forests in Far and Distant representations use one regional canopy proxy driven by deterministic coverage metadata. Individual asset rendering remains with `ProceduralRuntimeManager`, which already owns finite asset resolution, geometry caching and object pooling. Horizon deliberately contains no individual assets.

Atmospheric visibility and terrain render distance are independent. The shared terrain shader attenuates contrast toward the environment fog color after geometry is rendered. Maximum-visibility debug disables this attenuation, making geometric gaps observable. Weather can change atmospheric visibility without regenerating geometry.

## Migration and limitations

The existing bounded V4 sandbox terrain remains available as the detailed compatibility representation while gameplay data migrates to global cells. `TerrainScalabilityPolicy` keeps that eager detailed area inside its validated 5 km/500 m envelope; additional area must come from hierarchical cells rather than multiplying full terrain, physics and decorations. The common runtime root is rebased atomically so detailed terrain, assets, liquids and distant proxies cannot diverge. Explicit geomorph/cross-fade, disk cache, water macro surfaces and GPU indirect rendering are extension points, not part of this iteration.

## Consequences

Large-scale geography is stable across LODs, physics stays out of distant cells, camera travel no longer requires full chunks for the horizon, and profiling can separate hierarchy selection, jobs and mesh commits. Future server-owned logical cells and GPU-driven render data can consume the same global identity without depending on GameObjects.

## Scalable highlands validation

Generator V5 adds the versioned `LargeScaleTerrainProfile` to the shared height source. The validation sandbox keeps its eager detailed footprint at 5 km, raises only the validated vertical envelope to 420 m, and adds 7.2 km deterministic mountain ridges that continue through hierarchical cells. Content is capped at 3,600 planned detailed placements; distant forest coverage remains aggregated. This replaces the rejected approach of expanding a complete 8 km heightfield with 900 m local features and thousands of additional physics-capable objects.

Generator V6 replaces unconstrained radial landform subtraction with `GeologicalLandformModel`. Hills use a smooth dome constrained by a 34 degree stability envelope. Depressions use a broad concave profile, a maximum depth/radius ratio of 0.12, an 18 degree envelope and a deterministic shallow spillway direction. This avoids narrow, circular pits while remaining globally point-sampleable at every LOD.

The design is based on the official [FastNoise Lite fractal/domain-warp model](https://github.com/Auburn/FastNoiseLite/wiki/Documentation), Musgrave's [Methods for Realistic Landscape Imaging](https://www.kenmusgrave.com/dissertation.pdf), the [Priority-Flood depression analysis paper and reference implementation](https://github.com/r-barnes/Barnes2013-Depressions), and Unity's [Hydraulic Erosion Terrain Tool](https://docs.unity3d.com/Packages/com.unity.terrain-tools@5.0/manual/erosion-hydraulic.html). Full hydraulic simulation is intentionally deferred because runtime point sampling must remain deterministic and independent of generation order.
