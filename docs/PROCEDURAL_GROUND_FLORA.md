# Procedural Ground Flora

Esta etapa adiciona quatro identidades persistentes ao domínio já existente:

- `Flower`: uma flor singular;
- `FlowerCluster`: um único elemento que materializa um aglomerado de flores;
- `Mushroom`: um cogumelo singular;
- `MushroomCluster`: um único elemento que materializa um conjunto de cogumelos.

Um grupo não é uma lista acidental de `GameObject`s. Ele possui um `ElementId`, seed, `AssetId`, bounds, parâmetros de forma e estado próprios. As várias flores ou cogumelos que aparecem dentro dele são partes determinísticas de uma única malha runtime. Isso permite evoluir, persistir, remover ou substituir o grupo como unidade sem perder a possibilidade de futuramente promover uma parte interna a uma entidade independente.

## Pipeline

```text
DecorationPlacement (domínio)
  -> ProceduralGenerationRequest
  -> ProceduralRuntimeManager
  -> NaturalDecorationGeometryProvider
  -> ProceduralGroundFloraGeometryBuilder
  -> cache por AssetId + LOD + variação + versão de estilo
  -> malha e materiais compartilhados
```

O domínio determina o que existe. O manager determina como essa identidade é materializada. Flores e cogumelos não criam meshes nem materiais por conta própria.

## Determinismo e distribuição

O `DecorationGeneratorV2` usa somente o gerador pseudoaleatório determinístico do projeto. A escolha de categoria, posição, escala e composição interna deriva da seed da zona e da identidade do elemento. Cogumelos favorecem habitats com maior índice ambiental; caminhos e inclinações inválidas continuam sendo recusados pelas regras comuns de colocação.

A zona sandbox usa `TerrainGeneratorVersion V3` e `AssetCatalogVersion V2`. V1/V2 anteriores não passam a gerar flora retroativamente.

## Geometria e LOD

As formas nascem low-poly, sem gerar high-poly para depois decimar:

- flores: haste afunilada, centro e pétalas radiais;
- aglomerados de flores: distribuição em ângulo áureo dentro de uma única malha;
- cogumelos: haste afunilada e chapéu irregular achatado;
- conjuntos de cogumelos: alturas e escalas controladas dentro de uma única malha.

O LOD reduz primeiro pétalas, subdivisões e quantidade de membros internos, preservando altura, largura e silhueta. Materiais são compartilhados e habilitados para GPU instancing. Os colliders são caixas simples e permanecem desacoplados da geometria visual.

## Extensão segura

Novas flores ou fungos devem entrar como novos `AssetId`s/receitas visuais quando possuírem identidade artística diferente. Variações menores devem preferir transformação, cor e parâmetros de material, evitando uma mesh por seed. Uma nova categoria só é necessária quando houver semântica e ciclo de vida próprios no domínio.

Limitações atuais: não há folhas individuais, vento em shader, estados de crescimento, colheita, toxicidade, reação ao jogador ou persistência de partes internas do aglomerado. Esses comportamentos devem ser adicionados sobre a identidade existente, sem reconstruir topologia continuamente por frame.
