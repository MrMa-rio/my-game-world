# Game Vision

## 1. Visão Geral

Este projeto tem como objetivo construir um MMORPG leve, escalável e altamente dinâmico, baseado em composição procedural determinística de conteúdo.

O jogo não dependerá de geração pesada de assets em tempo real. Todos os assets visuais fundamentais — corpos, cabeças, cabelos, armaduras, armas, estruturas, vegetação, elementos de cenário, animações e materiais — existirão previamente no cliente.

A variabilidade visual e comportamental será criada em runtime por meio da combinação desses assets a partir de seeds, DNA de entidades, regras de composição e estados persistidos.

O objetivo central é obter um mundo visualmente amplo, vivo e variável sem exigir uma quantidade proporcionalmente gigantesca de arquivos, modelos ou conteúdo artesanal.

---

## 2. Princípios Fundamentais

1. **Assets finitos, combinações massivas**
   - O jogo possui um catálogo limitado e controlado de assets.
   - As entidades são montadas dinamicamente em runtime.
   - A combinação entre peças, materiais, escala, morfologia e parâmetros permite uma enorme variedade.

2. **Geração determinística**
   - A mesma seed, versão do gerador e catálogo de assets devem produzir o mesmo resultado.
   - Entidades não precisam persistir sua representação visual completa no servidor.

3. **Servidor autoritativo**
   - O servidor é responsável pelas regras críticas de gameplay.
   - O cliente não determina resultados de combate, economia, loot, progressão ou estados persistentes.

4. **Cliente responsável pela representação**
   - O cliente monta entidades, estruturas e elementos visuais.
   - O servidor transmite apenas estados, IDs, seeds, modificadores e dados essenciais.

5. **NPCs com inteligência parametrizada**
   - NPCs compartilham sistemas cognitivos genéricos.
   - Cada NPC possui características mentais próprias.
   - Inteligência, memória, personalidade, conhecimento, profissão, facção e contexto influenciam o comportamento.

6. **Simulação escalável**
   - Sistemas de AI LOD e simulation LOD reduzem custo computacional.
   - Entidades distantes podem ser representadas estatisticamente em vez de simuladas integralmente.

7. **Arquitetura orientada a evolução**
   - O projeto não será tratado como protótipo descartável.
   - Sistemas fundamentais devem nascer desacoplados e versionáveis.

---

## 3. Conceito de Mundo

O mundo será dividido em regiões, zonas ou células.

Cada zona poderá possuir:

- seed;
- bioma;
- cultura;
- clima;
- facção predominante;
- nível de perigo;
- assentamentos;
- recursos;
- fauna;
- flora;
- eventos;
- modificações persistentes.

A geração base é determinística.

Exemplo:

```text
WorldSeed
   ↓
RegionSeed
   ↓
ZoneSeed
   ↓
Biome
   ↓
Terrain
   ↓
Vegetation
   ↓
Structures
   ↓
Population
```

Alterações realizadas por jogadores ou por sistemas persistentes não substituem o mundo base.

O estado atual será:

```text
Base Procedural World
        +
Persistent World Delta
        =
Current World State
```

---

## 4. Entidades

Toda entidade procedural deverá possuir uma identidade reprodutível.

Exemplos:

- players;
- NPCs;
- mobs;
- criaturas;
- itens;
- estruturas;
- elementos naturais;
- pontos de interesse.

O modelo conceitual central será o `EntityDNA`.

```text
EntityDNA
 ├── Identity
 ├── Archetype
 ├── Seed
 ├── GeneratorVersion
 ├── Appearance
 ├── Morphology
 ├── Equipment
 ├── BehaviorProfile
 └── RuntimeModifiers
```

O servidor poderá armazenar dezenas de bytes para representar visualmente uma entidade que, no cliente, resulta em uma composição complexa.

---

## 5. Avatares

O jogador poderá criar seu próprio avatar utilizando parâmetros controlados pelo sistema.

A criação poderá envolver:

- espécie;
- sexo ou tipo corporal;
- altura;
- composição corporal;
- rosto;
- cabelo;
- barba;
- pele;
- marcas;
- tatuagens;
- acessórios;
- roupas;
- armaduras;
- equipamentos.

O sistema deve evitar dependência de modelos 3D únicos por personagem.

O avatar será uma composição de:

```text
Base Mesh
 + Morph Targets
 + Head
 + Hair
 + Facial Features
 + Equipment
 + Materials
 + Accessories
 = Runtime Character
```

---

## 6. NPCs e Mobs

NPCs e mobs também serão gerados através de composição.

Duas entidades da mesma classe poderão ser visualmente e comportamentalmente diferentes.

Exemplo:

```text
Archetype: Guard
Seed: 981281
Intelligence: 6
Courage: 82
Aggression: 43
Social: 55
EquipmentTier: 3
```

Outro guarda pode compartilhar o mesmo archetype e possuir:

```text
Seed: 1128
Intelligence: 4
Courage: 38
Aggression: 76
Social: 22
EquipmentTier: 2
```

O resultado deve ser perceptivelmente diferente em aparência e comportamento.

---

## 7. Inteligência dos NPCs

A inteligência será tratada como um conjunto de capacidades cognitivas.

O nível geral de inteligência pode desbloquear capacidades, mas cada atributo poderá possuir seu próprio valor.

Dimensões iniciais:

- percepção;
- memória;
- raciocínio;
- linguagem;
- inteligência social;
- planejamento;
- curiosidade;
- agressividade;
- coragem;
- empatia;
- lealdade;
- paciência;
- ganância;
- sociabilidade.

Exemplo de níveis:

| Nível | Capacidade predominante |
|---|---|
| 0 | Instinto |
| 1 | Perigo e necessidades básicas |
| 2 | Reconhecimento de aliados e inimigos |
| 3 | Interação social básica |
| 4 | Memória de eventos |
| 5 | Conversação contextual |
| 6 | Planejamento de curto prazo |
| 7 | Inferência e negociação |
| 8 | Planejamento complexo |
| 9 | Estratégia social |
| 10 | Alta adaptação contextual |

---

## 8. Interação com Jogadores

NPCs não devem obrigatoriamente interagir com jogadores.

A interação depende de:

- inteligência;
- percepção;
- relacionamento;
- profissão;
- facção;
- estado emocional;
- objetivo atual;
- reputação do jogador;
- conhecimento;
- perigo;
- contexto social.

Um NPC pode:

- ignorar;
- observar;
- fugir;
- cumprimentar;
- conversar;
- negociar;
- comercializar;
- ameaçar;
- atacar;
- pedir ajuda;
- espalhar rumores.

---

## 9. Experiência Desejada

O mundo deve transmitir:

- variedade;
- consistência;
- imprevisibilidade controlada;
- sensação de sociedade;
- identidade regional;
- memória de acontecimentos;
- NPCs com personalidade;
- continuidade persistente;
- baixo custo de armazenamento;
- boa escalabilidade.

O objetivo não é simular tudo o tempo todo.

O objetivo é criar a ilusão coerente de um mundo vivo.

---

## 10. Direção Técnica

O projeto deverá evoluir inicialmente como uma base de tecnologia de jogo, contendo:

- entity runtime;
- procedural runtime;
- asset registry;
- character composer;
- world generation;
- NPC cognition;
- simulation LOD;
- networking;
- persistence;
- server authority;
- tooling;
- debug visualization.

O gameplay final será construído sobre essa fundação.

---

## 11. Definição de Sucesso Inicial

A primeira versão tecnicamente bem-sucedida deve demonstrar:

1. uma entidade criada apenas por `EntityDNA`;
2. montagem visual determinística;
3. NPCs com perfis cognitivos diferentes;
4. decisões diferentes diante do mesmo evento;
5. reconstrução idêntica por seed;
6. sincronização client/server baseada em estado compacto;
7. versionamento do gerador;
8. arquitetura desacoplada e testável;
9. suporte à evolução progressiva dos sistemas.

---

## 12. Filosofia do Projeto

> O jogo deve armazenar regras e identidade, não conteúdo redundante.

> A variedade deve surgir de composição, não de duplicação.

> A inteligência deve surgir de sistemas genéricos parametrizados, não de milhares de scripts específicos.

> O servidor deve controlar a verdade do mundo.

> O cliente deve materializar essa verdade de forma eficiente.
