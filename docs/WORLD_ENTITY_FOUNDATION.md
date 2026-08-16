# World Entity Foundation

`MyGameWorld.Client.EntityRuntime` materializa qualquer identidade capaz de existir no runtime Unity sem assumir Player, NPC, controller ou gameplay.

```text
EntityId
  -> WorldEntity
       |- WorldEntityLifecycle
       |- WorldPresence
       `- IWorldEntityRegistry
```

`WorldPresence` mantém `GlobalPosition` em `double` separada de `Transform.position`. Um `WorldCoordinateFrame` converte entre coordenadas globais e locais, permitindo aplicar rebases sem alterar a posição lógica. `WorldSpatialContext` recebe célula, região, bioma e identificador de superfície já resolvidos por sistemas de mundo; ele não gera nem carrega conteúdo.

O lifecycle explícito é `Uninitialized -> Created -> Spawned -> Active/Disabled -> Despawning -> Destroyed`. `Awake`, `Start` e `OnEnable` não comandam spawn. `OnDestroy` existe apenas como proteção final para remover registros e encerrar uma entidade Unity destruída externamente.

`WorldEntityRegistry` é uma implementação local e injetável. Não é singleton e rejeita identidades duplicadas. Um composition root futuro será responsável por seu ownership e por conectar eventos de floating origin ao `WorldCoordinateFrame` das presenças registradas.
