# Roadmap

## 1. Objetivo

Este roadmap organiza a evolução inicial do projeto como uma base tecnológica de MMORPG procedural.

O foco inicial não é conteúdo final.

O foco é provar os pilares técnicos.

---

# Phase 0 — Project Bootstrap

## Objetivo

Dar ao projeto uma estrutura profissional e consistente.

### Entregáveis

- [x] definir nome provisório;
- [x] criar repositório;
- [x] adicionar `AGENTS.md`;
- [x] adicionar documentação inicial;
- [x] definir linguagem e engine;
- [x] definir padrão de módulos;
- [x] configurar formatter;
- [ ] configurar linter;
- [ ] configurar build;
- [x] configurar CI;
- [ ] definir logging;
- [x] definir conventions.

### Resultado

Projeto compilável, organizado e preparado para desenvolvimento contínuo.

---

# Phase 1 — Core Domain

## Objetivo

Criar os modelos fundamentais sem dependência gráfica.

### Implementar

- [x] `EntityId`;
- [x] `AssetId`;
- [x] `EntityDNA`;
- [ ] `CharacterDNA`;
- [x] `NpcDNA`;
- [ ] `MobDNA`;
- [x] `GeneratorVersion`;
- [x] `AssetCatalogVersion`;
- [x] `GenerationContext`;
- [x] `SimulationLOD`.

### Resultado

Modelos capazes de representar entidades proceduralmente.

---

# Phase 2 — Deterministic Procedural Runtime

## Objetivo

Materializar uma entidade determinística.

### Implementar

- [x] deterministic RNG;
- [x] seed derivation;
- [x] `ProceduralGenerator`;
- [x] contrato `IAssetRegistry<TAsset>`;
- [ ] compatibility rules;
- [x] weighted asset selection;
- [x] generator versioning;
- [x] catalog versioning.

### Prova

```text
Same DNA
+
Same Generator Version
+
Same Catalog
=
Same Entity
```

### Resultado

Primeiro runtime procedural funcional.

---

# Phase 3 — Character Composer

## Objetivo

Montar personagens em runtime.

### Implementar

- [ ] body;
- [ ] head;
- [ ] hair;
- [ ] skin;
- [ ] equipment;
- [ ] accessories;
- [ ] materials;
- [ ] basic morph parameters;
- [ ] skeleton binding.

### Resultado

Gerar dezenas ou centenas de personagens visualmente distintos através do mesmo catálogo.

---

# Phase 4 — Animation Runtime

## Objetivo

Criar um sistema comum de animação.

### Implementar

- [ ] base skeleton;
- [ ] animation profile;
- [ ] idle;
- [ ] walk;
- [ ] run;
- [ ] turn;
- [ ] attack;
- [ ] animation blending;
- [ ] retargeting;
- [ ] basic IK.

### Resultado

Personagens de diferentes proporções utilizando o mesmo conjunto lógico de animações.

---

# Phase 5 — NPC Intelligence Foundation

## Objetivo

Criar o primeiro `NPCBrain`.

### Implementar

- [x] `IntelligenceDNA`;
- [x] `PersonalityDNA`;
- [ ] perception;
- [ ] attention;
- [ ] memory;
- [ ] needs;
- [ ] goals;
- [ ] utility scoring;
- [x] decision output neutro e política injetável;
- [ ] interaction gate.

### Primeiro cenário

Dois NPCs recebem o mesmo evento:

```text
Player enters area
```

NPC A:

```text
Observe
```

NPC B:

```text
Greet
```

NPC C:

```text
Flee
```

### Resultado

Demonstração de comportamento individual.

---

# Phase 6 — NPC Memory and Knowledge

## Objetivo

Fazer NPCs lembrarem e conhecerem apenas partes do mundo.

### Implementar

- [ ] short-term memory;
- [ ] long-term memory;
- [ ] relationship memory;
- [ ] memory decay;
- [ ] knowledge domains;
- [ ] rumor system;
- [ ] reputation awareness.

### Resultado

NPC reage de forma diferente com base em acontecimentos anteriores.

---

# Phase 7 — Interaction System

## Objetivo

Permitir interação contextual player/NPC.

### Implementar

- [ ] interaction states;
- [ ] talk eligibility;
- [ ] trade eligibility;
- [ ] negotiation eligibility;
- [ ] hostile response;
- [ ] fear response;
- [ ] social context;
- [ ] dialogue intent.

### Resultado

NPCs decidem quando e como interagir.

---

# Phase 8 — Procedural Mobs

## Objetivo

Criar mobs através do mesmo conceito de DNA.

### Implementar

- [ ] archetype;
- [ ] seed;
- [ ] visual variants;
- [ ] equipment;
- [ ] mutations;
- [ ] ability profile;
- [ ] behavior profile.

### Resultado

Mobs variáveis, determinísticos e baratos em armazenamento.

---

# Phase 9 — World Generation Foundation

## Objetivo

Criar primeira zona procedural.

### Implementar

- [ ] `ZoneDNA`;
- [ ] zone seed;
- [ ] biome;
- [ ] terrain;
- [ ] vegetation;
- [ ] spawn points;
- [ ] basic structures;
- [ ] deterministic placement.

### Resultado

Mesma seed recria exatamente a mesma zona.

---

# Phase 10 — World Delta

## Objetivo

Separar mundo base de mudanças persistentes.

### Implementar

- [ ] `WorldDelta`;
- [ ] entity destroyed;
- [ ] resource collected;
- [ ] structure changed;
- [ ] delta application;
- [ ] delta persistence.

### Resultado

O mundo procedural pode mudar sem perder reprodutibilidade.

---

# Phase 11 — Simulation LOD

## Objetivo

Escalar grande quantidade de NPCs.

### Implementar

- [ ] LOD 0 full;
- [ ] LOD 1 reduced update;
- [ ] LOD 2 simplified behavior;
- [ ] LOD 3 statistical simulation;
- [ ] transitions;
- [ ] debug visualization.

### Resultado

Centenas ou milhares de NPCs sem simulação completa constante.

---

# Phase 12 — Networking Foundation

## Objetivo

Criar primeiro fluxo client/server.

### Implementar

- [ ] connection;
- [ ] protocol version;
- [ ] spawn;
- [ ] despawn;
- [ ] transform sync;
- [ ] DNA sync;
- [ ] entity state;
- [ ] basic snapshot;
- [ ] deltas.

### Resultado

Servidor envia identidade compacta e cliente materializa entidades.

---

# Phase 13 — Server Authority

## Objetivo

Remover decisões críticas do cliente.

### Implementar

- [ ] authoritative movement validation;
- [ ] health;
- [ ] damage;
- [ ] death;
- [ ] inventory;
- [ ] loot;
- [ ] basic progression.

### Resultado

Primeira vertical slice multiplayer segura.

---

# Phase 14 — Interest Management

## Objetivo

Evitar replicação global.

### Implementar

- [ ] zones;
- [ ] proximity;
- [ ] subscriptions;
- [ ] entity visibility set;
- [ ] spawn/despawn by interest.

### Resultado

Escalabilidade inicial da rede.

---

# Phase 15 — Persistence

## Objetivo

Persistir estado crítico.

### Implementar

- [ ] account;
- [ ] player;
- [ ] inventory;
- [ ] progression;
- [ ] NPC important memory;
- [ ] relationship;
- [ ] WorldDelta.

---

# Phase 16 — Tooling

## Objetivo

Criar ferramentas para desenvolvimento.

### Implementar

- [ ] Seed Inspector;
- [ ] Entity Preview;
- [ ] NPC Inspector;
- [ ] Decision Trace;
- [ ] World Debugger;
- [ ] Determinism Checker;
- [ ] Asset Validator.

---

# Phase 17 — First Playable Slice

## Objetivo

Criar uma pequena região jogável.

### Conteúdo

- 1 pequena vila;
- 1 zona externa;
- 5–10 tipos de NPC;
- 3–5 tipos de mobs;
- criação de personagem;
- combate básico;
- comércio;
- memória de NPC;
- reputação simples;
- persistência;
- multiplayer.

### Critério

A slice deve demonstrar os pilares tecnológicos, não quantidade de conteúdo.

---

# Phase 18 — Advanced NPC Cognition

## Implementar

- [ ] multi-step planning;
- [ ] social inference;
- [ ] faction knowledge;
- [ ] gossip propagation;
- [ ] deception;
- [ ] trust;
- [ ] negotiation;
- [ ] collective behavior.

---

# Phase 19 — Optional LLM Layer

## Objetivo

Adicionar linguagem natural sem entregar autoridade à IA generativa.

### Pipeline

```text
NPC Brain
   ↓
Dialogue Intent
   ↓
Structured Context
   ↓
LLM
   ↓
Natural Language
```

### Nunca delegar ao LLM

- combate;
- inventário;
- loot;
- economia;
- ownership;
- progressão;
- autoridade.

---

# Phase 20 — Scaling

## Evoluções futuras

- [ ] zone servers;
- [ ] world coordinator;
- [ ] sharding;
- [ ] distributed persistence;
- [ ] matchmaking;
- [ ] guilds;
- [ ] market;
- [ ] territory;
- [ ] global events;
- [ ] large population simulation.

---

# Prioridade Inicial

A sequência recomendada é:

```text
Core Domain
    ↓
Procedural Runtime
    ↓
Character Composer
    ↓
NPC Brain
    ↓
Procedural World
    ↓
Simulation LOD
    ↓
Networking
    ↓
Server Authority
```

Não iniciar por:

```text
quests complexas
economia completa
crafting
guildas
PvP
grandes mapas
centenas de itens
```

antes de validar os pilares técnicos.

---

# Primeira Meta Concreta

O primeiro marco importante do projeto deve ser:

> Executar cliente e servidor, entrar em uma pequena zona, receber do servidor o DNA de três NPCs, materializar visualmente os três no cliente, fazê-los reagir de maneira diferente ao jogador e reconstruir exatamente o mesmo cenário após reiniciar a aplicação.

Quando isso funcionar, a fundação principal estará provada.
