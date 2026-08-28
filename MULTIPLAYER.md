# Garantia de multiplayer crossplay

> **A regra que manda em tudo neste projeto:** o assistente é opcional, o multiplayer não.
> Se as duas coisas entrarem em conflito, o assistente perde. Sempre.

Ela joga no celular, não instala nada, e não quer instalar nada. Este documento existe para
garantir isso — e para você conseguir reverter em 60 segundos se algo der errado numa noite
em que vocês querem jogar.

---

## As 5 camadas

Cada camada sozinha já seria suficiente. Elas não falham juntas.

### Camada 1 — A versão do jogo não muda

É o ponto central. **O SMAPI não altera a versão do Stardew Valley.** Ele se instala *ao lado*
do executável e carrega o jogo. Nada do que o celular dela enxerga muda. O crossplay que
funciona hoje entre vocês continua funcionando idêntico.

> ⚠️ **O único vetor real de quebra neste projeto não é o SMAPI — é atualizar o jogo.**
>
> Se o seu PC estiver numa versão anterior e você atualizar só para instalar o SMAPI, você
> quebra a paridade de versão com o celular dela e vocês param de jogar juntos.
>
> **Confirme a versão do celular dela ANTES de tocar em qualquer coisa.**
> Se as versões já divergirem hoje, o problema é esse — e existe com ou sem este projeto.

### Camada 2 — O SMAPI trata cliente vanilla explicitamente

Isso não é suposição de fórum. Está no código do SMAPI (`src/SMAPI/Framework/SMultiplayer.cs`):

- Existem caminhos nomeados para `"Received connection for vanilla player"` e
  `"Received connection for vanilla host"`. Cliente sem SMAPI é um caso **previsto e
  suportado**, não um acidente.
- Peers vanilla entram com `model: null` e `HasSmapi = false`.
- Mensagens de mod são filtradas com `Where(p => p.HasSmapi)` — **nada que o SMAPI ou um mod
  envie chega ao celular dela.** Ela é excluída da comunicação de mods por construção.
- Clientes vanilla ignoram tipos de mensagem que não conhecem.

A existência de `IMultiplayerPeer.HasSmapi`, com as propriedades vizinhas documentadas como
"válidas se HasSmapi for true", prova que sessão mista host-modded / cliente-vanilla é um
cenário de projeto do SMAPI. É literalmente o seu caso.

### Camada 3 — O Pelican não tem como quebrar nada

Regras invioláveis no código. É isso que separa "mod seguro" de "mod que desincroniza":

| Regra | Por quê |
|---|---|
| **Somente leitura** | Nunca escreve estado de jogo, nunca cria/remove item, nunca move nada |
| **Somente desenho** | Só `RenderedHud` e menus próprios |
| **Zero conteúdo novo** | Nenhum item, NPC, mapa, receita, nem `AssetRequested` que edite conteúdo |
| **Zero rede** | Nada de `IMultiplayer.SendMessage` |

**É adicionar conteúdo que quebra cliente vanilla.** Um mod que só desenha na sua tela é
invisível para a rede. O Pelican, por definição, não adiciona nada.

Os mods prontos da Fase 0 são todos da mesma categoria: HUD e tooltip, client-side, sem conteúdo.

### Camada 4 — Reversível em 60 segundos

O SMAPI **não modifica os arquivos do jogo**.

**Recomendado — dois botões no Steam:** adicione o SMAPI como jogo não-Steam. Você fica com
"Stardew Valley" (vanilla) e "Stardew modded". Se qualquer coisa der errado numa noite em que
vocês querem jogar, clica no vanilla e joga.

**O assistente nunca vira pré-requisito pra vocês jogarem.**

Reversão total, se quiser: rode o instalador do SMAPI e escolha `uninstall`, depois limpe as
launch options (Steam → botão direito em Stardew Valley → Propriedades → Geral → Opções de
inicialização → apagar o conteúdo).

### Camada 5 — Prova empírica antes de escrever código

Nada disso vale sem teste real. Por isso a Fase 0 acontece **antes de qualquer linha de código**.

---

## Procedimento da Fase 0

A ordem não é negociável.

- [ ] **1. Confirme a versão do jogo nos dois lados.** A dela no celular, a sua no PC.
      Se divergirem, pare e resolva isso primeiro.
- [ ] **2. Baseline:** ela entra pelo celular na sua partida como faz normalmente. Funciona?
- [ ] **3. Instale no seu PC, sem atualizar o jogo:** SMAPI 4.x, UI Info Suite 2,
      Lookup Anything, Community Center Companion.
- [ ] **4. Configure os dois botões no Steam** (vanilla e modded).
- [ ] **5. Teste decisivo:** ela entra de novo pelo celular.

> ### 🚦 Portão 0
> **Ela entrou no passo 5?**
>
> - **Sim** → requisito de multiplayer provado empiricamente. Todo o resto do plano liberado.
> - **Não** → **pare.** Desinstale o SMAPI e o projeto vira o app de navegador da v3, sem
>   inventário ao vivo. Custo dessa descoberta: uma noite, zero código.

---

## Portão MP — a cada fase

**Ao fim de toda fase, ela entra pelo celular com o Pelican ativo.**

Se falhar, reverte a fase. Não negocie, não investigue "só mais um pouco", não deixe pra
depois. Multiplayer sempre ganha.

Registre aqui:

| Data | Fase | Versão PC | Versão celular | Ela entrou? |
|---|---|---|---|---|
| | Fase 0 | | | |
| | Fase 1 | | | |
| | Fase 2 | | | |
| | Fase 3 | | | |

---

## Riscos que continuam de pé (e não são culpa do mod)

- **O co-op de celular é experimental por natureza.** Exige mesma versão dos dois lados e,
  remoto, VPN (Radmin/ZeroTier). É frágil por conta própria e **independe deste projeto** —
  mas é exatamente por isso que a regra de versão da Camada 1 é inegociável.
- **Atualização automática do Steam.** Se o Steam atualizar seu jogo sozinho e o celular dela
  ficar pra trás, vocês param de jogar juntos e não tem nada a ver com o Pelican. Considere
  desligar a atualização automática enquanto a versão dela não acompanhar.
