# Foundation Architecture

## Escopo

Esta fundação implementa domínio e contratos, não gameplay. Ela define identidade procedural, determinismo, cognição parametrizada e pontos de extensão para decisões futuras. Renderização, rede, persistência, comportamento concreto e integração com assets permanecem fora deste marco.

## Organização

```text
MyGameWorld.Shared.Core
├── IDs e versões fortes
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
└── ProceduralGenerator<TDNA, TResult>

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

- `EntityId` e `ArchetypeId` devem ser positivos.
- `GeneratorVersion` e `AssetCatalogVersion` começam em 1; zero é reservado para dados ausentes/inválidos.
- `EntityDNA` é imutável e contém somente identidade, arquétipo, seed e versões necessárias à reconstrução.
- Especializações, como `NpcDNA`, usam composição em vez de herança.
- Traits cognitivos usam inteiros entre 0 e 100; o nível geral de inteligência usa 0 a 10.
- Tempo cognitivo entra como `simulationTick`, nunca como relógio local.

## Determinismo e versionamento

`DeterministicRandom` usa SplitMix64 e `SeedDerivation` usa mistura inteira estável. Não se deve trocar constantes, ordem de chamadas ou regras de seleção dentro de uma versão existente. Mudanças que alterem resultados exigem uma nova `GeneratorVersion` e coexistência explícita entre implementações.

`GenerationContext` torna seed e versões dependências explícitas. `ProceduralGenerator` recusa versões incompatíveis antes de executar. O catálogo de assets também é versionado porque adicionar uma opção pode mudar seleções ponderadas.

## Cognição e escala

`IntelligenceCapabilityResolverV1` transforma `IntelligenceDNA` em um bitset sem allocations por capability. Os valores numéricos do enum são IDs persistentes e não podem ser reordenados. Novas regras incompatíveis devem entrar em outro resolver versionado.

`NpcBrain` não conhece sensores, memória, diálogo, rede ou ações. Ele recebe uma `INpcDecisionPolicy` e delega a decisão usando DNA, capabilities e LOD. Dessa forma, decisões autoritativas podem rodar no servidor, enquanto o cliente recebe apenas resultados de representação. Nenhuma política concreta é fornecida nesta fase para evitar gameplay prematuro.

## Serialização

Os modelos de domínio são imutáveis e não usam a serialização de campos do Unity. Persistência e rede deverão criar DTOs/adapters explícitos, com schema e versão próprios. Isso evita acoplar invariantes do domínio a uma engine ou formato de transporte.

## Próximos passos permitidos

Antes de gameplay: definir DTOs de rede, catálogo de assets, contratos de memória/percepção, tracing determinístico e benchmarks de alocação. Qualquer integração Unity deve permanecer em um assembly de adapter voltado ao cliente.

