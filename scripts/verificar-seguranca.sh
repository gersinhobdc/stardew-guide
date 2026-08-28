#!/usr/bin/env bash
#
# Verifica as invariantes que garantem o multiplayer crossplay (ver MULTIPLAYER.md).
#
# A Camada 3 da garantia diz que o Pelican e somente leitura, somente desenho,
# sem conteudo novo e sem rede. Isso e uma PROMESSA ate existir algo que a
# verifique. Este script e esse algo.
#
# Rode antes de todo commit:  ./scripts/verificar-seguranca.sh

set -uo pipefail

SRC="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)/Pelican"
FALHAS=0

# Procura um padrao proibido no codigo, ignorando comentarios.
proibir() {
  local padrao="$1"
  local motivo="$2"

  local achados
  achados=$(grep -rnE "$padrao" "$SRC" --include='*.cs' 2>/dev/null \
    | grep -vE '^\s*[^:]+:[0-9]+:\s*(//|\*|/\*)' || true)

  if [ -n "$achados" ]; then
    echo "FALHOU: $motivo"
    echo "$achados" | sed 's/^/    /'
    echo
    FALHAS=$((FALHAS + 1))
  else
    echo "ok: $motivo"
  fi
}

echo "=== Invariantes de multiplayer do Pelican ==="
echo

proibir 'SendMessage|IMultiplayer' \
  "ZERO REDE: nenhuma mensagem de mod e enviada"

proibir 'AssetRequested|IAssetEditor|IAssetLoader|e\.Edit\(|e\.LoadFrom' \
  "ZERO CONTEUDO: nenhum asset do jogo e editado ou carregado"

proibir 'HarmonyLib|new Harmony|HarmonyPatch' \
  "ZERO PATCH: nenhum metodo do jogo e substituido"

proibir '\.addItemToInventory|\.removeItem|\.addItem\(|Game1\.player\.reduceActiveItemByOne' \
  "SOMENTE LEITURA: nenhum item e adicionado ou removido"

proibir '\bsaveGame|SaveGame\.|\.write\(|File\.WriteAllText.*[Ss]ave' \
  "SOMENTE LEITURA: o save nunca e escrito"

echo
if [ "$FALHAS" -eq 0 ]; then
  echo "TUDO OK — as 4 invariantes da Camada 3 continuam valendo."
  echo "Lembrete: isto verifica o codigo, nao substitui o Portao MP."
  echo "Ao fim de cada fase, ela precisa entrar pelo celular de verdade."
  exit 0
else
  echo "$FALHAS invariante(s) violada(s). NAO comite: isso pode quebrar o jogo dela."
  exit 1
fi
