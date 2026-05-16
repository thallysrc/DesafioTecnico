# Documentação StockEasy

Esta pasta reúne a documentação técnica do StockEasy em três markdowns navegáveis no GitHub e três PDFs branded em `dist/` (gerados via Pandoc + WeasyPrint + mermaid-filter).

## Conteúdo

| Documento | Descrição |
|-----------|-----------|
| [`01-product-decisions.md`](01-product-decisions.md) | Visão do produto, decisões estratégicas (stack, agentic API, soft delete, idempotência), trade-offs conscientes e roadmap v2 (chat com LLM). |
| [`02-architecture.md`](02-architecture.md) | Cinco diagramas Mermaid (System Context, Backend Layered, Sequence de Saída, ER, Frontend Feature Flow), tabela de stack e instruções de execução. |
| [`03-business-rules.md`](03-business-rules.md) | Entidades, enums, regras enforced pelo backend e o catálogo completo de `errorCodes` (9 slugs com HTTP status, categoria, exception class, hint pattern e condição que dispara). |

## PDFs prontos

Os três PDFs branded ficam em [`dist/`](dist/):

- `dist/01-product-decisions.pdf`
- `dist/02-architecture.pdf`
- `dist/03-business-rules.pdf`

**O avaliador NÃO precisa instalar nada** — basta clonar o repositório e abrir os PDFs em `docs/dist/` no leitor de PDF preferido. A regeneração local é opcional e está descrita na próxima seção.

Os PDFs carregam a identidade visual do StockEasy: paleta `brand-50..900` (primary `#1863DC`), tipografia Inter, capa cheia com wordmark, header com título do documento, footer com paginação `n / N`. Diagramas Mermaid renderizam como SVG inline via `mermaid-filter`.

## Regenerar PDFs (opcional)

Pré-requisitos no host:

```bash
# Ubuntu / Debian
sudo apt install pandoc python3-weasyprint
npm install -g mermaid-filter

# macOS
brew install pandoc weasyprint
npm install -g mermaid-filter
```

`mermaid-filter` baixa o Chromium do Puppeteer automaticamente na primeira execução para renderizar diagramas Mermaid → SVG.

Para regerar os três PDFs:

```bash
cd docs && ./generate-pdfs.sh
```

> O script `generate-pdfs.sh` é entregue pelo Plano 04-05 (PDF pipeline). Se você está lendo esta documentação a partir de um clone que ainda não contém o script, basta abrir os PDFs já commitados em `dist/` — a regeneração é estritamente opcional.

## Fontes

- Markdown sources são PT-BR (identifiers de código em EN — `Product`, `StockMovement`, `errorCode`, `Idempotency-Key`).
- Assets de estilo ficam em [`assets/`](assets/) (`pdf-style.css` + `pandoc-template.html`).
- A spec original do desafio está preservada em [`../README.challenge-spec.md`](../README.challenge-spec.md).
