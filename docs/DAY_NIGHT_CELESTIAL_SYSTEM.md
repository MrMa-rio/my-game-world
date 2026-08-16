# Day, Night and Celestial System V1

## Relógio

`WorldTimeSystem` é a única fonte do tempo ambiental cliente. O ciclo padrão dura 300 segundos reais e inicia às 05:30. `WorldTimeSnapshot` publica hora, dia, fase e pesos contínuos de luz diurna, noite, amanhecer e entardecer.

```text
DeepNight -> Dawn -> Day -> Dusk -> Night -> DeepNight
```

O relógio não modifica a seed, o DNA ou o fingerprint da zona. No futuro, a hora autoritativa poderá ser recebida do servidor e aplicada através de `SetHour` sem alterar os consumidores.

## Consumidores

```text
WorldTimeSnapshot
  ├── CelestialCycleSystem
  │    ├── Sun directional light
  │    ├── Moon directional light
  │    ├── ambient trilight
  │    ├── fog
  │    └── celestial sky shader
  ├── ProceduralWorldVertexColor shader
  └── CelestialEventSystem
```

Sol e lua percorrem arcos opostos. Intensidade, cor e sombras variam continuamente; não existem mudanças abruptas por `if hour == X`. A luz `Sun` já presente na cena é reutilizada. A lua é criada uma vez no runtime ambiental.

## Céu e materiais

`ProceduralWorldSky.shader` desenha gradiente dia/noite, horizonte de amanhecer/entardecer, disco solar, lua crescente estilizada e estrelas procedurais com twinkle. Não depende de cubemap ou textura externa.

O shader compartilhado dos objetos recebe `_WorldTimeTint`. Terreno, árvores, rochas, flora, líquidos e demais materiais procedurais preservam suas cores, mas respondem à mesma exposição e tonalidade ambiental.

### Paletas por fase

As fases usam paletas graduais, sem troca abrupta: madrugada azul-violeta com horizonte coral, dia azul limpo, entardecer laranja-avermelhado evoluindo para azul e noite azul-marinho. `_WorldTimeRimColor` adiciona um contorno luminoso dependente do angulo de visao, mais presente a noite e discreto durante o dia. O contorno reutiliza o shader compartilhado e nao cria materiais extras por objeto.

## Estrelas cadentes e meteoros

As estrelas fixas não são mais desenhadas por um hash angular no skybox. `ProceduralStarFieldSystem` cria estrelas singulares determinísticas e conglomerados irregulares em uma esfera 3D, eliminando padrões circulares no horizonte e no zênite. A representação permanece batched em um único mesh/material.

Cada estrela é um `CelestialItemKind.Star` singular com `ItemId`, seed derivada, direção, magnitude, tamanho, cor e indicador de pertencimento a conglomerado. `CelestialOrbitModel` separa o dia solar de 24 h, o dia sideral de 23,9344696 h, o ano tropical de 365,2422 dias e a órbita lunar sideral de 27,321662 dias. Assim, estrelas avançam aproximadamente quatro minutos por dia solar, o Sol acompanha o ciclo solar e a Lua desloca-se contra o fundo estelar em vez de permanecer artificialmente oposta ao Sol. A observabilidade usa magnitude: no início do entardecer aparecem apenas estrelas de destaque, depois estrelas comuns e conglomerados; na madrugada a ordem se inverte; durante o dia o renderer é desativado completamente. O mesmo `StarVisibility` limita o agendamento de estrelas cadentes e meteoros.

O centro de cada estrela permanece posicionado na abóbada 3D, mas seu pequeno disco é expandido em espaço de tela. Assim, o tamanho aparente permanece estável entre resoluções, FOVs e ângulos próximos ao horizonte, sem transformar cada estrela em um `GameObject` ou aumentar draw calls.

No URP, o campo usa a fila `Transparent-100`: ele é composto depois do skybox, que de outra forma o sobrescreveria, e respeita o depth buffer do terreno. A abóbada também escala para 92% do `farClipPlane`, mantendo estrelas atrás da geometria observável.

A densidade estelar usa luminosidade local normalizada pelo sol de meio-dia: sol máximo mais ambiente diurno representam `1.0`. Lua, ambiente e luzes direcionais/locais próximas entram no mesmo cálculo. Em luminosidade `<= 0.25`, o catálogo libera até 30 camadas; em `>= 0.75`, mantém a camada padrão 1x; entre os limites, interpola continuamente. A câmera de desenvolvimento representa o observador até existir um player.

Nebulosas estilizadas são calculadas no sky shader com ruído tridimensional em múltiplas escalas. A amostragem ocorre no mesmo referencial sideral das estrelas, evitando projeção circular e mantendo a rotação física do céu profundo. Sua visibilidade combina `StarVisibility` com luminosidade local: céu lunar escuro revela filamentos azul, ciano e violeta; sol, amanhecer e luz artificial intensa os esmaecem até desaparecer.

`CelestialEventSystem` possui pool fixo de quatro `TrailRenderer`s. Durante a noite, um scheduler determinístico agenda eventos visuais:

- estrela cadente: rápida, fina e azul-branca;
- meteoro: mais raro, espesso, lento e alaranjado.

Não ocorre `Instantiate/Destroy` durante o ciclo normal. Meteoros são apenas fenômenos celestes nesta versão: não colidem, causam dano, criam crateras ou alteram `WorldDelta`.

## Debug

- `F8`: avança três horas;
- `F9`: pausa/continua o relógio;
- `F10`: força uma estrela cadente;
- `F11`: força um meteoro;
- HUD: hora, fase e eventos ativos.

## Extensões futuras

- sincronização autoritativa da hora;
- latitude, estação e duração variável do dia;
- fases lunares e calendário;
- eclipses;
- constelações por região;
- múltiplas luas;
- eventos de meteoro com impacto autoritativo;
- iluminação urbana/noturna;
- reflexão celeste em água;
- clima afetando visibilidade do céu.
