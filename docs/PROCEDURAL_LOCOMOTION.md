# Procedural Locomotion

## Objetivo

A locomocao visual procedural transforma o estado proprioceptivo do Actor em uma pose humanoide suave. Ela nao move o Actor, nao le input e nao possui autoridade de gameplay. O deslocamento permanece no `ActorLocomotion`; a animacao e uma representacao client-side substituivel por Mecanim no futuro.

## Modelo biomecanico inicial

`ProceduralGaitMath` separa apoio e balanco, resposta de carga, flexao do joelho para liberar o pe, impulso do tornozelo, contrabalanco dos bracos e movimentos secundarios de pelve e tronco. A frequencia e amplitude acompanham suavemente a velocidade observada pela propriocepcao.

Na caminhada, os cotovelos permanecem quase estendidos. Na corrida, a flexao aumenta progressivamente para encurtar o pendulo do braco. As transicoes idle/walk/run e as poses aereas usam amortecimento exponencial para evitar mudancas abruptas.

## Referencias biomecanicas

- Cadencia, velocidade e comprimento da passada em adultos saudaveis: https://pmc.ncbi.nlm.nih.gov/articles/PMC2674051/
- Controle e funcao de contrabalanco dos bracos: https://pubmed.ncbi.nlm.nih.gov/19181900/
- Bracos estendidos ao caminhar e cotovelos flexionados ao correr: https://pubmed.ncbi.nlm.nih.gov/31289110/
- Relacao entre velocidade, cadencia e comprimento da passada na corrida: https://pmc.ncbi.nlm.nih.gov/articles/PMC8523042/

## Limitacoes

Este modelo e uma aproximacao procedural estilizada, nao uma simulacao musculoesqueletica. Contato preciso do pe, IK em terreno irregular, adaptacao individual por proporcoes do avatar e clips autorais com motion capture continuam como evolucoes recomendadas.

## Human Basic Motions FREE

A representacao principal do Player usa os clips Humanoid in-place do pacote `Human Basic Motions FREE 2.4.2`, de Kevin Iglesias, importado sob a Standard Unity Asset Store EULA. O projeto gera seu proprio `HumanBasicMotions.controller`; controllers, cenas, modelos e scripts de demonstracao do fornecedor nao participam do runtime do jogo.

O Blend Tree combina `HumanM@Idle01`, `HumanM@Walk01_Forward` e `HumanM@Run01_Forward` pelo parametro `Speed`. `HumanM@Jump01 - Begin`, `HumanM@Fall01` e `HumanM@Jump01 - Land` representam salto, queda e pouso. O `ActorAnimationDriver` continua sendo a origem dos estados e o `ActorLocomotion` continua responsavel pelo deslocamento e colisoes. Root motion permanece desativado.

As partes modulares System G6 sao importadas como Humanoid. Durante a composicao, `ModularHumanoidRigAssembler` escolhe a armature do corpo como esqueleto canonico e remapeia os arrays `SkinnedMeshRenderer.bones` das demais partes por nomes de bones compativeis. Animators duplicados sao desativados e apenas um Animator dirige corpo, cabeca, pernas, pes, maos, roupas e armaduras. Isso impede divergencia de root motion/retargeting entre pecas e reduz o custo de avaliacao da animacao.

Caso um avatar futuro nao possua um Avatar Humanoid valido, `ProceduralAvatarAnimation` permanece como fallback funcional. Acessorios com menos de 85% de compatibilidade de bones nao sao remapeados automaticamente, evitando deformacao parcial; eles deverao usar sockets ou uma receita especifica.

Fonte: https://assetstore.unity.com/packages/3d/animations/human-basic-motions-free-154271
