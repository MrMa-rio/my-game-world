# Procedural Liquids

O domínio fundamental é a substância, não o nome geográfico. `Water`, `Lava` e futuros líquidos são valores de `LiquidSubstance`. Cada ocorrência concreta é um `LiquidBodyDNA` singular com `WorldElementId`, seed, versão, bounds, volume, nível superficial, proporções e fluxo próprios.

```text
Liquid substance: Water / Lava / ...
  + volume
  + radiusX / radiusZ
  + surface level
  + flow rate
  -> quantity tier
  -> contextual form
```

`LiquidQuantityTier` classifica a quantidade como `Trace`, `Small`, `Medium`, `Large` ou `Vast`. `LiquidBodyForm` é uma leitura derivada: `Puddle`, `Pond`, `Lake`, `Sea`, `Stream` ou `River`. Assim, água não vira outra categoria ao formar um lago, e lava usa exatamente o mesmo contrato. Formas alongadas ou com fluxo tornam-se córrego/rio; corpos contidos usam volume e proporção para poça/lago etc.

A sandbox V4 materializa deterministicamente um corpo de água sobre a maior depressão planejada. A geometria visual é uma malha radial leve e cache futuro poderá separar receita geométrica de animações GPU. A versão inicial não implementa física de fluidos, natação, correnteza, erosão, reflexão, refração nem propagação de lava.

Limites de classificação são uma política versionada e deverão futuramente vir de perfil de mundo/bioma. Nunca se deve persistir somente o rótulo “lago”: persistem-se substância, quantidade, forma espacial, fluxo, estado e seed; o rótulo pode ser recalculado pela mesma versão.
