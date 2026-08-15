# My Game World

Projeto de jogo 3D em estágio inicial, desenvolvido com Unity 6 e Universal Render Pipeline (URP).

## Estado atual

O projeto possui uma fundação de domínio compartilhada e independente de `UnityEngine`, além da primeira materialização visual: a **Procedural World Sandbox**. Ela gera uma zona 3D determinística de 1.000 × 1.000 m com terrain low-poly, biome temperado, caminhos, vegetação, rochas, câmera livre e ferramentas de inspeção.

Não há personagem, NPC visual, rede ou gameplay próprio implementado. Abra `Assets/Scenes/ProceduralWorldSandbox.unity` e execute a cena para explorar. Consulte [Procedural World Sandbox](docs/PROCEDURAL_WORLD_SANDBOX.md), [Procedural Runtime Manager](docs/PROCEDURAL_RUNTIME_MANAGER.md), [Foundation Architecture](docs/FOUNDATION_ARCHITECTURE.md) e [Roadmap](docs/ROADMAP.md).

## Requisitos

- Unity `6000.4.0f1` (a mesma versão registrada em `ProjectSettings/ProjectVersion.txt`)
- Git 2.x
- Git LFS 3.x

## Começando

```bash
git lfs install
git clone <URL_DO_REPOSITORIO>
```

Abra a pasta clonada pelo Unity Hub usando a versão indicada acima. Na primeira abertura, o Unity recriará `Library`, `Temp`, `Logs` e outros artefatos locais ignorados pelo Git.

## Estrutura

```text
Assets/             Conteúdo, cenas, scripts e configurações do jogo
Assets/Game/Shared/ Domínio compartilhado sem dependência da engine
Assets/Game/Client/ Adapters e representação específicos do cliente Unity
Assets/Tests/       Testes Edit Mode e futuros testes Play Mode
Packages/           Manifesto e lockfile de pacotes Unity
ProjectSettings/    Configurações versionadas do projeto
docs/               Decisões e documentação técnica/produtiva
.github/            CI, templates e governança do repositório
```

## Fluxo de desenvolvimento

1. Crie uma branch curta a partir de `main` (`feature/...`, `fix/...`, `chore/...`).
2. Faça commits pequenos e descritivos.
3. Não versione diretórios gerados pelo Unity.
4. Confirme que novos binários estão cobertos pelo Git LFS.
5. Abra um pull request e aguarde a validação automática.

Mais detalhes estão em [CONTRIBUTING.md](CONTRIBUTING.md).

## Git LFS

Arquivos binários de arte, modelos, áudio, vídeo, fontes e pacotes são rastreados por LFS através do `.gitattributes`. Arquivos YAML do Unity permanecem como texto para permitir revisão e merge.

## Licença

Nenhuma licença de uso ou redistribuição foi concedida neste momento. Consulte [LICENSE.md](LICENSE.md).
