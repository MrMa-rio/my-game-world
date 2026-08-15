# ADR 0005: Sandbox de mundo procedural em camadas

**Status:** Aceita
**Data:** 2026-08-15

## Contexto

A primeira zona visual precisa gerar terrain matematicamente sem transformar uma cena de demonstração em arquitetura paralela ou acoplar DNA e algoritmos à Unity. O terrain também precisa nascer consciente de budget, chunks, determinismo e futura evolução para LOD.

## Decisão

Criar `MyGameWorld.Shared.World` para DNA, biome, noise, height field, mesh data, placements e fingerprint sem `UnityEngine`. Criar `MyGameWorld.Client.ProceduralWorld` como adapter descartável de representação para meshes, shader, props, câmera e debug.

`TerrainGeneratorV1` usa um height field global e materializa uma grade de chunks. O budget resolve a quantidade de células antes da geração. A configuração padrão usa 2 × 2 chunks, 3.200 triângulos e shading flat. Mudanças que alterem heights, paths ou placements exigem nova `GeneratorVersion`.

Cada característica semântica deriva de `WorldElementDNA` e possui identidade, seed, versão e limites próprios. `ZoneFeaturePlannerV1` planeja morros, depressões, platôs e caminhos como elementos limitados antes de compor o height field. Decorações usam o mesmo princípio e acrescentam parâmetros próprios de forma. Chunks continuam sendo apenas partições da representação da superfície; consultas espaciais resolvem quais elementos influenciam um contato sem exigir objetos físicos separados.

Terrain mesh gerada em runtime é aceita como geometria procedural compacta, não como geração pesada de assets artísticos. Props matemáticos são placeholders da sandbox e deverão poder ser substituídos por recursos finitos via `AssetRegistry`.

## Consequências

O domínio pode ser testado e futuramente executado em ferramentas ou servidor sem renderização. Flat shading custa mais vértices que a grade lógica, mas mantém o budget de triângulos explícito. Adaptive tessellation, streaming e LOD permanecem futuras substituições do mesh builder, não requisitos de reescrita do DNA.
