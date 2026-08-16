# Procedural World Sandbox

## Objetivo

A sandbox é a primeira materialização visual real de `My Game World`. Ela não é uma cena descartável: estabelece os contratos de zona, height generation, chunks, budget geométrico, materialização cliente, observabilidade e determinismo que poderão evoluir para regiões e mundos completos.

Cena inicial:

```text
Assets/Scenes/ProceduralWorldSandbox.unity
```

## Configuração padrão

| Parâmetro | Valor |
|---|---:|
| Zone ID | `TEST_002` / `2` |
| Seed | `829173` |
| Generator version | `4` |
| Asset catalog version | `3` |
| Biome | `TemperateGrassland` |
| Tamanho | `1.000 × 1.000 m` |
| Resolução solicitada | `257 × 257` |
| Resolução pelo budget | `201 × 201` |
| Células | `200 × 200` |
| Chunks | `10 × 10` |
| Triângulos | `80.000` |
| Budget | `80.000` |
| Vértices lógicos | `40.401` |
| Vértices renderizados flat | `240.000` |
| Shading | `Flat` |

A diferença entre vértices lógicos e renderizados é intencional: flat shading duplica os três vértices de cada triângulo para preservar normals facetadas. Smooth shading reutiliza os vértices da grade dentro de cada chunk.

## Arquitetura

### Elementos procedurais singulares

Toda parte semanticamente relevante da zona deriva de `WorldElementDNA`. O contrato comum contém `WorldElementId`, `ZoneId`, `WorldElementKind`, seed própria, `GeneratorVersion`, `AssetCatalogVersion` e limites espaciais. A V1 materializa elementos especializados para a superfície-base, morros, depressões, platôs, caminhos, árvores, rochas, arbustos e marcadores.

`ZoneFeaturePlannerV1` cria primeiro um plano determinístico limitado por `WorldGenerationLimits`. O height field é então composto pela superfície-base e pelas contribuições individuais de cada `LandformDNA` e `PathDNA`. Isso permite atualizar uma característica por ID no futuro sem transformar posições específicas em regras do algoritmo.

Os chunks são partições de renderização da mesma superfície, não novas identidades de gameplay. Um contato no `MeshCollider` pode chamar `ResolveTerrainContact(position)` para obter a superfície e todos os acidentes procedurais que influenciam aquele ponto. Assim evitamos um collider ou `GameObject` por morro, preservando identidade e performance ao mesmo tempo.

Cada decoração possui DNA próprio e parâmetros de forma próprios (`ShapeA/B/C`). A representação Unity recebe `WorldElementRuntimeIdentity`, enquanto meshes e materiais continuam compartilhados. A identidade sobrevive à troca futura das primitivas por prefabs do `AssetRegistry`.

```text
MyGameWorld.Shared.World
├── ZoneDNA / ZoneId
├── TerrainGenerationConfig
├── BiomeDefinition
├── DeterministicNoise2D
├── HeightFieldGeneratorV1
├── TerrainHeightField
├── TerrainMeshDataBuilder
├── TerrainGeneratorV1
├── DecorationGeneratorV1
└── GenerationFingerprint

MyGameWorld.Client.ProceduralWorld
├── ProceduralWorldSandbox
├── ProceduralRuntimeManager
├── UnityTerrainChunkRuntime
├── ProceduralGeometryLibrary
├── ProceduralMeshCache / ProceduralLodResolver
├── material library / geometry providers
├── DevelopmentFreeCamera
└── ProceduralWorldDebugHud
```

O assembly compartilhado não referencia `UnityEngine`. Ele produz arrays e tipos matemáticos neutros. O cliente cria meshes, colliders, materiais e GameObjects descartáveis a partir desse resultado.

## Height generation V2

Cada ponto usa coordenadas globais da zona. Seeds independentes são derivadas da seed da zona para:

- relevo regional;
- macro landform;
- detail noise;
- features singulares;
- caminhos singulares.

O height final combina:

```text
height  = baseHeight
height += regionalFBM × regionalAmplitude
height += macroFBM × macroAmplitude
height += singularLandformContributions
height  = blend(height, pathHeight, singularPathMasks)
height += detailFBM × smallAmplitude × offPathMask
```

O regional fBm cria grandes bacias e massas de terreno legíveis na zona ampliada. O macro fBm cria variação intermediária, enquanto o detail fBm adiciona irregularidade de baixa amplitude. Morros, depressões, platôs e caminhos continuam sendo elementos singulares com DNA próprio.

## Polygon budget e chunks

Para um grid de resolução `N`:

```text
triangles = (N - 1) × (N - 1) × 2
```

O resolver calcula o maior número de células que cabe em `targetTriangleBudget`, limita pela resolução solicitada e arredonda para um múltiplo da quantidade de chunks. Com budget 80.000, o resultado é exatamente 200 × 200 células distribuídas em 100 chunks.

Todos os chunks consultam o mesmo height field global. Assim, vértices e normals das bordas compartilhadas são idênticos. `TerrainGeneratorV1.GenerateChunk(...)` já expõe o limite necessário para streaming e LOD futuros, embora V1 gere a pequena zona inteira sincronamente.

Adaptive tessellation não está implementada. A separação entre height field e mesh builder permite substituir a topologia por quadtree, simplificação ou LOD sem mudar `ZoneDNA` ou o algoritmo de height.

## Biome e cores

`BiomeDefinition` contém escalas, amplitudes, densidade, distância mínima, limiar de rocha e paleta. `TerrainGeneratorV1` não possui valores específicos de grassland.

As cores são gravadas por vértice:

- grass varia com altura;
- dirt segue a máscara de caminhos;
- rock cresce com inclinação.

Um shader URP pequeno combina vertex color, luz principal, spherical harmonics, shadows e fog.

## Decoração

`DecorationGeneratorV1` divide a zona em células de candidatos, deriva uma seed por célula e aplica jitter controlado. Candidatos são rejeitados por:

- proximidade de caminho;
- distância mínima de outro objeto;
- regras de inclinação usadas na seleção do tipo.

Árvores, rochas, arbustos e quatro marcadores de escala são meshes low-poly geradas por código. Materiais e meshes são compartilhados. Esses props são placeholders de desenvolvimento; o posicionamento neutro poderá ser materializado por prefabs resolvidos no `AssetRegistry`.

## Determinismo

O gerador não usa `UnityEngine.Random`, horário, ordem instável de coleções ou estado de cena. `ZoneDNA`, parâmetros, versão e catálogo são explícitos.

Validação automatizada:

```text
seed 100 → fingerprint A
destroy/rebuild
seed 100 → fingerprint A
seed 101 → fingerprint B
A != B
```

O fingerprint cobre heights, path masks, configuração resolvida, identidades dos acidentes, líquidos, `AssetId` visual e DNAs das decorações. O mundo visual atual usa a seed `829173`, catálogo V3 e gerador V4. A referência dourada V4 da seed `829172` permanece `9B330B8968E0830E`, e a referência histórica do gerador V3 com catálogo V2 permanece `7433A6EE28E0AC51`.

## Direção visual pesquisada

A geometria matemática utiliza referências apenas como estudo de linguagem visual, sem copiar ou importar seus modelos. Foram observados principalmente:

- [coleção de árvores low-poly indicada para a tarefa](https://www.magnific.com/free-vector/flat-low-poly-trees-collection_45199327.htm);
- [Quaternius Ultimate Nature Pack](https://quaternius.com/packs/ultimatenature.html);
- [Kenney Nature Kit](https://kenney.nl/assets/nature-kit);
- [Quaternius Ultimate Stylized Nature](https://www.patreon.com/quaternius/posts/ultimate-nature-67157089).

Os princípios extraídos são massas de copa sobrepostas, taper de tronco, assimetria controlada, rochas com planos largos, famílias de silhueta e paleta compartilhada. Cada família continua recebendo variação procedural por seed, LOD, proporção, cor e ambiente.

A evolução específica das árvores está registrada em [Direção Artística da Árvore Procedural](PROCEDURAL_TREE_ART_DIRECTION.md).

A direção conjunta de terreno e pedras está registrada em [Direção Artística do Terreno e das Pedras](PROCEDURAL_TERRAIN_ROCK_ART_DIRECTION.md).

## Controles

| Controle | Ação |
|---|---|
| `W/S` | frente/trás |
| `A/D` | esquerda/direita |
| `Q/E` | descer/subir |
| botão direito + mouse | olhar |
| `Shift` | movimento rápido |
| scroll | ajustar velocidade |
| `F1` | mostrar/ocultar HUD |
| `F2` | mostrar/ocultar wireframe |
| `F3` | regenerar a mesma seed |
| `F4` | incrementar seed e regenerar |

A seed também pode ser alterada no campo `_zoneSeed` do componente `ProceduralWorldSandbox` no Inspector.

## Limitações atuais

- apenas um biome e um terrain profile;
- geração síncrona da zona completa;
- sem água, rios, estradas autoradas, cavernas ou estruturas;
- sem adaptive geometry ou LOD de mesh;
- sem pooling de GameObjects de decoração entre regenerações;
- props matemáticos ainda não usam `AssetRegistry`;
- sem `WorldDelta`, streaming ou persistência;
- o contato resolve identidades, mas mutações por elemento ainda aguardam `WorldDelta`;
- HUD usa IMGUI por ser uma ferramenta de desenvolvimento;
- câmera não possui colisão e não representa a futura câmera do player.

## Materialização procedural centralizada

Árvores, rochas, arbustos, marcadores e chunks não são mais materializados por código de criação espalhado. A representação cliente passa pelo [Procedural Runtime Manager](PROCEDURAL_RUNTIME_MANAGER.md), que controla fila, budget por frame, geometria compartilhada, LOD, cache, pooling, instancing, colliders simplificados e métricas.

## Próximo passo recomendado

Criar validação/editor tooling para `ZoneDNA` e `BiomeDefinition`, seguida por um adapter de decoração que associe `DecorationKind`/`AssetId` a prefabs do `AssetRegistry`. Não iniciar estruturas, streaming ou `WorldDelta` antes de estabilizar esse contrato e medir a geração em diferentes budgets.
