# Direção Artística do Terreno e das Pedras

## Identidade compartilhada

Terreno e pedras usam a mesma linguagem da árvore procedural V5: low-poly legível, planos largos, assimetria controlada, paleta limpa, silhueta antes de detalhe e iluminação facetada. `TerrainSurface` e `Rock` continuam sendo as identidades existentes; esta etapa altera somente sua representação visual.

## Referências

Nenhuma mesh ou textura externa foi incorporada. As referências foram usadas para estudar forma, proporção, famílias e budgets:

- [Quaternius Simple Nature Pack](https://quaternius.com/packs/simplenature.html), CC0: formas reduzidas e paleta compartilhada;
- [Kenney 3D Nature Pack](https://opengameart.org/content/3d-nature-pack), CC0: modularidade, materiais simples e leitura isométrica;
- [OpenGameArt Low Poly Rocks](https://opengameart.org/content/low-poly-rocks), CC0: aproximadamente 300 triângulos por pedra;
- [Poly Haven Boulder 01](https://polyhaven.com/a/boulder_01), CC0: erosão, base de contato e planos geológicos observados antes da estilização;
- [Stylized Rocks FREE Sample](https://assetstore.unity.com/packages/3d/environments/stylized-rocks-free-sample-283215): LOD0 publicado entre 208 e 328 triângulos;
- [Stylized Rocks Package](https://assetstore.unity.com/packages/3d/environments/landscapes/stylized-rocks-package-topologic-team-305157): famílias por tamanho, três variações cromáticas e média publicada de 1.170 triângulos;
- [LMHPOLY Low Poly Rocks Pack](https://www.lmhpoly.com/game-assets/low-poly-rocks-pack): variedade de boulders compatível com URP e Unity 6.

## Pedras V6

As quatro receitas são: boulder, monólito, cluster e laje estratificada. Todas nascem de icosferas irregulares escaladas e assentadas no terreno. High LOD utiliza subdivisão limitada; LODs inferiores preservam largura, altura e base, removendo primeiro volumes secundários.

Três materiais compartilhados — rocha base, plano claro e plano escuro — são distribuídos deterministicamente pelas faces. Não há material por instância.

## Terreno V6

O height field, chunks, topologia e fingerprint persistente permanecem inalterados. A camada visual do cliente transforma as cores do biome em uma cor uniforme por triângulo, aumenta discretamente a saturação, quantiza luminosidade e aplica variação determinística pequena por face.

Isso evita gradientes lavados dentro dos polígonos e faz a baixa resolução geométrica participar explicitamente da direção artística.

## Budgets iniciais

- pedra High LOD: 80–300 triângulos conforme a família;
- pedra Medium/Low: receita reduzida diretamente;
- terreno: permanece em 80.000 triângulos e 100 chunks;
- materiais de pedra: três compartilhados;
- materiais de terreno: um compartilhado.
