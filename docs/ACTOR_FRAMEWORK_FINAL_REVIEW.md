# Actor Framework V1 — Final Review

## Resultado

A fundacao separa identidade, decisao, acao, percepcao e apresentacao. Player e Mock AI dirigem o mesmo Actor por intents; camera, HUD e feedback sensorial permanecem no modulo de Player.

```text
EntityRuntime
  WorldEntity + WorldPresence + WorldObserver
        |
ActorRuntime
  Actor + Controller -> Intent -> Capability
  Locomotion + PhysicalBody + Sensors + AnimationDriver
        |
PlayerRuntime
  Human input + Camera + HUD + SensoryPresentation + CompositionRoot
```

Dependencias seguem somente para baixo. `EntityRuntime` nao conhece Actor ou Player; `ActorRuntime` nao conhece Player, Camera ou HUD. Extensoes sao adicionadas por capabilities, sensores, controllers, perfis e adaptadores, sem classificacao por tipo concreto de Actor.

## Integracao com o mundo

- `WorldPresence` preserva posicao global separada da posicao local Unity e esta preparado para floating origin.
- `WorldObserverRegistry` publica relevancia para streaming, render, fisica e ambiente sem carregar chunks diretamente.
- `IWorldEnvironmentContextProvider` adapta o ambiente procedural existente para biome, superficie, vento e weather.
- `IPhysicalSurfaceProvider` compartilha classificacao de contato entre terrain, GroundProbe e TouchSensor.
- `MovementSpeedModifiers` combina Run e efeitos de superficie sem condicionais no locomotion core.

## Revisao de performance

- Locomotion e propriocepcao usam schedulers centralizados de fisica.
- Sensores intervalados usam `ActorSensorScheduler`; touch, hearing e taste sao orientados a eventos.
- Vision usa consultas non-alloc e intervalo configuravel.
- `GetComponent` aparece somente em inicializacao ou callbacks de colisao, nao em loops de movimento/sensores.
- Camera executa um `SphereCast` somente para o Player observado; GroundProbe executa uma consulta por tick fisico do Actor ativo.
- Nao existem buscas globais (`FindObjectOfType`, `Camera.main`) nos modulos da fundacao.
- Labels de modificadores sao estaticos; mudancas de superficie nao formatam strings no caminho runtime.

## Validacao Open/Closed

`HumanActorController` e `MockAIController` produzem os mesmos `MoveIntent` e `RunIntent`. Ambos utilizam sem alteracao:

- `ActorIntentRouter`;
- `WalkCapability`;
- `RunCapability`;
- `MovementSpeedModifiers`;
- `ActorLocomotion`;
- `PhysicalBody`.

Nao existem branches `isPlayer`/`if Player` nos sistemas genericos. Um novo controller pode ser composto sobre o Actor sem modificar locomotion, capabilities ou sensores.

## Limites e pendencias conhecidas

- Perfis ainda sao criados em runtime na cena tecnica; a producao deve usar assets ScriptableObject versionados e um prefab composition root.
- `WorldObserverRegistry` esta pronto, mas o streaming/distant rendering ainda precisa consumi-lo diretamente.
- O weather exposto pelo adaptador ambiental permanece neutro ate existir um sistema climatico autoritativo.
- O descritor do collider de cada chunk informa sua superficie-base; transicoes finas continuam vindo do resolver ambiental por posicao.
- `AnimatorAnimationSink` e a integracao tecnica; controllers, clips, blend trees e avatar finais ainda nao existem.
- Vision faz broadphase por Actor. Para grandes populacoes, o proximo patamar deve compartilhar uma particao espacial entre sensores.
- A montagem possui rollback de controller, mas a composition root completa ainda nao possui transacao unica para falhas intermediarias de prefab/configuracao.
- Floating origin esta previsto no modelo de coordenadas, mas seu deslocamento runtime ainda nao foi implementado.

## Proximo passo recomendado

Criar os assets de perfil e o prefab de producao do primeiro Actor humano, integrar esse prefab a uma zona procedural real e validar streaming/floating-origin antes de adicionar gameplay. Essa etapa deve preservar a mesma composition root e nao mover Camera, HUD ou input para `ActorRuntime`.
