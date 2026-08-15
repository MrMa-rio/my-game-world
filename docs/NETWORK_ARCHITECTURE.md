# Network Architecture

## 1. Objetivo

A arquitetura de rede deve suportar um MMORPG onde o servidor mantém a verdade do mundo e o cliente reconstrói representações visuais através de dados compactos.

Princípio:

```text
Server-Authoritative State
+
Client-Generated Representation
```

---

## 2. Separação de Responsabilidades

Servidor controla:

- identidade;
- autenticação;
- posição validada;
- combate;
- HP;
- morte;
- loot;
- inventário;
- economia;
- XP;
- progressão;
- quests;
- NPC state relevante;
- WorldDelta;
- ownership;
- eventos globais.

Cliente controla:

- render;
- animação;
- efeitos;
- interpolação;
- UI;
- composição procedural;
- materialização de entidades;
- áudio;
- cosméticos locais.

---

## 3. Entidade em Rede

O servidor não transmite mesh.

Transmite descrição compacta.

Exemplo conceitual:

```json
{
  "entityId": 81292,
  "archetype": 4,
  "seed": 827192812,
  "generatorVersion": 2,
  "catalogVersion": 8,
  "position": [100.2, 2.0, 889.5],
  "state": "IDLE"
}
```

O cliente resolve o visual.

---

## 4. Binário

Para produção, mensagens críticas devem preferencialmente utilizar representação binária compacta.

Exemplo:

```text
entityId          8 bytes
archetype         2 bytes
seed              8 bytes
generatorVersion  2 bytes
catalogVersion    2 bytes
state             1 byte
...
```

---

## 5. Snapshot e Delta

Primeira entrada em região:

```text
Zone Snapshot
```

Atualizações posteriores:

```text
State Delta
```

Pipeline:

```text
Initial Snapshot
      ↓
Client Materialization
      ↓
Incremental Deltas
      ↓
Runtime Updates
```

---

## 6. Interest Management

O servidor não deve enviar todas as entidades para todos os jogadores.

Critérios:

- distância;
- zona;
- visibilidade;
- party;
- combate;
- eventos;
- relevância.

```text
Player
  ↓
Interest Set
  ↓
Relevant Entities
  ↓
Network Stream
```

---

## 7. Spatial Partitioning

Estratégias possíveis:

- grid;
- quadtree;
- octree;
- zone partition;
- region partition.

A primeira versão pode utilizar zonas e células.

---

## 8. Entity Spawn

Mensagem conceitual:

```text
EntitySpawn
 ├── entityId
 ├── entityType
 ├── archetype
 ├── seed
 ├── generatorVersion
 ├── catalogVersion
 ├── transform
 └── state
```

O cliente:

```text
EntitySpawn
    ↓
Resolve DNA
    ↓
Procedural Runtime
    ↓
Runtime Entity
```

---

## 9. Entity Update

Atualizações frequentes devem conter somente o necessário.

Exemplo:

```text
EntityStateDelta
 ├── entityId
 ├── position
 ├── rotation
 ├── movementState
 └── combatState
```

---

## 10. World Streaming

Ao entrar em nova zona:

```text
Client
   ↓
ZoneEnter
   ↓
Server
   ↓
ZoneMetadata
+ WorldDelta
+ Relevant Entities
   ↓
Client
   ↓
Generate Base Zone
+ Apply Delta
+ Spawn Entities
```

---

## 11. World Delta

O servidor persiste mudanças.

Exemplos:

- ponte destruída;
- porta aberta;
- recurso coletado;
- construção criada;
- NPC morto;
- território conquistado.

O cliente nunca altera o mundo persistente sem validação do servidor.

---

## 12. Movement

Movimento pode utilizar:

```text
Client Prediction
+
Server Validation
+
Reconciliation
```

Fluxo:

```text
Input
 ↓
Client predicts
 ↓
Send command
 ↓
Server validates
 ↓
Authoritative state
 ↓
Client reconciles
```

---

## 13. Combat

Combat deve ser autoritativo.

Cliente envia intenção:

```text
AttackRequest
```

Servidor resolve:

```text
Hit validation
Damage
Critical
Status effect
Death
Loot
```

Cliente apenas representa.

---

## 14. NPC Network State

Nem todo estado mental do NPC precisa ser transmitido.

O cliente precisa apenas do necessário para representação.

Exemplo:

```text
NpcState
 ├── currentAction
 ├── target
 ├── movement
 ├── interactionState
 └── animationHint
```

Memória e raciocínio permanecem no servidor quando relevantes.

---

## 15. AI Authority

Decisões que podem afetar gameplay devem ser server-side.

Exemplos:

- atacar;
- fugir;
- vender;
- conceder recompensa;
- mudar reputação;
- entregar quest;
- escolher loot.

Reações puramente visuais podem ser client-side.

---

## 16. Protocol Layers

Estrutura sugerida:

```text
Transport
   ↓
Session
   ↓
Authentication
   ↓
Replication
   ↓
Gameplay Messages
   ↓
Domain Events
```

---

## 17. Transport

A tecnologia será definida após requisitos mais concretos.

Candidatos:

- UDP custom;
- ENet;
- QUIC;
- reliable UDP;
- TCP para canais específicos;
- WebSocket apenas se houver necessidade específica.

Não assumir protocolo definitivo antes dos testes de latência e escalabilidade.

---

## 18. Canais Lógicos

Separar semanticamente:

```text
AUTH
WORLD
ENTITY
MOVEMENT
COMBAT
CHAT
SOCIAL
ECONOMY
```

Mesmo que compartilhem transporte.

---

## 19. Reliability

Dados que exigem confiabilidade:

- login;
- inventário;
- compra;
- venda;
- quest;
- loot;
- trade;
- spawn/despawn importantes.

Dados que podem tolerar perda:

- transform updates frequentes;
- aim;
- cosmetic state;
- interpolação.

---

## 20. Security

O servidor nunca confia integralmente no cliente.

Validar:

- velocidade;
- distância;
- cooldown;
- recursos;
- inventário;
- hit;
- posição;
- ownership;
- permissões;
- rate limits.

---

## 21. Versionamento de Protocolo

Toda conexão deve declarar versão.

```text
protocolVersion
clientVersion
assetCatalogVersion
generatorVersionSet
```

O servidor pode rejeitar incompatibilidades.

---

## 22. Asset Compatibility

Se o cliente não possuir o catálogo necessário:

```text
Server Entity DNA
+
Wrong Catalog
=
Wrong Visual
```

Portanto o handshake deve validar versão do catálogo.

---

## 23. Persistence

Servidor persiste:

```text
PlayerState
Inventory
Progression
Important NPC Memory
Relationships
WorldDelta
Economy
Guilds
Territory
```

Não persiste necessariamente:

```text
Mesh
Texture
Animation Instance
Rendered Object
```

---

## 24. Zone Servers

Evolução futura:

```text
Gateway
   ↓
World Coordinator
   ↓
Zone Server A
Zone Server B
Zone Server C
```

Players podem ser transferidos entre zonas.

---

## 25. Sharding

Se necessário:

```text
World
 ├── Shard 1
 ├── Shard 2
 └── Shard 3
```

A decisão será posterior.

---

## 26. Observabilidade

Métricas:

- ping;
- RTT;
- packet loss;
- bandwidth/player;
- messages/sec;
- bytes/entity;
- interest set size;
- snapshot size;
- replication delay;
- reconciliation frequency;
- disconnect causes.

---

## 27. Objetivo Inicial de Rede

Primeira milestone:

1. login fake/local;
2. conectar cliente e servidor;
3. spawn de player;
4. spawn de NPC;
5. sincronizar posição;
6. enviar DNA compacto;
7. materializar entidade no cliente;
8. despawn;
9. zone snapshot simples;
10. delta incremental.
