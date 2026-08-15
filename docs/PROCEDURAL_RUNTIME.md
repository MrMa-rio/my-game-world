# Procedural Runtime

## 1. Objetivo

O Procedural Runtime é responsável por transformar descrições compactas em entidades e estruturas completas no cliente.

A regra principal é:

> Nenhum conteúdo procedural importante deve depender de geração pesada de assets em runtime.

O runtime combina assets previamente existentes.

---

## 2. Entrada e Saída

Entrada:

```text
EntityDNA
+ GeneratorVersion
+ AssetCatalog
+ Context
```

Saída:

```text
RuntimeEntity
```

Exemplo:

```text
CharacterDNA
   ↓
CharacterGenerator
   ↓
Asset Selection
   ↓
Morph Application
   ↓
Material Composition
   ↓
Equipment Attachment
   ↓
Rig Binding
   ↓
Animation Profile
   ↓
Runtime Character
```

---

## 3. Determinismo

Toda seleção pseudoaleatória deverá usar RNG baseado em seed.

Nunca utilizar RNG global para decisões de geração.

Exemplo conceitual:

```kotlin
val rng = DeterministicRandom(dna.seed)

val head = heads[rng.nextInt(heads.size)]
val hair = hairs[rng.nextInt(hairs.size)]
val armor = armors[rng.nextInt(armors.size)]
```

---

## 4. Hierarquia de Seeds

```text
WorldSeed
 └── RegionSeed
     └── ZoneSeed
         ├── SettlementSeed
         ├── VegetationSeed
         ├── MobSeed
         └── NpcSeed
```

Seeds filhas devem ser derivadas de maneira estável.

Exemplo conceitual:

```text
childSeed = hash(parentSeed, entityType, localId)
```

---

## 5. EntityDNA

Exemplo genérico:

```kotlin
data class EntityDNA(
    val entityId: Long,
    val archetypeId: Int,
    val seed: Long,
    val generatorVersion: Int,
    val assetCatalogVersion: Int
)
```

---

## 6. CharacterDNA

Exemplo:

```kotlin
data class CharacterDNA(
    val base: EntityDNA,
    val bodyId: Int?,
    val headId: Int?,
    val hairId: Int?,
    val skinVariant: Int?,
    val height: Float,
    val bodyScale: Float,
    val equipment: EquipmentDNA
)
```

Campos nulos podem ser resolvidos proceduralmente.

Campos explícitos sobrescrevem a escolha procedural.

---

## 7. Asset Catalog

Cada asset deve possuir metadados.

```text
AssetDescriptor
 ├── id
 ├── type
 ├── category
 ├── tags
 ├── compatibility
 ├── rarity
 ├── weight
 ├── skeleton
 ├── sockets
 └── version
```

---

## 8. Compatibilidade

Combinações inválidas devem ser prevenidas pelo catálogo.

Exemplos:

- cabelo incompatível com determinada cabeça;
- armadura incompatível com skeleton;
- espada incompatível com raça de tamanho extremo;
- telhado incompatível com determinada base;
- acessório incompatível com outro acessório.

---

## 9. Weighted Selection

Nem todo asset deve possuir a mesma chance.

```text
CommonHair      weight 80
RareHair        weight 15
UniqueHair      weight 5
```

A seleção ponderada deve permanecer determinística.

---

## 10. Runtime Assembly

Pipeline:

```text
Resolve Archetype
      ↓
Resolve Generator Version
      ↓
Initialize Seed
      ↓
Select Compatible Assets
      ↓
Apply Morphology
      ↓
Apply Materials
      ↓
Attach Equipment
      ↓
Attach Accessories
      ↓
Bind Skeleton
      ↓
Configure Animation
      ↓
Create Physics
      ↓
Return Runtime Entity
```

---

## 11. Procedural Mobs

Entrada típica:

```text
MobDNA
 ├── archetype
 ├── seed
 ├── level
 ├── region
 ├── mutation
 └── equipmentTier
```

Resultado:

```text
Mob Runtime
 ├── body
 ├── head
 ├── skin
 ├── mutations
 ├── equipment
 ├── animation profile
 └── visual effects
```

---

## 12. Procedural Structures

Estruturas devem ser construídas por módulos compatíveis.

Exemplo:

```text
BuildingDefinition
   ↓
Foundation
   ↓
Wall Modules
   ↓
Door/Window Rules
   ↓
Roof
   ↓
Interior Layout
   ↓
Decoration
```

Módulos:

```text
Wall_A
Wall_B
Door_A
Window_A
Roof_A
Beam_A
Floor_A
```

---

## 13. Procedural World

Uma zona pode ser definida por:

```kotlin
data class ZoneDNA(
    val zoneId: Long,
    val seed: Long,
    val biomeId: Int,
    val cultureId: Int,
    val dangerLevel: Int,
    val generatorVersion: Int
)
```

Pipeline:

```text
ZoneDNA
   ↓
Terrain
   ↓
Biome Mask
   ↓
Water
   ↓
Roads
   ↓
Vegetation
   ↓
Structures
   ↓
Spawn Points
   ↓
Population
```

---

## 14. World Delta

Mudanças persistentes não alteram a seed base.

Exemplo:

```json
{
  "zoneId": 284,
  "changes": [
    {
      "entityId": 8172,
      "state": "DESTROYED"
    }
  ]
}
```

Resultado:

```text
Generated Zone
   +
WorldDelta
   =
Current Zone
```

---

## 15. Cache

O cliente deve poder cachear resultados de geração.

Tipos:

- entity composition cache;
- material cache;
- mesh combination cache;
- animation binding cache;
- zone cache.

Cache nunca deve mudar o resultado lógico.

---

## 16. Streaming

Zonas devem ser carregadas progressivamente.

```text
Player Position
    ↓
Interest Radius
    ↓
Zone Streaming
    ↓
Generate / Load Cache
    ↓
Apply Delta
    ↓
Materialize Entities
```

---

## 17. Simulation vs Representation

Não confundir:

```text
Entity State
```

com:

```text
Entity Representation
```

Uma entidade pode existir no servidor sem estar renderizada.

Uma entidade pode estar em simulation LOD baixo sem possuir mesh materializada.

---

## 18. Procedural Versioning

Cada alteração de algoritmo que possa mudar resultados exige nova versão.

```text
Generator V1
Seed 123
→ Result A

Generator V2
Seed 123
→ Result B
```

Ambos devem coexistir enquanto necessário.

---

## 19. Asset Catalog Versioning

O catálogo também precisa ser versionado.

Adicionar novos assets sem controle pode alterar seleções ponderadas antigas.

Por isso:

```text
Seed
+ GeneratorVersion
+ AssetCatalogVersion
```

devem definir completamente o resultado.

---

## 20. Debug

Ferramentas mínimas:

### Seed Inspector

Entrada:

```text
seed
generatorVersion
catalogVersion
```

Saída:

```text
resolved assets
selection trace
compatibility filters
final DNA
```

### Entity Preview

Permite materializar qualquer `EntityDNA`.

### Determinism Checker

Executa a mesma geração repetidamente e compara outputs.

---

## 21. Performance

Prioridades:

1. evitar criação repetida de recursos;
2. reutilizar materiais;
3. usar pooling;
4. evitar recomposição desnecessária;
5. aplicar LOD;
6. cachear resultados;
7. descarregar zonas;
8. utilizar processamento assíncrono quando permitido pela engine;
9. minimizar allocations;
10. separar geração lógica de materialização gráfica.

---

## 22. Regra de Ouro

> O Procedural Runtime deve ser capaz de reconstruir qualquer entidade compatível usando apenas dados compactos, versões conhecidas e o catálogo local de assets.
