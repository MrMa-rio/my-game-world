# ADR 0003: Limite da cognição de NPC

**Status:** Aceita  
**Data:** 2026-08-15

## Contexto

NPCs precisam variar e escalar sem scripts exclusivos, sem estado visual e sem entregar autoridade a um LLM ou ao cliente.

## Decisão

Separar dados (`IntelligenceDNA`, `PersonalityDNA`), capacidades derivadas (`IIntelligenceCapabilityResolver`) e escolha (`INpcDecisionPolicy`). `NpcBrain` apenas compõe esses contratos e recebe tick e `SimulationLod` explícitos.

Capabilities V1 usam bitset para consultas baratas e IDs numéricos estáveis. A política retorna um token neutro; sistemas autoritativos futuros atribuirão semântica e validarão efeitos.

## Consequências

É possível trocar utility AI, planners ou simulação estatística sem mudar DNA. O modelo não implementa memória, percepção, diálogo ou gameplay nesta fase. Regras de capability incompatíveis exigem outro resolver versionado.

