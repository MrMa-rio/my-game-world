# World Visual Asset IDs

## Bloco ambiental inicial

| AssetId | Significado estável | Resolução atual |
|---:|---|---|
| `10001` | árvore temperada estilizada | receita procedural / prefab opcional |
| `10002` | rocha temperada estilizada | receita procedural / prefab opcional |
| `10003` | arbusto temperado estilizado | receita procedural / prefab opcional |
| `10004` | flor temperada estilizada | receita procedural / prefab opcional |
| `10005` | aglomerado de flores temperadas | receita procedural / prefab opcional |
| `10006` | cogumelo temperado estilizado | receita procedural / prefab opcional |
| `10007` | conjunto de cogumelos temperados | receita procedural / prefab opcional |
| `10008` | conjunto de árvores temperadas | receita procedural / prefab opcional |
| `10009` | formação de rochas temperadas | receita procedural / prefab opcional |
| `10010` | conjunto de arbustos temperados | receita procedural / prefab opcional |
| `11001` | superfície procedural de água | receita procedural / material compartilhado |
| `11002` | superfície procedural de lava | receita procedural / material compartilhado |
| `10900` | marcador de escala de desenvolvimento | receita procedural |

Esses números são identidades lógicas, não índices de enum, posições em array ou GUIDs Unity. Não devem ser reutilizados para outro significado.

`DecorationPlacement` transporta o `AssetId` escolhido pelo domínio. O `ProceduralRuntimeManager` consulta primeiro um `IAssetRegistry<UnityEngine.Object>` compatível quando fornecido. Um `Mesh` ou prefab com `MeshFilter` válido é reutilizado sem assumir ownership. Se o ID não estiver presente no registry, o provider matemático cria a representação cacheável correspondente.

O fallback garante que a sandbox continue executável sem pacote artístico externo. Adicionar posteriormente um prefab ao catálogo não muda identidade, placement, seed, scheduler, pooling ou regras de LOD.
