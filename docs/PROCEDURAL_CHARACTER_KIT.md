# Procedural Character Kit

## Fonte recebida

`Assets/ia assets/base-kit-person.webp` e uma prancha 2D com marca d'agua da Dreamstime. Ela nao e um modelo 3D, nao contem corpo completo nem vestimentas e nao deve ser recortada ou distribuida pelo jogo sem comprovacao de licenca. O arquivo foi tratado somente como referencia conceitual de modularidade.

`Assets/ia assets/avatar` contem o pacote **Animated Characters 1 (1.0)** de Kenney, criado por Kay Lousberg, acompanhado de `License.txt` CC0. Ele inclui `characterMedium.fbx`, quatro skins e fontes FBX de idle, run e jump. Este pacote e o fixture tecnico inicial para validar importacao, rig, render e animacoes.

O fixture nao representa o avatar final. O corpo e monolitico e suas roupas/rostos estao principalmente nas skins; por isso ele valida somente o slot `Body` e o caminho de materializacao. Ele nao valida ainda troca real de cabeca, cabelo, membros ou roupas como meshes separados.

## Modelo adotado

O sistema reutiliza `AssetId`, `AssetDescriptor`, `AssetCategory`, `AssetTrait`, `AssetCompatibility`, `AssetCatalogVersion` e `DeterministicRandom` existentes. Uma parte finita ocupa um `CharacterPartSlot`; o gerador seleciona somente partes compativeis e produz `CharacterAppearanceDNA` reproduzivel.

Slots iniciais: corpo, cabeca, olhos, sobrancelhas, nariz, boca, orelhas, cabelo, roupa superior, roupa inferior, maos, pes e acessorio.

```text
seed + generatorVersion + catalogVersion + actorTraits + part catalog
  -> CharacterAppearanceDNA
  -> AssetIds por slot + indices de paleta
  -> materializacao visual no cliente
```

Meshes, texturas ou prefabs nao fazem parte do DNA nem do estado autoritativo. Eles sao resolvidos no cliente pelo catalogo versionado. Uma troca de roupa altera slots, nao recria a identidade do Actor.

## Escala combinatoria

O numero bruto e o produto das opcoes validas por slot multiplicado pelas paletas e variacoes parametricas. Por exemplo, 20 opcoes em 13 slots ja representam `20^13`, antes de proporcoes e cores. Isso permite trilhoes de combinacoes sem armazenar trilhoes de assets, mas o numero real deve ser calculado a partir do catalogo licenciado e das regras de compatibilidade.

## Pipeline de arte necessario

Cada parte 3D futura deve usar o mesmo esqueleto humanoide, bind pose, escala, orientacao, sockets e limites de penetracao. Roupas devem ser meshes skinned separados ou variantes combinaveis; materiais devem ser compartilhados e receber paleta por propriedades, evitando um material por avatar.

## Referencias 3D para validacao

Pesquisa realizada em 16 de agosto de 2026. Estes modelos servem para testar proporcao, rig, separacao e importacao; nao definem a identidade visual final.

1. **Kenney Character Assets / Kay Lousberg** — melhor referencia modular: quatro corpos low-poly, mais de 75 skins, 40 acessorios e 17 animacoes. E um pacote pago com demo; antes de importar, confirmar formatos e termos incluidos no download: https://www.kaylousberg.com/work/kenney-character-assets
2. **The Company Characters (Styloo)** — melhor candidato gratuito para validar um humanoide completo rigado. O pacote declara CC0, fornece GLB/FBX, personagens de 8k–10k triangulos e rigs de face/cabelo. Nao e modular e exige revisao de pesos, portanto deve ser usado como referencia tecnica, nao como catalogo final: https://styloo.itch.io/company
3. **Mr. RedHat** — GLB CC0 muito leve (402 kB) e estilizado, adequado para validar importacao e escala, mas nao possui rig nem modularidade: https://yoogameart.itch.io/mr-redhat
4. **3D Low Poly Head** — cabeca em GLB/FBX/OBJ/Blend declarada CC0; util para testar somente o slot Head. A pagina tambem pede que o arquivo nao seja revendido, entao a licenca deve ser arquivada e revisada antes da distribuicao: https://zakariya-el-onsri.itch.io/3d-low-poly-head

O projeto ja possui `com.unity.cloud.gltfast` 6.19.0, portanto GLB/glTF e o formato preferido para testes. Ainda assim, um GLB monolitico nao vira automaticamente um kit modular: a separacao correta deve ser feita no arquivo-fonte 3D, preservando armature, bind poses, weights e materiais.

### Escolha recomendada

Usar temporariamente **The Company Characters** para validar rig/Animator e **Mr. RedHat** para validar o caminho GLB leve. Em paralelo, criar nosso proprio base mesh e shape language. O catalogo final deve conter somente partes autorais ou explicitamente licenciadas e adaptadas ao nosso esqueleto canonico.

## Proxima entrega recomendada

`AvatarCreationManager` e a fachada client-side inicial. Ele recebe o catalogo existente, gera/cacheia `CharacterAppearanceDNA`, limita requests por frame, resolve prefabs por `AssetId`, monta partes por slot, aplica indices de paleta com `MaterialPropertyBlock` e reutiliza raizes liberadas.

Produzir ou licenciar um kit 3D original com arquivos fonte separados e comprovacao de direitos. Depois, importar cada parte como prefab, atribuir AssetId estavel e registrar no Unity Asset Catalog. A imagem atual nao fornece geometria suficiente para essa etapa.

## Direcao visual contextual

O avatar agora recebe no momento da materializacao um `AvatarEnvironmentContext` compacto: bioma, superficie, altitude e inclinacao do ponto de origem. `AvatarEnvironmentalStyleResolver` combina esse contexto com a seed e produz uma receita visual deterministica, sem modificar identidade, locomocao ou collider.

Familias iniciais de silhueta: `TemperateTraveler`, `ForestRanger`, `DesertWayfarer`, `SnowHighlander` e `RockyHighlander`. Elas controlam proporcao visual moderada, paleta e angularidade. O contexto e fixado na origem do personagem; caminhar entre grama e pedra nao remodela o corpo.

O pacote `Assets/Goblin_Character` foi auditado como referencia local de linguagem low-poly: possui prefab, FBX humanoide, material URP e animacoes. Ele nao foi inserido como parte humana intercambiavel porque possui esqueleto e topologia proprios. Mistura direta quebraria bind poses e animacao. Sua contribuicao nesta fase e orientar planos mais marcados, silhueta compacta, massas legiveis e baixo custo geometrico.

As referencias CGTrader fornecidas sao modelos comerciais de alta densidade e licenca customizada. Elas orientam hierarquia facial, proporcao estilizada, leitura do cabelo e silhueta de vestuario, mas nenhuma geometria ou textura foi copiada para o projeto. A identidade final deve ser reconstruida em partes autorais low-poly sobre o esqueleto canonico.

## Integração jogável

O Player da `ProceduralWorldSandbox` usa o catálogo System G6 como fixture modular determinístico, com seed visual `3201`. O `PlayerRuntimeBootstrap` mantém física e gameplay no root do Actor e materializa `RuntimeAvatar` como filho visual. Isso permite trocar a receita sem substituir `CharacterController`, sensores, câmera ou capacidades.

`ProceduralAvatarAnimation` consome o `ActorAnimationState` já produzido pela propriocepção e aplica ciclos estilizados de idle, walk, run, jump e fall aos bones compatíveis de todas as partes. A locomotion continua in-place e autoritativa; não se usa root motion para deslocar o Actor.

Essa separação segue a hierarquia recomendada pelo Unity para retargeting: componentes do personagem no root e modelo/Animator em um filho. Quando o esqueleto autoral e clips finais estiverem prontos, o sink procedural pode ser substituído por `AnimatorAnimationSink` e um Animator Controller/Blend Tree sem mudar Player, Actor ou locomotion.

Referências oficiais consultadas:

- Humanoid retargeting e hierarquia recomendada: https://docs.unity3d.com/6000.0/Documentation/Manual/Retargeting.html
- Blend Trees para combinar walk/run por velocidade: https://docs.unity3d.com/6000.0/Documentation/Manual/class-BlendTree.html
- Root motion e separação do deslocamento visual: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Animator-applyRootMotion.html
