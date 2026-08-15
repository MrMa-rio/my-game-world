# Procedural Runtime Manager

## Papel

`ProceduralRuntimeManager` é a fachada cliente responsável por transformar definições existentes do mundo em representação Unity. Ele não cria categorias nem decide o que um objeto é. O vertical slice recebe diretamente `DecorationPlacement`, `DecorationKind`, `WorldElementId`, seed, biome e amostras do `TerrainHeightField` existentes.

```text
DecorationPlacement
        ↓
ProceduralGenerationRequest
        ↓
ProceduralRuntimeManager
        ├── ProceduralLodResolver
        ├── ProceduralMeshCache
        ├── IProceduralGeometryProvider
        ├── frame generation queues
        └── instance pool
        ↓
WorldElementRuntimeIdentity + optimized Unity representation
```

O terreno continua sendo gerado pelo domínio `MyGameWorld.Shared.World`. Seus chunks passam pela fachada do manager para materialização, mas não entram na fila de decorações porque o height field e o polygon budget já são resolvidos como uma operação explícita de geração da zona.

## Vertical slice

O primeiro adapter geométrico suporta as categorias existentes `Tree`, `Rock`, `Bush` e o marcador de escala da sandbox. Adicionar outra família visual exige registrar outro `IProceduralGeometryProvider`; scheduler, cache, LOD, pooling e métricas não precisam conhecer a nova categoria.

`LowPolyMeshDraft` centraliza:

- triângulos com normals flat;
- rings com irregularidade determinística;
- prismas;
- bipirâmides radiais;
- volumes irregulares;
- submeshes, cores e bounds.

A geometria nasce low-poly. Não existe geração high-poly seguida por decimation.

## Estilo e variação

`ProceduralStyleProfile` define somente regras globais que não existiam no domínio: angularidade, assimetria, variação de silhueta, variação de cor, quantidade de variantes geométricas e versão do estilo.

Por padrão existem quatro variantes geométricas compartilháveis por categoria e LOD. A seed do elemento escolhe deterministicamente uma dessas variantes. A individualidade restante usa transform, `ShapeA/B/C`, rotação, cor por `MaterialPropertyBlock` e adaptação à normal do terreno. Assim dezenas de elementos não criam dezenas de meshes quase idênticas.

Materiais são compartilhados, têm GPU instancing habilitado e o shader declara `_InstanceColor` como propriedade instanciada. Árvores permanecem majoritariamente verticais; pedras acompanham 90% da normal local; arbustos, 30%.

## LOD

O manager resolve LOD visual por distância e escala:

| Categoria | High | Medium | Low |
|---|---:|---:|---:|
| Tree | abaixo de 520 m | abaixo de 900 m | demais distâncias |
| Rock, Bush e Marker | abaixo de 24 m | abaixo de 52 m | demais distâncias |

As distâncias são multiplicadas pela escala do elemento. Árvores possuem alcance próprio porque sua silhueta é uma referência visual importante na câmera panorâmica da zona de 1 km.

LOD reduz a receita geométrica diretamente. Elementos secundários de copa/volume desaparecem antes que a silhueta principal seja alterada. O manager verifica um número limitado de instâncias por frame e enfileira transições que ainda não estejam no cache.

## Scheduler e budget

Requests entram em filas `High`, `Normal` e `Low`. A configuração inicial limita cada frame a:

- 2 ms de geração;
- 24 objetos;
- aproximadamente 14.000 vértices solicitados;
- 16 avaliações de LOD.

O processamento para ao atingir qualquer limite. Cache hits ainda passam pelo mesmo lifecycle, mas não reconstroem a mesh.

## Cache, instâncias e lifetime

A chave da mesh contém categoria existente, LOD, variante geométrica quantizada e versão do estilo. Ela não contém posição, rotação, escala ou cor.

`ReleaseAll()` devolve representações para pools por `DecorationKind`, limpa requests pendentes e mantém o cache geométrico. Regenerar a zona reutiliza GameObjects, meshes e materiais. `OnDestroy` libera pools e meshes cacheadas.

Colliders são independentes da geometria visual: cápsula simples para árvores, box para rochas e arbustos e nenhum collider para marcadores.

## Debug e métricas

`ProceduralRuntimeDebugInfo` expõe por instância:

- identidade e seed pelo `WorldElementRuntimeIdentity`;
- LOD;
- vértices e triângulos;
- tempo de geração;
- cache hit/miss;
- chave do cache;
- indicação de mesh compartilhada.

O HUD mostra objetos ativos, fila, meshes cacheadas/geradas, hits/misses, vértices, triângulos, tempo gasto no frame e passes de renderer estimados. O último valor é uma estimativa por submesh, não uma leitura de draw calls reais do GPU profiler.

## Limites atuais

- geração de `Mesh` ocorre na main thread, protegida pelo budget;
- ainda não há Jobs/Burst, renderização GPU-driven ou compute shaders;
- pooling é por categoria, sem política de capacidade/expiração;
- LOD usa distância, sem teste de oclusão ou importância de gameplay;
- terreno é materializado de forma síncrona ao regenerar a pequena zona;
- deformações de vento ainda não foram adicionadas ao shader;
- o adapter usa geometria matemática da sandbox; substituição por `AssetId` continua possível na fronteira do provider.

## Próximo passo recomendado

Adicionar uma interface de representação que permita ao provider escolher entre mesh procedural cacheada e prefab finito resolvido por `AssetId`, mantendo request, scheduler, LOD, pooling e métricas inalterados. Depois disso, medir o vertical slice no Unity Profiler antes de introduzir Jobs ou streaming avançado.
