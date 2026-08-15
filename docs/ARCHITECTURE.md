# Architecture

> Estado implementado e decisões detalhadas: consulte [Foundation Architecture](FOUNDATION_ARCHITECTURE.md) e os registros em [`adr/`](adr/).

## 1. Objetivo

Este documento define a arquitetura inicial do projeto e os limites entre os principais módulos.

A prioridade é manter:

- baixo acoplamento;
- forte separação de responsabilidades;
- determinismo;
- testabilidade;
- escalabilidade;
- versionamento;
- compatibilidade com multiplayer autoritativo.

---

## 2. Visão de Alto Nível

```text
                       MMORPG SERVER
                            │
              ┌─────────────┼─────────────┐
              │             │             │
         Persistence    Authority      Network
              │             │             │
              └─────────────┼─────────────┘
                            │
                       Game State
                            │
                      DNA / IDs / Seed
                            │
                            ▼
                       GAME CLIENT
                            │
                   Procedural Runtime
                            │
          ┌─────────────────┼──────────────────┐
          │                 │                  │
    Entity Runtime      World Runtime      Animation
          │                 │                  │
          └─────────────────┼──────────────────┘
                            ▼
                         Renderer
```

---

## 3. Módulos Propostos

```text
/
├── client/
├── server/
├── shared/
├── tools/
└── docs/
```

Estrutura sugerida:

```text
shared/
├── game-core/
├── entity-model/
├── procedural-contract/
├── network-contract/
└── simulation-model/

client/
├── asset-registry/
├── entity-runtime/
├── character-runtime/
├── mob-runtime/
├── procedural-world/
├── animation-runtime/
├── client-network/
└── client-debug/

server/
├── auth/
├── player-service/
├── world-service/
├── entity-service/
├── npc-service/
├── combat-service/
├── economy-service/
├── persistence/
└── server-network/

tools/
├── asset-validator/
├── seed-inspector/
├── entity-preview/
├── world-debugger/
└── content-pipeline/
```

---

## 4. Shared Layer

A camada compartilhada contém modelos e contratos.

Ela não deve depender de engine gráfica.

Responsabilidades:

- identificadores;
- enums;
- DNA;
- mensagens de rede;
- eventos;
- snapshots;
- interfaces de geração;
- versionamento;
- tipos matemáticos neutros quando necessário.

---

## 5. Entity Model

Estrutura base:

```kotlin
data class EntityDNA(
    val entityId: Long,
    val archetypeId: Int,
    val seed: Long,
    val generatorVersion: Int
)
```

Extensões:

```text
EntityDNA
 ├── CharacterDNA
 ├── NpcDNA
 ├── MobDNA
 ├── CreatureDNA
 ├── ItemDNA
 └── StructureDNA
```

Cada tipo deve carregar somente os dados necessários para reproduzir seu estado base.

---

## 6. Procedural Contract

Contrato inicial:

```kotlin
interface ProceduralGenerator<DNA, RESULT> {

    fun generate(
        dna: DNA,
        context: GenerationContext
    ): RESULT
}
```

O gerador deve ser:

- determinístico;
- stateless quando possível;
- dependente explicitamente da versão;
- validável;
- reprodutível.

---

## 7. Asset Registry

O cliente precisa de uma camada de resolução de assets.

Exemplo:

```text
AssetId
   ↓
AssetRegistry
   ↓
AssetDescriptor
   ↓
Engine Resource
```

Nunca espalhar caminhos físicos de arquivos por código de gameplay.

Exemplo:

```kotlin
interface AssetRegistry {
    fun resolve(id: AssetId): AssetDescriptor
}
```

---

## 8. Runtime Entity

`EntityDNA` representa identidade e composição.

`RuntimeEntity` representa a entidade materializada.

```text
EntityDNA
   ↓
Generator
   ↓
RuntimeEntity
   ├── Transform
   ├── Visual
   ├── Animation
   ├── Physics
   ├── Interaction
   └── Runtime State
```

O runtime é descartável.

O DNA é reconstruível.

---

## 9. World Runtime

Responsabilidades:

- carregar zonas;
- gerar conteúdo base;
- aplicar `WorldDelta`;
- materializar entidades;
- gerenciar streaming;
- descarregar regiões;
- controlar simulation LOD.

---

## 10. Simulation LOD

A simulação deve operar em camadas.

```text
LOD 0
Full simulation

LOD 1
Reduced frequency

LOD 2
Simplified behavioral simulation

LOD 3
Statistical/offline simulation
```

Critérios podem considerar:

- distância;
- relevância;
- visibilidade;
- participação em combate;
- interação com players;
- importância narrativa;
- densidade regional.

---

## 11. NPC Architecture

```text
NpcEntity
   │
   ├── NpcDNA
   ├── NPCBrain
   ├── Memory
   ├── Knowledge
   ├── Personality
   ├── Goals
   └── Runtime Context
```

`NPCBrain` não deve conter estado visual.

---

## 12. Client Responsibilities

O cliente pode controlar:

- render;
- animação;
- look-at;
- efeitos;
- UI;
- áudio;
- interpolação;
- composição visual;
- animações cosméticas;
- comportamento local não crítico.

O cliente não deve possuir autoridade final sobre:

- dano;
- morte;
- loot;
- inventário;
- moeda;
- XP;
- quest state;
- atributos persistentes;
- ownership;
- posição validada;
- resultados competitivos.

---

## 13. Server Responsibilities

O servidor deve controlar:

- autenticação;
- identidade;
- posição validada;
- estado persistente;
- combate;
- spawn autoritativo;
- loot;
- economia;
- progressão;
- inventário;
- quests;
- relações persistentes;
- WorldDelta;
- eventos globais;
- decisões críticas de NPC.

---

## 14. Event-Driven Core

Sistemas devem preferencialmente comunicar mudanças através de eventos.

Exemplos:

```text
PlayerEnteredZone
EntitySpawned
EntityDamaged
NpcObservedPlayer
NpcMemoryCreated
QuestCompleted
WorldObjectDestroyed
FactionReputationChanged
```

Isso reduz dependência direta entre sistemas.

---

## 15. Determinismo

Determinismo exige:

```text
Seed
+ GeneratorVersion
+ AssetCatalogVersion
+ Input Parameters
= Same Output
```

Nunca depender de:

- horário local;
- ordem imprevisível de coleções;
- RNG global;
- valores não inicializados;
- estado externo não declarado.

---

## 16. Versionamento

Todos os sistemas procedurais precisam de versão.

Exemplo:

```text
CharacterGeneratorV1
CharacterGeneratorV2

MobGeneratorV1
MobGeneratorV2
```

O DNA armazena:

```text
generatorVersion
```

A migração de versões deverá ser explícita.

---

## 17. Observabilidade

Desde o início, o projeto deve suportar:

- logs estruturados;
- entity inspection;
- seed inspection;
- generation trace;
- debug overlays;
- network metrics;
- AI decision trace;
- simulation LOD visualization.

---

## 18. Regras Arquiteturais

1. Gameplay não acessa arquivos diretamente.
2. DNA não depende da engine.
3. Renderer não decide regras de jogo.
4. Servidor não depende de representação visual.
5. Todo gerador procedural deve possuir seed explícita.
6. Toda mudança de algoritmo procedural deve considerar versionamento.
7. Estados críticos são server-authoritative.
8. Simulações caras devem suportar degradação por LOD.
9. Sistemas de NPC devem ser desacoplados de diálogo.
10. LLM, se utilizado, nunca será autoridade de gameplay.

---

## 19. Primeiros Contratos a Implementar

- `EntityId`
- `AssetId`
- `EntityDNA`
- `NpcDNA`
- `GeneratorVersion`
- `AssetCatalogVersion`
- `GenerationContext`
- `ProceduralGenerator`
- `AssetRegistry`
- `NPCBrain`
- `NpcIntelligenceDNA`
- `PersonalityDNA`
- `InteractionResult`
- `SimulationLOD`
- `WorldDelta`
- `EntitySnapshot`
- `NetworkEntityState`

---

## 20. Direção de Evolução

A arquitetura inicial deve permitir posteriormente:

- multithreading;
- ECS;
- zone servers;
- sharding;
- instance servers;
- interest management;
- replay;
- deterministic simulation;
- rollback em sistemas específicos;
- modding controlado;
- geração assistida por IA;
- ferramentas de autoria.
