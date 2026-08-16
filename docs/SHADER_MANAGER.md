# Procedural Shader Manager

`ProceduralShaderManager` centraliza a representacao visual compartilhada do terreno, vegetacao, rochas, flora, liquidos e demais itens que usam `ProceduralWorldVertexColor.shader`. Objetos nao consultam luz ou horario individualmente.

## Pipeline

```text
WorldTime + Sun/Moon elevation + Quality Budget
                         |
                         v
              ProceduralShaderManager
                         |
       global lighting/reflection/shadow parameters
                         |
                         v
          Shared procedural world shader
```

O shader preserva a direcao fisica das luzes direcionais e o shadow map do URP. Sobre essa base aplica bandas de luz/sombra, reflexo especular quantizado, Fresnel cartoon e rim light ambiental. Intensidade e cor continuam interpoladas durante dia, entardecer, noite e madrugada.

## Camadas de custo

| Qualidade | Camadas |
|---|---|
| Low | luz base e duas bandas toon; sem sombra dinamica, rim ou reflexo |
| Medium | adiciona sombra dinamica e rim light |
| High | adiciona reflexo cartoon, quatro bandas de luz e tres de sombra |
| Ultra | cinco bandas, quatro bandas de sombra e reflexo/rim completos |

`F12` alterna o nivel em runtime. Materiais continuam compartilhados e instanciaveis. A camada selecionada muda parametros globais; nao cria materiais por objeto e nao reconstrui meshes.

## Resposta por superficie

- terreno: reflexo baixo e rugoso;
- troncos/galhos: resposta difusa;
- folhas/flora: brilho suave;
- rochas: reflexo moderado e facetado;
- agua: reflexo forte e superficie lisa;
- lava: brilho moderado estilizado.

Limitacoes atuais: nao ha reflection probes dinamicos, SSR, oclusao ambiente customizada, cascatas de sombra adaptativas nem decals. Esses recursos devem ser adicionados como novas camadas de budget, nao diretamente nos objetos.
## Sombras, reflexos e transicoes tonais

Arvores, pedras, decoracoes e terreno usam o mesmo contrato de renderizacao: projetam e recebem sombras URP. A oclusao vem do shadow map da luz direcional, enquanto as normais da propria geometria produzem o auto-sombreamento das faces. Ceu, estrelas, nebulosas, wireframe e VFX nao escrevem no shadow map.

A paleta do ciclo diario possui pontos intermediarios e interpolacao suave entre madrugada, amanhecer, dia, entardecer e noite. As bandas cartoon tambem usam bordas suavizadas, evitando saltos secos de tonalidade.

O brilho especular estilizado usa o vetor fisico de reflexao da luz principal (`reflect`) e so aparece na face iluminada. A cor refletida e a cor das sombras acompanham a paleta horaria. Isto preserva a identidade low-poly sem simular ray tracing ou reflexoes globais completas.

O shadow map do perfil PC cobre 300 m em quatro cascatas (4096 px), permitindo que elevacoes do terreno projetem sombra sobre outros chunks. O perfil Mobile cobre 100 m em duas cascatas (1024 px). Fora desses limites a sombra e removida por distancia de maneira intencional para limitar custo de GPU.
