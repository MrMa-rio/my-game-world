# ADR 0006: Materialização procedural centralizada no cliente

**Status:** Aceita
**Data:** 2026-08-15

## Contexto

A sandbox materializava cada categoria diretamente em `LowPolyDecorationRuntime`, usando uma biblioteca fixa de primitivas. Isso atendia à primeira visualização, mas distribuía decisões de geometria, não fornecia budget por frame, LOD procedural, cache observável ou lifetime reutilizável.

## Decisão

Manter `WorldElementDNA`, `DecorationPlacement`, categorias, parâmetros e terreno no domínio existente. Criar `ProceduralRuntimeManager` no adapter Unity como fachada de representação. Requests carregam a definição existente, contexto amostrado do terreno, LOD desejado e prioridade.

O manager delega geometria a providers registrados, usa uma biblioteca matemática compartilhada, limita trabalho por tempo/objetos/vértices, quantiza seeds em variantes cacheáveis, reutiliza instâncias e controla LOD. Materiais, meshes e propriedades de shader são compartilhados. A física usa colliders simples independentes.

## Consequências

Categorias permanecem no domínio e o núcleo do manager não precisa mudar para receber um novo provider. Variação de transform e shader não explode o cache geométrico. Geração continua na main thread nesta fase, mas passa a ter limites mensuráveis e um ponto claro para evolução futura. Meshes são estado cliente descartável; DNA e seed continuam sendo a fonte reconstruível.
