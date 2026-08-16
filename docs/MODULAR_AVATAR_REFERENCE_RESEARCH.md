# Pesquisa de Referencias para Avatar Modular

Pesquisa realizada em 16 de agosto de 2026. O objetivo e validar tecnicamente um personagem componentizado, nao adotar a identidade visual de terceiros nem substituir o `AvatarCreationManager` do projeto.

## Recomendacao

Usar duas referencias separadas:

1. **UMA 3** para estudar receitas, slots, remapeamento de bones, combinacao incremental, cache e reducao de draw calls.
2. **Modular RPG Characters (System G6/Qoma)** para inspecionar meshes componentizados CC0 em Blender e produzir um pequeno fixture Unity sob nosso esqueleto canonico.

Nao integrar UMA como dependencia runtime neste momento. Nosso dominio deterministico, `AssetId`, catalogo versionado, fila e cache ja existem. UMA deve servir como referencia de algoritmos e pipeline, evitando uma segunda arquitetura de avatar.

## Opcoes avaliadas

### UMA 3 — referencia de engenharia

- Fonte: https://github.com/umasteeringgroup/UMA
- Licenca do framework: MIT.
- Sistema gratuito de criacao e modificacao de avatares para Unity.
- Possui slots, recipes, DNA corporal, atlas, mesh combining, LOD e geracao incremental.
- Releases recentes incluem compatibilidade Unity 6, combinacao incremental sob budget e estatisticas para crowds.
- Risco: framework grande, identidade visual padrao fora do objetivo e sobreposicao com nossa arquitetura.
- Decisao: estudar e eventualmente adaptar algoritmos pequenos; nao instalar como nucleo do jogo.

### Modular RPG Characters — fixture CC0 recomendado

- Fonte: https://opengameart.org/content/modular-rpg-characters
- Licenca declarada: CC0.
- Dois personagens, masculino e feminino, aproximadamente 2k–3k triangulos.
- Componentes e equipamentos em arquivos Blender, texturas de 512–1024 px e cerca de 45 animacoes declaradas.
- Inclui rig base, sockets de armas e arquivos de rig IK separados.
- Riscos: Blender 2.79b, materiais precisam ser refeitos, relatos de armaduras sem weights corretos em exportacoes Unity e animacoes que exigem bake.
- Decisao: usar apenas depois de abrir/auditar no Blender, corrigir weights e exportar um FBX/GLB normalizado.

#### Normalizacao executada

O pacote CC0 foi preservado em `Assets/ia assets/avatar-reference/system-g6`, junto do arquivo original, fonte e auditorias. Foi aberto com Blender 2.79 portatil, sem instalacao no sistema.

- Masculino: 32 meshes, 43 bones, 9.872 vertices e 18.842 triangulos somando todas as variantes.
- Feminino: 26 meshes skinned exportadas, 43 bones, 9.100 vertices e 17.420 triangulos somando todas as variantes.
- Foram reparados ArmatureModifiers ausentes em cabelos, cabecas e equipamentos que ja possuiam os 41 vertex groups esperados.
- Foram exportados `human_male_modular.fbx` e `human_female_modular.fbx`, preservando meshes separadas e um armature compartilhado por fixture.

Esses totais representam todas as opcoes simultaneamente, nao um avatar visivel. Uma receita deve ativar apenas um item compativel por grupo, reduzindo o custo final para aproximadamente 2k–3k triangulos conforme declarado pelo autor.

### Stylized Modular Characters — bom conteudo, licenca insuficiente

- Fonte: https://tajsensei.itch.io/stylized-modular-characters
- 17 cabelos, 11 tops, 6 bottoms, 7 calcados, olhos, headwear e blendshapes.
- 11k–22k triangulos e shader de mascara para tres cores.
- A pagina nao informa claramente a licenca; ha inclusive pergunta publica sem resposta sobre ela.
- Decisao: nao baixar nem usar ate haver termos explicitos.

### PolyActors — opcao comercial Unity 6

- Fonte: https://assetstore.unity.com/packages/3d/characters/polyactors-modular-fantasy-people-370222
- 60 partes de corpo e 77 itens de roupa declarados.
- Compatibilidade URP e Unity 6000.3 declarada; versao 1.4, publicada em maio de 2026.
- Preco observado: US$ 49,99, Unity Asset Store EULA Single Entity.
- Decisao: candidato comercial forte para teste visual, mas exige compra pelo proprietario do repositorio.

## Calculo combinatorio

O limite bruto de aparencias e:

```text
combinacoes = produto(opcoes compativeis por slot)
            * tonsDePele
            * tonsDeCabelo
            * paletasDeRoupa
            * variacoesDeProporcao
```

Exemplo conservador:

```text
Body 4
Head 12
Eyes 8
Hair 24
Upper 20
Lower 16
Feet 12
Accessory 18, incluindo nenhum
Skin 16
Hair palette 24
Clothing palette 32

4 * 12 * 8 * 24 * 20 * 16 * 12 * 18 * 16 * 24 * 32
= 7.827.577.896.960 combinacoes
```

Compatibilidade reduz esse total, enquanto proporcoes parametrizadas o ampliam. O manager nunca enumera esse espaco; a seed seleciona uma unica receita em custo proporcional ao numero de slots e partes candidatas.

## Budget tecnico recomendado

Para o primeiro fixture autoral low-poly:

- LOD0: 8k–18k triangulos por avatar montado.
- LOD1: 4k–8k triangulos.
- LOD2: 1k–3k triangulos ou mesh combinada simplificada.
- 1 esqueleto canonico por avatar proximo.
- Ate 4 materiais compartilhados no LOD0; meta de 1–2 apos atlas/mesh combine.
- Variacao de cor via `MaterialPropertyBlock`, paleta ou atlas, nunca material exclusivo por avatar.
- Criacao sob fila: manter o limite inicial de 2 avatares por frame e adicionar budget de vertices/milissegundos apos medir fixtures reais.

## Pipeline de normalizacao

```text
Fonte CC0/licenciada
  -> auditoria de licenca
  -> Blender: separar meshes e corrigir weights
  -> esqueleto/bind pose canonicos
  -> nomes de slots e sockets
  -> FBX ou GLB normalizado
  -> Unity humanoid import validation
  -> prefab por parte
  -> AssetId estavel
  -> UnityAssetCatalog
  -> AvatarCreationManager
```

## Criterios para aceitar um fixture

- Licenca arquivada no mesmo diretorio.
- Corpo e roupas realmente separados, nao apenas skins.
- Todas as partes compartilham bones/bind poses compativeis.
- Sem gaps graves em ombros, cintura, pulsos e tornozelos.
- Idle, walk, run e jump deformam corpo e roupas sem separacao.
- Materiais funcionam em URP sem shader proprietario obrigatorio.
- Nao ha dependencia runtime do pacote original depois da normalizacao.
- O visual e tratado como referencia temporaria; silhueta e materiais finais serao autorais.

## Galeria Unity

`Assets/Scenes/AvatarFixtureGallery.unity` e a cena tecnica gerada. Ela usa dois catalogos temporarios versionados, 58 `AssetId`s estaveis derivados do caminho, `AvatarCreationManager` e uma UI IMGUI simples para trocar seed, regenerar e alternar a familia corporal. A cena nao e conteudo final e nao deve definir a direcao artistica.
