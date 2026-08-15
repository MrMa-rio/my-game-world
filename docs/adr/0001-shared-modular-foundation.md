# ADR 0001: Fundação compartilhada modular

**Status:** Aceita  
**Data:** 2026-08-15

## Contexto

O cliente atual é Unity, mas o domínio precisará executar também em servidores autoritativos e ferramentas. Referências à engine impediriam reuso e testes isolados.

## Decisão

Organizar a fundação em assemblies independentes sob `Assets/Game/Shared`: `Core`, `EntityModel`, `Procedural` e `NpcCognition`. Dependências apontam para módulos mais fundamentais e nenhum deles referencia `UnityEngine`.

Especializações de DNA usam composição. Integrações de Unity, transporte e persistência serão adapters externos.

## Consequências

O domínio pode ser extraído para outro projeto C# no futuro. Em contrapartida, serialização Unity e recursos visuais exigirão mapeamento explícito, evitando conveniência acoplada no núcleo.

