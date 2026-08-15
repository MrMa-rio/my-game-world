# Versionamento e colaboração

## O que entra no Git

São fontes do projeto: `Assets`, `Packages`, `ProjectSettings`, documentação e configuração do repositório. Arquivos `.meta` são parte essencial da identidade dos assets e devem acompanhar seus respectivos arquivos ou diretórios.

## O que não entra

`Library`, `Temp`, `Logs`, `UserSettings`, builds e configurações locais são derivados ou específicos da máquina. O `.gitignore` os exclui.

## Git LFS

O `.gitattributes` envia formatos binários de arte, modelos, texturas, áudio, vídeo, fontes e pacotes para o Git LFS. Para validar:

```bash
git lfs install
git lfs ls-files
git lfs fsck
```

Arquivos serializados em YAML pelo Unity (`.unity`, `.prefab`, `.asset`, `.meta` e relacionados) continuam no Git comum para preservar diffs e merges úteis.

## CI

O workflow inicial valida a forma do repositório, artefatos indevidos, integridade de ponteiros LFS e pares de metadata sem exigir uma licença Unity. Testes no Editor ou builds automatizados devem ser adicionados depois, junto de credenciais Unity armazenadas como secrets do GitHub e de uma plataforma-alvo definida.
