# Environmental Interaction Framework V1

## Arquitetura

O framework ambiental é uma camada de representação cliente. Ele não altera `ZoneDNA`, fingerprint, estado autoritativo ou geração persistente.

```text
EnvironmentalManager (uma atualização central)
  ├── WindSystem
  │    ├── SampleWind(worldPosition)
  │    └── shader globals
  ├── EnvironmentalPhysicalResponseSystem
  │    └── registros processados em lotes
  ├── EnvironmentalSurfaceResolver
  │    └── height field + slope + path + liquids
  └── EnvironmentalVfxSystem
       ├── biome/surface profiles
       ├── LOD/câmera/frustum
       ├── células dos chunks existentes
       └── EnvironmentalVfxPool
```

Não existem managers específicos para árvores, grama, desertos ou neve. O vento publica uma força; perfis físicos e ambientais interpretam a mesma amostra.

## Campo de vento

`WindProfile` concentra direção, velocidade, intensidade, turbulência, força/frequência de rajadas, escala espacial e velocidade de variação. `WindSystem.SampleWind(Vector3)` combina duas amostras Perlin espaciais/temporais, rotação local e envelope de rajada. Pontos diferentes não ficam sincronizados.

Uma atualização por frame publica `_WorldWindDirectionStrength` e `_WorldWindParameters`. O shader compartilhado executa pequenas deformações GPU com fase espacial. Não há atualização CPU por folha, branch ou asset.

## Resposta física

`PhysicalResponseProfile` contém massa, rigidez, flexibilidade, damping, resistências, thresholds, recuperação, área e drag. `PhysicalResponseCatalog` fornece perfis iniciais para raiz, tronco, galhos grandes/pequenos, folhas e superfícies flexíveis.

Árvores usam quatro slots compartilhados: tronco, duas cores de copa e galhos. Os pesos do shader são derivados do mesmo catálogo físico: folhas respondem mais que galhos e galhos mais que troncos. `EnvironmentalPhysicalResponseSystem` registra somente roots runtime, amostra um budget distribuído e guarda LOD; não cria `MonoBehaviour.Update`, Rigidbody ou collider adicional por zona. Física seletiva após quebra fica para uma versão futura.

## Contexto e VFX

`EnvironmentalSurfaceResolver` reutiliza `TerrainHeightField`, path mask, normal e `LiquidBodyDNA`; não usa raycasts. A primeira tabela padrão resolve:

| Biome | Superfície | Efeito |
|---|---|---|
| Desert | Sand | SandDust |
| Forest | Grass | DryLeaves |
| Grassland | Grass | Pollen |
| Snow | Snow | LooseSnow |

Água, rocha e concreto bloqueiam emissões rasteiras nessa V1. `BiomeEnvironmentalResponseProfile` é um `ScriptableObject` opcional para substituir a tabela por dados autorados, incluindo threshold, densidade, velocidade, tamanho, lifetime, chance rara e cooldown.

A emissão usa curva smoothstep contínua. Direção e velocidade vêm da amostra de vento; noise do ParticleSystem adiciona perturbação. Rajadas aumentam emissão e podem produzir bursts raros com cooldown global.

## Escala e lifetime

As 100 células ambientais reutilizam os chunks do terreno. A cada 0,35 s o sistema ordena relevância, testa distância/frustum e ativa no máximo 12 emitters:

- até 30 m: densidade integral;
- 30–80 m: 45%;
- 80–150 m: 16%;
- acima de 150 m: desativado.

O pool é criado uma vez, usa uma malha low-poly compartilhada e nunca executa `Instantiate/Destroy` durante emissão normal. O ParticleSystem limita cada emitter a 180 partículas. Detalhes massivos permanecem no shader; partículas representam somente elementos visualmente relevantes.

## Debug

- `F5`: alterna vento fraco/médio/forte;
- `F6`: alterna Grassland/Forest/Desert/Snow;
- `F7`: alterna densidade VFX;
- HUD: força, velocidade, rajada, biome e chunks VFX ativos;
- gizmos do `EnvironmentalManager`: campo local de direção.

## Limitações e extensões

Ainda não há chuva, fogo, umidade persistente, neve acumulada, quebra estrutural, bones próximos, GPU VFX Graph ou oclusão. As extensões devem publicar novos fenômenos/contextos e reutilizar registro, surface resolver, chunks, profiles, LOD e pool. Estado crítico ou destruição futura deverá permanecer autoritativo; animação cosmética continua cliente.
