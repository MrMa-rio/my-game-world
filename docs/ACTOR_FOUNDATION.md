# Actor Foundation

`MyGameWorld.Client.ActorRuntime` compoe um `Actor` sobre uma `WorldEntity`. O Actor coordena contexto, disponibilidade, registros de capacidades e sensores e uma fonte substituivel de decisao; ele nao implementa qualquer acao.

```text
WorldEntity
  `- Actor
      |- ActorContext
      |- ActorState
      |- CapabilityRegistry
      |- SensorHub
      `- IActorController
```

`ActorContext` expoe somente o Actor, sua entidade, presenca e transform ja resolvidos. Nao e um service locator. `ActorState` representa apenas disponibilidade operacional derivada do lifecycle da entidade; movimento, queda e interacao serao facetas separadas nas etapas correspondentes.

Os registros sao indexados por contrato e evitam `GetComponent<T>()` espalhado. Eles ainda aceitam objetos comuns porque os contratos reais de capability e sensor pertencem as etapas seguintes. `IActorController` define apenas binding e substituicao da fonte de decisao; intents e atualizacao de controllers ainda nao foram implementados.

## Capability System

Capabilities implementam `IActorCapability`; componentes Unity reutilizaveis podem derivar de `ActorCapability`. O Actor inicializa e registra cada capability por seu contrato funcional, permitindo consultar, habilitar, desabilitar ou remover uma implementacao sem conhecer sua categoria concreta.

`IsEnabled` representa configuracao propria da capability. `CanExecute` combina essa configuracao com a disponibilidade operacional do Actor. Assim, desabilitar temporariamente o Actor nao perde a configuracao de suas capacidades. A fundacao nao possui `Update` e nao cria custo por frame.

## Intent System

Controllers produzem valores imutaveis (`MoveIntent`, `LookIntent`, `RunIntent`, `JumpIntent` e `InteractIntent`) e os submetem ao `ActorIntentRouter`. O router conhece apenas o tipo da intencao e o handler registrado por uma capability.

```text
Controller -> Intent -> ActorIntentRouter -> Capability
```

O dispatch recusa Actors indisponiveis e capabilities desabilitadas antes de executar o handler. Cada tipo possui um unico handler para evitar execucao ambigua. Remover uma capability tambem remove suas rotas. Novas intencoes e handlers podem ser adicionados sem alterar o router. Nao ha fila, polling ou alocacao por dispatch; a pequena lista temporaria existe somente ao remover uma capability.

## Controller Abstraction

`IActorController` continua sendo o contrato substituivel de decisao. `ActorController` fornece binding seguro para componentes Unity, enquanto `HumanActorController` adapta o Input System existente para intents. Ele reutiliza `Assets/InputSystem_Actions.inputactions`, mapa `Player`, e cria uma copia runtime por controller para nao compartilhar estado de enable/bindings.

O Human Controller le `Move`, `Look`, `Sprint`, `Jump` e `Interact` e produz somente intents. `HumanInputSnapshot` separa a leitura do dispositivo da traducao em intencoes, permitindo teste deterministico e futuras fontes alternativas. Nenhum controller executa movimento ou consulta capabilities concretas.

## Locomotion Core

`ActorLocomotion` e uma capability reutilizavel que recebe direcao e velocidade desejadas sem conhecer teclado, Player ou IA. Sua simulacao e coordenada por um unico `ActorLocomotionScheduler`, evitando um `FixedUpdate` por Actor.

```text
Desired motion
  -> GroundProbe (uma SphereCast)
  -> SlopeResolver / StepResolver
  -> MovementMotor / GravityResolver
  -> CollisionResolver (CharacterController.Move)
  -> LocomotionState + WorldPresence
```

`LocomotionProfile` centraliza gravidade, velocidade terminal, limites de inclinacao, step, probe e aceleracao do motor. Slopes sao classificados em `Walkable`, `Difficult`, `Slide` e `Blocked`. O `CharacterController` permanece a fronteira cinetica e de colisao; input, caminhada, corrida e pulo pertencem a etapas posteriores. A posicao local resultante e sincronizada com `WorldPresence`, preservando a separacao entre coordenadas Unity e globais.

## Walk Capability

`WalkCapability` e o primeiro consumidor funcional de `MoveIntent`. Ela transforma o vetor 2D da decisao em direcao horizontal relativa ao Actor e solicita ao `IActorLocomotion` a velocidade definida por `WalkProfile`. A locomotion continua responsavel pela aceleracao, desaceleracao, slopes, gravidade e colisao.

```text
Human ou AI Controller -> MoveIntent -> WalkCapability -> IActorLocomotion
```

Direcao zero solicita parada gradual pelo motor. Desabilitar ou remover Walk limpa imediatamente a solicitacao de movimento. A capability nao conhece Input System, Player, camera ou tipo concreto de locomotion.

## Run Capability

`RunCapability` consome `RunIntent` e registra seu multiplicador configurado em `RunProfile` no pipeline de velocidade da caminhada. Ela nao troca diretamente uma constante no motor e nao conhece Shift ou qualquer dispositivo.

`MovementSpeedModifiers` combina multiplicadores por fonte. Run e apenas a primeira fonte; efeitos futuros podem adicionar ou remover seus proprios modificadores sem alterar a locomotion. Remover ou desabilitar Run retira somente o modificador que ela possui.

## Jump, Fall e Land

`JumpCapability` consome `JumpIntent`, valida cooldown e solicita um impulso ao contrato `IActorLocomotion`. A locomotion aceita o impulso somente quando ativa e grounded. `JumpProfile` contem velocidade vertical e cooldown.

`LocomotionState.VerticalState` expoe `Grounded`, `Rising`, `Falling` e `Landing`. O estado `Landing` dura uma simulacao na transicao do ar para o chao; isso fornece um evento observavel para animacao e apresentacao sem colocar essas responsabilidades no controller.

## Physical Body e Collision

`PhysicalBody` configura a representacao colisora do Actor a partir de `PhysicalBodyProfile` e publica contatos, sem implementar locomotion. `WorldCollisionBody` classifica colliders do ambiente em `None`, `Soft` ou `Solid`: None desativa colisao, Soft usa trigger e Solid bloqueia fisicamente.

As layers reservadas sao `Actor`, `Terrain`, `StaticWorld`, `DynamicWorld`, `SoftEnvironment`, `Trigger`, `Interaction`, `Projectile` e a layer Unity `Water`. A classificacao e pequena e sem categorias de gameplay; objetos procedurais existentes poderao receber o descritor durante sua materializacao sem reescrever suas definicoes.

## Sensor Foundation

Sensores implementam `IActorSensor` e componentes reutilizaveis derivam de `ActorSensor`. `SensorHub` registra cada sentido por contrato, enquanto `ActorSensorScheduler` processa sensores em lote.

Os modos sao `EventDriven`, `Interval` e `Physics`. Sensores event-driven nao entram no scheduler; sensores de visao e cheiro podem usar intervalos; propriocepcao pode usar o tick de fisica. O sensor coleta dados, mas nao decide acoes nem apresenta feedback.

## Proprioception

`ProprioceptionSensor` amostra no tick de fisica o estado da locomotion e o Transform do Actor. Seu snapshot imutavel inclui velocidade, aceleracao, orientacao, velocidade angular, grounded, falling, slope, direcao e estado de movimento (`Idle`, `Moving`, `Rising`, `Falling`, `Landing`).

O sensor publica `Sampled` para animacao, IA ou apresentacao, sem decidir comportamento. Aceleracao e velocidade angular sao derivadas entre amostras, sem queries adicionais de Physics.

## Touch Sensor

`TouchSensor` e completamente event-driven: ele transforma contatos publicados por `IPhysicalBody` em `TouchEvent`, contendo origem, ponto, normal, velocidade relativa, forca aproximada, nivel de interacao e identificador de superficie. Nao possui `Update`, raycast ou polling.

`IPhysicalSurfaceProvider` e a fronteira de integracao para terrain e objetos ambientais informarem seu identificador existente sem o ActorRuntime depender de enums concretos do mundo procedural. Temperatura, umidade e material podem ser acrescentados por provedores futuros.

## Vision Sensor

`VisionSensor` detecta `IVisionTarget` por alcance, FOV e linha de visada. Ele usa `OverlapSphereNonAlloc` com buffer persistente e uma unica amostragem intervalada centralizada; candidatos fora do FOV sao descartados antes do raycast de oclusao.

`VisionProfile` configura alcance, abertura, altura dos olhos, masks e capacidade do buffer. Vision nao referencia `Camera`, rendering, Player ou controller: coleta alvos observaveis e publica a percepcao para consumidores externos.

## Hearing Sensor

`PerceptionSoundStream` distribui eventos logicos de som com posicao, intensidade, categoria e fonte. Ele nao e um singleton e nao depende de `AudioSource`; a composition root fornece a mesma instancia aos sensores relevantes.

`HearingSensor` e event-driven e calcula alcance/percepcao usando `HearingProfile`. Somente eventos dentro do alcance efetivo sao publicados como `HeardSound`, sem `Update`, overlap ou busca global.

## Smell Sensor

`ScentField` mantem fontes logicas reconstruiveis por chave; `SmellSensor` as amostra em baixa frequencia usando alcance e sensibilidade de `SmellProfile`. `IScentTransport` permite que o WindSystem altere propagacao direcional futuramente sem acoplar o sensor ao ambiente concreto. Nao existe simulacao fluida ou Physics query.

## Taste Sensor

`TasteSensor` recebe `TasteStimulus` somente quando outro sistema executa uma acao de provar, beber ou consumir. O estimulo carrega identificador de sabor, intensidade, toxicidade, nutricao e fonte. O sensor nao possui tick e nao implementa alimentacao, inventario ou efeitos de gameplay.

## Player Assembly

`MyGameWorld.Client.PlayerRuntime.PlayerActorAssembly` e a composition root do Player. Ela recebe perfis e servicos explicitamente, inicializa `WorldEntity`/`Actor` e compoe Human Controller, Locomotion, Walk, Run, Jump, PhysicalBody e os seis sensores. O assembly de Player contem somente montagem; todas as implementacoes funcionais permanecem reutilizaveis no ActorRuntime.

## Player Camera Foundation

`PlayerCameraSystem` e especifico da apresentacao do Player e referencia o Actor sem ser parte dele. `PlayerCameraRig` encapsula Camera, root e configuracao. `PlayerCameraModeController` registra estrategias por `PlayerCameraModeId` e troca modos pelo contrato `IPlayerCameraMode`, sem cadeia crescente de condicionais.

## First Person Camera

`FirstPersonCameraMode` posiciona o rig na altura dos olhos, aplica yaw ao Transform do Actor e mantem pitch local limitado pelo perfil. A estrategia recebe apenas deltas acumulados e nao consulta mouse ou Input System.

`PlayerCameraLookBridge` e uma capability especifica do Player que encaminha `LookIntent` ao modo ativo. Assim, HumanController continua produzindo intents, enquanto ActorRuntime e locomotion permanecem independentes de Camera.

## Third Person Camera

`ThirdPersonCameraMode` orbita um pivot acima do Actor com distancia, altura, limites verticais, smoothing posicional e suavizacao rotacional configurados em `ThirdPersonCameraProfile`. A estrategia preserva yaw proprio para free-look e nao modifica a implementacao de First Person.

## Camera Switching

`ChangeCameraIntent` e emitida opcionalmente pelo HumanController pela action `ChangeCamera` (`V` ou clique do analogico direito). `PlayerCameraSwitchCapability` alterna entre estrategias registradas; Camera, rig e Actor permanecem os mesmos durante a troca.

## Camera Collision

`CameraCollisionResolver` usa `SphereCast` entre pivot e posicao desejada, aplica padding e respeita distancia minima. Obstaculos aproximam a camera imediatamente; ao liberar a linha, a distancia retorna de forma gradual. O resolver e injetado na estrategia Third Person e nao conhece paredes, terrain ou categorias concretas.

## Player HUD Foundation

`PlayerHudPresenter` recebe eventos de propriocepcao e troca de camera e publica `PlayerHudState` para `IPlayerHudView`. Nao usa `FindObjectOfType`, polling ou referencias globais. Prompt e visibilidade do crosshair/debug sao atualizados por metodos explicitos.

`PlayerHudView` fornece uma representacao inicial leve com crosshair, interaction prompt, movement debug opcional e modo de camera. A view pode ser substituida por UI Toolkit sem alterar sensores, Actor ou presenter.

## Player Sensory Presentation

`PlayerSensoryPresentation` conecta os seis contratos sensoriais do Actor a `IPlayerSensoryFeedback`. Vision, hearing, touch, smell, taste e proprioception permanecem coletores genericos; rendering, audio, camera feedback e UI pertencem ao adaptador de apresentacao fornecido pelo Player.

## World Observer

`WorldObserverRegistry` recebe observadores por contrato com posicao global, Transform, prioridade e finalidades de streaming/rendering/physics/environment. `PlayerWorldObserverSystem` registra a presenca do Actor e nunca chama Load/Unload de chunks. Cinematic Camera, veiculos e atores remotos podem registrar outras implementacoes sem alterar o Player.

## Environment Integration

`IWorldEnvironmentContextProvider` fornece biome, surface, vento e weather por posicao local/global. `EnvironmentContextSensor` amostra esse contrato em intervalo configuravel. O `EnvironmentalManager` existente implementa o provider usando seu WindSystem e EnvironmentalSurfaceResolver, sem expor tipos concretos ao ActorRuntime.

## Surface Interaction

`IPhysicalSurfaceProvider` e `PhysicalSurfaceDescriptor` ficam no EntityRuntime e fornecem somente um identificador, sem duplicar o enum ambiental. GroundProbe inclui `SurfaceId` no estado da locomotion/propriocepcao; TouchEvent usa o mesmo contrato. Chunks de terrain recebem um descritor-base, enquanto o EnvironmentContext preserva a classificacao posicional mais precisa para transicoes internas do terreno.

## Movement Modifier Pipeline

`MovementSpeedModifiers` combina modificadores por fonte como `(base + additive) * multipliers`, removendo cada efeito independentemente. `MovementModifier` inclui label somente para debug; o pipeline nao conhece Run, Mud, Snow, injury, buff ou encumbrance.

Run usa o pipeline como uma fonte. `SurfaceMovementModifier` demonstra integracao data-driven: escuta EnvironmentContext e aplica a regra configurada por SurfaceId sem adicionar condicionais ao locomotion core.

## Animation Integration

`ActorAnimationDriver` transforma snapshots de propriocepcao em `ActorAnimationState`: Idle, Walk, Run, Jump, Fall e Land. Ele e event-driven e nao executa gameplay no Animator.

`IActorAnimationSink` desacopla a derivacao de estado da tecnologia visual. `AnimatorAnimationSink` e o adaptador Unity inicial e escreve somente hashes configurados por `ActorAnimationDriverProfile`. Player, NPC e outros Actors podem reutilizar o mesmo driver com controllers de animacao diferentes.

## Actor Debug Tooling

`ActorDebugView` exibe EntityId, posicao global/cell, biome/surface, controller, contagens de capabilities/sensors, grounded, velocity, slope, movement state e camera mode. Gizmos mostram ground probe, FOV, hearing/smell ranges e camera probe. Referencias sao injetadas uma vez; nao ha buscas globais.

## Player Test Scene

`Assets/Scenes/PlayerTestScene.unity` e gerada por `PlayerTestSceneBuilder` e contem flat terrain, slopes, escadas, rocha, parede, vegetacao, tronco, plataformas e quedas. `PlayerTestSceneBootstrap` monta em runtime o Player real, cameras, HUD, observer, sensory presentation e debug usando os mesmos componentes do framework.

## Architecture Validation

O ActorRuntime nao referencia PlayerRuntime, Camera ou HUD. GetComponent ocorre apenas durante inicializacao/callback de colisao, nunca nos loops de locomotion/sensores. Os unicos loops genericos sao os schedulers centralizados; HumanActorController possui um Update porque representa a unica fonte humana local. Testes de boundary verificam essas dependencias por reflection.

## Mock AI Validation

`MockAIController` e uma fonte de decisao minima agendada por `ActorDecisionScheduler`. Ele produz somente MoveIntent e RunIntent com variacao deterministica de fase. O mesmo ActorLocomotion, WalkCapability e RunCapability usados pelo Player executam as decisoes; nenhum desses sistemas possui branch para NPC/AI.

## Final Review

A revisao consolidada da fundacao, seus limites de dependencia, custos runtime e pendencias esta em `docs/ACTOR_FRAMEWORK_FINAL_REVIEW.md`.
