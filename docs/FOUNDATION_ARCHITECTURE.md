# Foundation Architecture

## Escopo

Esta fundação implementa domínio e contratos, não gameplay. Ela define identidade procedural, determinismo, cognição parametrizada e pontos de extensão para decisões futuras. Renderização, rede, persistência, comportamento concreto e integração com assets permanecem fora deste marco.

## Organização

```text
MyGameWorld.Shared.Core
├── EntityId, ArchetypeId e AssetId
├── versões fortes
├── SimulationLod
├── DeterministicRandom
└── SeedDerivation
        ↓
MyGameWorld.Shared.EntityModel
└── EntityDNA
        ↓
MyGameWorld.Shared.Procedural
├── GenerationContext
├── IProceduralGenerator<TDNA, TResult>
├── ProceduralGenerator<TDNA, TResult>
├── AssetCatalog versionado
├── IAssetRegistry<TAsset>
└── WeightedAssetSelector

Core + EntityModel
        ↓
MyGameWorld.Shared.NpcCognition
├── NpcDNA
├── IntelligenceDNA / PersonalityDNA
├── IntelligenceCapabilityResolverV1
└── NpcBrain + INpcDecisionPolicy
```

Cada diretório possui um assembly definition próprio. As dependências são unidirecionais e nenhum assembly compartilhado referencia `UnityEngine`. Isso permite testar o domínio isoladamente e, no futuro, mover o mesmo código para processos de servidor.

## Contratos e invariantes

- `EntityId` e `ArchetypeId` devem ser positivos; `AssetId` zero é reservado.
- `AssetId` é uma identidade lógica persistente. Um ID publicado não pode ser reutilizado para outro asset, mesmo que o arquivo original seja removido.
- `GeneratorVersion` e `AssetCatalogVersion` começam em 1; zero é reservado para dados ausentes/inválidos.
- `EntityDNA` é imutável e contém somente identidade, arquétipo, seed e versões necessárias à reconstrução.
- Especializações, como `NpcDNA`, usam composição em vez de herança.
- Traits cognitivos usam inteiros entre 0 e 100; o nível geral de inteligência usa 0 a 10.
- Tempo cognitivo entra como `simulationTick`, nunca como relógio local.

## Determinismo e versionamento

`DeterministicRandom` usa SplitMix64 e `SeedDerivation` usa mistura inteira estável. Não se deve trocar constantes, ordem de chamadas ou regras de seleção dentro de uma versão existente. Mudanças que alterem resultados exigem uma nova `GeneratorVersion` e coexistência explícita entre implementações.

`GenerationContext` torna seed e versões dependências explícitas. `ProceduralGenerator` recusa versões incompatíveis antes de executar. O catálogo de assets também é versionado porque adicionar uma opção pode mudar seleções ponderadas.

`AssetCatalog` copia e valida suas entradas na construção, rejeita IDs repetidos e pesos zero e preserva a ordem como parte do contrato determinístico. `WeightedAssetSelector` usa o mesmo RNG versionado e seleção sem viés para limites de até 64 bits. O contrato genérico `IAssetRegistry<TAsset>` resolve o ID lógico para uma representação concreta; implementações Unity devem existir somente em assemblies de adapter do cliente.

Cada entrada pode carregar um `AssetDescriptor` com categoria, traits e uma regra de compatibilidade. Uma regra declara traits obrigatórios e proibidos no outro asset; `AssetCompatibilityEvaluator` verifica os dois lados da combinação. Os valores de `AssetCategory` e os bits de `AssetTrait` são persistentes: não devem ser renumerados nem reutilizados.

`MyGameWorld.Client.AssetResolution` é o primeiro adapter de engine. `UnityAssetCatalog` permite autoria como `ScriptableObject`, enquanto `UnityAssetRegistry` valida e copia os bindings de `AssetId` para `UnityEngine.Object`. Nenhuma dessas classes participa das regras autoritativas ou altera o catálogo procedural compartilhado.

## Cognição e escala

`IntelligenceCapabilityResolverV1` transforma `IntelligenceDNA` em um bitset sem allocations por capability. Os valores numéricos do enum são IDs persistentes e não podem ser reordenados. Novas regras incompatíveis devem entrar em outro resolver versionado.

`NpcBrain` não conhece sensores, memória, diálogo, rede ou ações. Ele recebe uma `INpcDecisionPolicy` e delega a decisão usando DNA, capabilities e LOD. Dessa forma, decisões autoritativas podem rodar no servidor, enquanto o cliente recebe apenas resultados de representação. Nenhuma política concreta é fornecida nesta fase para evitar gameplay prematuro.

## Serialização

Os modelos de domínio são imutáveis e não usam a serialização de campos do Unity. Persistência e rede deverão criar DTOs/adapters explícitos, com schema e versão próprios. Isso evita acoplar invariantes do domínio a uma engine ou formato de transporte.

## Próximos passos permitidos

Antes de gameplay: criar validação de autoria para os catálogos Unity, o primeiro gerador/compositor concreto, DTOs de rede, contratos de memória/percepção, tracing determinístico e benchmarks de alocação. Qualquer integração Unity deve permanecer em um assembly de adapter voltado ao cliente.
