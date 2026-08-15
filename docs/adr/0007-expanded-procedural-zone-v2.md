# ADR 0007: Zona procedural expandida e generator V2

**Status:** Aceita
**Data:** 2026-08-15

## Contexto

A zona de 100 × 100 m validou determinismo e linguagem visual, mas não exercitava escala espacial, quantidade de chunks, distribuição por habitats ou grandes massas de relevo.

## Decisão

Ampliar a sandbox para 1.000 × 1.000 m usando 10 × 10 chunks e budget de 80.000 triângulos. A resolução final de 201 × 201 reduz a densidade geométrica por metro em relação à sandbox pequena, preservando o estilo low-poly e limitando memória.

Criar `TerrainGeneratorV2` e `HeightFieldGeneratorV2`. V2 adiciona relevo regional, aumenta de forma determinística a quantidade e escala de features singulares e distribui vegetação usando um campo de habitat. V1 permanece disponível para compatibilidade.

Cada decoração recebe um `AssetId` estável. O runtime tenta resolver recurso finito no registry e usa geometria procedural como fallback, sem alterar a definição lógica.

## Consequências

A zona exercita 100 chunks, aproximadamente 40 mil vértices lógicos, 240 mil vértices renderizados flat e centenas de elementos ambientais. A geração ainda é local e completa; streaming por distância continua sendo a próxima evolução antes de aumentar novamente a escala.
