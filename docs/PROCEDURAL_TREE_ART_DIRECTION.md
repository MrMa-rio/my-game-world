# Direção Artística da Árvore Procedural

## Objetivo

A árvore continua sendo `DecorationKind.Tree` e usa o `AssetId` existente `10001`. Os arquétipos descritos aqui são receitas de materialização visual; não são novas categorias de domínio.

## Referências e licenças

As referências servem para estudar silhueta, hierarquia de massas e orçamento. Nenhuma geometria ou textura foi copiada ou incorporada.

- Quaternius Ultimate Nature Pack: modelos CC0, útil como referência de copas facetadas e famílias coerentes.
- Kenney Nature Kit: conteúdo CC0 com grande variedade modular e linguagem visual consistente.
- Low Poly Trees Pack Lite: gratuito sob a Standard Unity Asset Store EULA, compatível com URP.
- Stylized Nature Bundle: pacote comercial sob licença Single Entity, com LOD, colisores e variações de folhas.
- LMHPOLY Low Poly Trees Pack: pacote comercial com múltiplas famílias e paletas sazonais.
- Stylized Snow Forest: pacote comercial com budgets publicados para LOD0 e LOD1.

Assets sujeitos à Unity Asset Store EULA ou a licença comercial somente podem ser importados depois de adquiridos pelo proprietário do projeto. A implementação atual deriva princípios artísticos gerais e gera geometria matemática própria.

## Source art do projeto

`Assets/ia assets/Meshy_AI_Geometric_Tree_0815191238_texture.glb` é a referência artística principal. O GLB 2.0 contém uma mesh com 273.056 vértices, 523.268 triângulos, um material e três imagens JPEG incorporadas. Ele não é instanciado no mapa: seu custo excede em várias ordens o budget de uma árvore procedural.

A análise da distribuição espacial do modelo identificou:

- tronco legível no quarto inferior;
- expansão rápida da copa acima da bifurcação;
- maior largura na faixa central-superior;
- fechamento progressivo no topo;
- massa orgânica contínua, sem depender de uma única primitiva.

## Linguagem de forma V5

O tronco é uma cadeia de prismas orientados e afunilados. Ramos estruturais aparecem no LOD alto e médio. A copa usa icosferas low-poly irregulares, com subdivisão limitada e perturbação determinística. Vários lobos fortemente interpenetrados recriam a distribuição de massa observada na referência sem copiar sua topologia.

As quatro receitas compartilham a mesma família broadleaf derivada da referência e variam largura, altura e inclinação: equilibrada, esguia, ampla e windswept.

O LOD remove primeiro ramos menores e volumes secundários. Tronco, altura, largura e silhueta principal são preservados.

## Determinismo e reutilização

`ProceduralMeshKey` combina `AssetId`, categoria, variante, LOD e versão de estilo. Há no máximo quatro receitas por LOD no fallback procedural. Escala, rotação e cor por instância criam diversidade adicional sem produzir uma mesh para cada árvore.

O estilo foi elevado para V5 para invalidar corretamente as copas anteriores e suas variantes de LOD no cache. O shader compartilhado usa iluminação half-Lambert, piso de sombra e contraste por orientação de face para preservar leitura cartoon sem achatar os volumes.

A galeria de comparação materializa suas árvores através do mesmo `ProceduralRuntimeManager`, materiais, propriedade de cor e High LOD usados pela câmera panorâmica do mapa. Ela não possui mais um caminho de preview visual paralelo. A qualidade da galeria é o contrato visual que o mapa deve preservar, não uma representação a ser degradada para imitar LOD distante.

## Proveniência

O arquivo foi fornecido pelo proprietário do projeto como asset criado externamente. Antes de distribuição comercial, registrar ferramenta/autoria, termos de uso e direito de redistribuição em um manifesto de proveniência. A ausência dessa informação não deve ser confundida com uma licença open source.

## Próxima evolução artística

Antes de adicionar novas espécies ao domínio, o próximo estudo deve calibrar uma única broadleaf em uma cena de apresentação próxima, incluindo paleta, proporções, resposta de luz e deformação de vento em GPU.
