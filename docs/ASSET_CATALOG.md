# Asset Catalog

## Objetivo

O catálogo separa identidade procedural de arquivos físicos. DNA, rede e persistência transportam `AssetId`; somente o cliente resolve esse ID para um objeto Unity.

## Regras de identidade

- zero é reservado e inválido;
- um ID publicado nunca muda de significado;
- mover ou renomear um arquivo não altera seu ID;
- remover conteúdo não libera seu ID para reutilização;
- IDs, ordem e pesos publicados permanecem preservados na mesma `AssetCatalogVersion`;
- qualquer mudança capaz de alterar uma seleção cria uma nova versão.

Um registro externo de IDs reservados ainda será necessário antes da produção de conteúdo em escala. Até essa ferramenta existir, novos IDs devem ser revisados junto do catálogo e nunca escolhidos a partir de índices de arrays ou GUIDs Unity.

## Camada compartilhada

`AssetCatalog` contém entradas determinísticas. Cada `AssetCatalogEntry` combina:

- `AssetDescriptor`;
- peso positivo de seleção.

O descritor define `AssetCategory`, traits próprias e `AssetCompatibility`. A compatibilidade exige todas as traits configuradas e rejeita qualquer trait excluída. A avaliação ocorre nos dois sentidos.

Os valores numéricos de `AssetCategory` e os bits de `AssetTrait` fazem parte do schema persistente. Novos valores podem ser acrescentados, mas valores existentes não podem ser reordenados ou reutilizados.

## Adapter Unity

No Editor, crie um catálogo em `Create > My Game World > Asset Catalog`. Configure a versão e associe cada ID lógico a um `UnityEngine.Object`.

`UnityAssetRegistry` valida o catálogo ao ser construído e cria um mapa somente de runtime. Ele rejeita:

- bindings nulos;
- IDs zero;
- IDs duplicados;
- referências Unity ausentes;
- versão zero.

O adapter não decide compatibilidade e não seleciona assets. Essas decisões permanecem na camada compartilhada; o registry apenas materializa o ID já escolhido.

## Próximas ferramentas

- inspector que mostre IDs repetidos antes do Play Mode;
- arquivo ou banco de reserva de IDs;
- validação entre `AssetCatalog` compartilhado e `UnityAssetCatalog`;
- relatório de IDs sem recurso local;
- migração explícita entre versões de catálogo.
