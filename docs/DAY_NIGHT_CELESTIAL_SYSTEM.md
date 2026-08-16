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
