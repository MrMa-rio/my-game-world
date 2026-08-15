# Contribuindo

## Preparação

Use a versão do Unity registrada em `ProjectSettings/ProjectVersion.txt` e execute `git lfs install` antes do primeiro checkout.

## Branches e commits

- `feature/<descricao>` para funcionalidades.
- `fix/<descricao>` para correções.
- `chore/<descricao>` para manutenção, conteúdo ou ferramentas.
- Prefira commits no imperativo e com uma única intenção.

## Assets Unity

- Mova e renomeie assets pelo Editor para preservar referências.
- Sempre versione o arquivo `.meta` correspondente.
- Evite mudanças não relacionadas em cenas, prefabs e configurações globais.
- Antes do commit, confirme que `Library`, `Temp`, `Logs`, `UserSettings` e builds não foram adicionados.
- Use Git LFS para formatos binários definidos no `.gitattributes`.

## Pull requests

Descreva objetivo, impacto, forma de validação e evidências visuais quando houver alterações perceptíveis. Mantenha o PR pequeno o bastante para revisão objetiva e marque limitações conhecidas.

## Validação mínima

- O projeto abre sem erros de compilação.
- As cenas alteradas carregam corretamente.
- Testes relevantes passam, quando existirem.
- Não há arquivos gerados ou segredos no diff.
- Assets binários aparecem em `git lfs ls-files`.

