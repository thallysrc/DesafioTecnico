#!/usr/bin/env bash
# Regenerates the three branded StockEasy PDFs in docs/dist/.
#
# Toolchain (host install — see docs/README.md):
#   - pandoc            (apt: pandoc | brew: pandoc)
#   - WeasyPrint        (apt: python3-weasyprint | pip: weasyprint | brew: weasyprint)
#   - mermaid-filter    (npm: npm install -g mermaid-filter)
#
# Per CONTEXT.md D-17: regeneration is opt-in for the developer. The evaluator
# never runs this script — they just open the committed PDFs in docs/dist/.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# Sanity-check the toolchain before doing anything destructive.
for tool in pandoc weasyprint mermaid-filter; do
  if ! command -v "$tool" >/dev/null 2>&1; then
    echo "ERROR: '$tool' não encontrado no PATH." >&2
    echo "Instale conforme docs/README.md (pandoc + WeasyPrint + mermaid-filter)." >&2
    exit 1
  fi
done

mkdir -p dist

DOCS=(
  "01-product-decisions"
  "02-architecture"
  "03-business-rules"
)

for doc in "${DOCS[@]}"; do
  echo "→ Gerando dist/${doc}.pdf"
  pandoc "${doc}.md" \
    --filter mermaid-filter \
    --template assets/pandoc-template.html \
    --css assets/pdf-style.css \
    --pdf-engine=weasyprint \
    --metadata=lang:pt-BR \
    -o "dist/${doc}.pdf"
done

echo "✓ PDFs regenerados em dist/:"
ls -la dist/*.pdf
