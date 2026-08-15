# ADR 0004: Identidade estável e catálogo de assets

**Status:** Aceita
**Data:** 2026-08-15

## Contexto

Caminhos, nomes de arquivo, GUIDs da engine e posições em listas não são contratos seguros para DNA persistido ou mensagens de rede. Renomear, mover ou reordenar conteúdo poderia mudar silenciosamente a entidade reconstruída.

## Decisão

Representar assets por `AssetId`, um inteiro positivo e estável cujo valor zero é reservado. IDs publicados nunca serão reutilizados para outro conteúdo. `AssetCatalog` associa IDs a pesos dentro de uma `AssetCatalogVersion`, rejeita entradas inválidas ou duplicadas e trata a ordem como parte do contrato determinístico.

A resolução para recursos concretos ocorre por `IAssetRegistry<TAsset>`. Implementações que conhecem Unity permanecem em assemblies de adapter do cliente. Seleção ponderada usa o RNG determinístico compartilhado e qualquer mudança de IDs, ordem ou pesos capaz de alterar resultados exige uma nova versão do catálogo.

Compatibilidade é descrita por categorias e traits persistentes, sem referências à engine. O adapter inicial `MyGameWorld.Client.AssetResolution` resolve bindings autorados em `UnityAssetCatalog`, mas não participa da seleção nem das regras autoritativas.

## Consequências

DNA e rede não dependem da organização física dos assets. Catálogos publicados precisam ser preservados enquanto forem referenciados, e será necessário tooling para reservar IDs, validar versões e detectar reutilização ou ausência de conteúdo.
