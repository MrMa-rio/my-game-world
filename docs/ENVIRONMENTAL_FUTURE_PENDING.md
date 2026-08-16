# Pendências Futuras do Sistema Ambiental

## Objetivo

Este documento registra funcionalidades deliberadamente deixadas fora do `Environmental Interaction Framework V1`.

Elas não constituem defeitos da entrega atual. São extensões futuras que devem reutilizar:

- `EnvironmentalManager`;
- `WindSystem` e `WindSample`;
- `EnvironmentalPhysicalResponseSystem`;
- `EnvironmentalSurfaceResolver`;
- `EnvironmentalVfxSystem`;
- chunks, LOD e pooling existentes;
- perfis ambientais configuráveis.

Não criar managers paralelos por asset ou biome ao implementar estas pendências.

## Clima e fenômenos

- [ ] Chuva global e localizada.
- [ ] Inclinação da chuva pelo mesmo campo de vento.
- [ ] Neve em precipitação.
- [ ] Tempestades e transições graduais de intensidade.
- [ ] Calor ambiental e ondas de calor estilizadas.
- [ ] Umidade do ambiente e das superfícies.
- [ ] Fogo ambiental.
- [ ] Cinzas transportadas pelo vento.
- [ ] Tempestades de areia regionais.

## Tempo e fenômenos celestes

- [x] Ciclo contínuo madrugada/dia/noite.
- [x] Sol, lua, estrelas procedurais, estrelas cadentes e meteoros visuais.
- [ ] Sincronização autoritativa do relógio pelo servidor.
- [ ] Latitude, estações e duração variável do dia.
- [ ] Calendário, fases lunares e eclipses.
- [ ] Clima e nuvens ocultando corpos celestes.
- [ ] Reflexos de sol, lua e estrelas nos líquidos.
- [ ] Impactos de meteoros, crateras e `WorldDelta` autoritativo.

## Interações entre fenômenos

- [ ] Chuva reduzindo poeira de solo seco.
- [ ] Chuva transformando `DrySoil` em `Mud`.
- [ ] Vento inclinando chuva e neve.
- [ ] Vento influenciando fogo e transporte de cinzas.
- [ ] Neve aumentando o peso sobre árvores e estruturas.
- [ ] Calor reduzindo umidade e aumentando material solto.
- [ ] Fogo alterando superfície para `Ash`.

## Física e resposta estrutural

- [ ] Bones ou animação procedural seletiva para assets próximos.
- [ ] Resposta distinta entre galhos grandes e pequenos além do shader.
- [ ] Acúmulo de deformação sob forças prolongadas.
- [ ] Impactos físicos externos.
- [ ] Peso acumulado de neve, líquidos ou detritos.
- [ ] Verificação de `deformationThreshold`.
- [ ] Verificação de `breakThreshold`.
- [ ] Quebra de galhos e estruturas.
- [ ] Ativação de `Rigidbody` e collider somente após quebra.
- [ ] Recuperação, pooling ou persistência de partes quebradas.

Resultados de quebra que afetem gameplay ou persistência deverão ser autoritativos no servidor. A resposta cosmética pode permanecer no cliente.

## Superfícies e terreno

- [ ] Perfis autorados para `Sand`, `DrySoil`, `Grass`, `Snow`, `Rock`, `Mud`, `Water`, `Ash`, `Concrete` e `Wood`.
- [ ] Mapas explícitos de superfície por chunk.
- [ ] Transições entre superfícies.
- [ ] Acumulação de neve.
- [ ] Solo molhado e secagem gradual.
- [ ] Erosão visual.
- [ ] Erosão persistente através de `WorldDelta`.
- [ ] Ondas, espuma e resposta do corpo líquido ao vento.
- [ ] Resposta específica de lava sem alterar o contrato comum de líquidos.

## VFX e renderização

- [ ] VFX Graph ou partículas GPU para densidades maiores.
- [ ] Poeira distante resolvida por shader.
- [ ] Pollen, folhas, neve e cinzas com meshes estilizadas próprias.
- [ ] Variação de cor e forma por biome.
- [ ] Opacidade ambiental localizada.
- [ ] Oclusão e redução de emissão em interiores.
- [ ] Interação das partículas com água e obstáculos.
- [ ] Áudio ambiental sincronizado com força e rajadas.
- [ ] Iluminação contextual para tempestade, fogo e calor.

## Streaming, escala e performance

- [ ] Integração com streaming real de zonas.
- [ ] Ativação e liberação de células ambientais durante load/unload.
- [ ] Budget separado por região e fenômeno.
- [ ] Priorização por importância ambiental.
- [ ] Job System/Burst somente após profiling demonstrar necessidade.
- [ ] Métricas de CPU, GPU, partículas visíveis e overdraw.
- [ ] Testes de estresse com milhares de assets reativos.
- [ ] Testes com múltiplos biomes simultâneos.

## Autoria e ferramentas

- [ ] Assets `BiomeEnvironmentalResponseProfile` autorados para todos os biomes.
- [ ] `SurfaceEnvironmentalProfile` autorável quando regras de superfície excederem o catálogo atual.
- [ ] Inspector para `PhysicalResponseProfile` por família visual.
- [ ] Visualização de superfície por chunk.
- [ ] Visualização de células VFX ativas/inativas.
- [ ] Visualização de LOD ambiental.
- [ ] Editor de curvas de emissão.
- [ ] Presets de vento e clima.
- [ ] Captura comparativa de cenários ambientais.

## Rede e persistência

- [ ] Contrato compacto de estado climático autoritativo.
- [ ] Sincronização de eventos ambientais relevantes.
- [ ] Seed e versão para fenômenos persistentes.
- [ ] Persistência de alterações ambientais no `WorldDelta`.
- [ ] Interest management para eventos de clima regionais.
- [ ] Separação explícita entre estado climático autoritativo e representação cosmética cliente.

## Critérios antes de iniciar as pendências

Para cada nova funcionalidade:

1. definir se é estado autoritativo ou representação cliente;
2. definir seed e versão quando houver resultado procedural persistente;
3. reutilizar contexto, chunks, LOD e pooling existentes;
4. evitar `Update()` individual por asset;
5. evitar física permanente em elementos passivos;
6. medir CPU, GPU, memória, partículas e draw calls;
7. criar testes de interação com vento, biome e superfície;
8. documentar limites e migração de versão.

## Próximo fluxo recomendado

Escolher apenas uma vertical slice ambiental. A sugestão é:

```text
Rain
  + Wind direction
  + Grass / DrySoil / Water surface filtering
  + redução contextual de Dust
  + LOD e pooling existentes
```

Não iniciar simultaneamente chuva, fogo, neve acumulativa e destruição estrutural.
