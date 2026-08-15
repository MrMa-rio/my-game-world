# ADR 0002: Determinismo e versionamento procedural

**Status:** Aceita  
**Data:** 2026-08-15

## Contexto

RNG global, relógio, ordem instável de coleções e mudanças silenciosas de algoritmo impedem a reconstrução idêntica de entidades.

## Decisão

Toda geração recebe `GenerationContext` com seed, versão do gerador e versão do catálogo. O RNG inicial é SplitMix64 e seeds filhas usam uma função estável de derivação. Geradores declaram sua versão e recusam contextos incompatíveis.

Alterações capazes de mudar uma saída criam uma versão nova; implementações antigas devem permanecer disponíveis enquanto houver DNA persistido que as referencie.

## Consequências

Replays e reconstrução tornam-se verificáveis. A ordem de consumo do RNG e os catálogos passam a fazer parte do contrato compatível e exigem testes golden antes de evolução.

