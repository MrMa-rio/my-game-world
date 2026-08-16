# Procedural Natural Clusters

A direção visual vem de `Assets/ia assets/base-forest.webp`. A referência contém bosques densos, formações de rocha, moitas e campos florais. Lago e caminhos continuam sob responsabilidade do terreno; `FlowerCluster` já representa os campos de flores.

Esta versão adiciona três identidades persistentes:

- `TreeCluster` (`AssetId 10008`): pequeno bosque composto por 3–5 árvores;
- `RockCluster` (`AssetId 10009`): formação composta por 3–6 rochas;
- `BushCluster` (`AssetId 10010`): moita composta por 3–7 volumes orgânicos.

Cada conjunto possui apenas um `WorldElementId`, seed, bounds, estado e mesh runtime. Seus membros internos são composição visual determinística, não entidades independentes nem vários `GameObject`s. A implementação usa o mesmo `ProceduralRuntimeManager`, scheduler, cache, pooling, materiais compartilhados e LOD das identidades singulares.

O LOD reduz a quantidade de membros e a complexidade de cada forma, preservando a silhueta geral. A diversidade deriva de arquétipo, escala, rotação e distribuição em ângulo áureo. Colliders permanecem simplificados e desacoplados da geometria visual.

A sandbox correspondente usa `TerrainGeneratorVersion V4`, `AssetCatalogVersion V3` e estilo procedural V7. A referência histórica V3 não é alterada.
