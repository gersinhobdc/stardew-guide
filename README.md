# Pelican — assistente pessoal de Stardew Valley

Mod SMAPI que avisa, **na mesma tela do jogo**, o que você está prestes a perder e o que o
item na sua mão desbloqueia.

> **Requisito duro do projeto:** jogar multiplayer crossplay PC ↔ celular, sem que a outra
> pessoa instale nada. Leia **[MULTIPLAYER.md](MULTIPLAYER.md)** antes de qualquer coisa.
> Ele manda neste repositório.

---

## Dependência única: SMAPI

**O Pelican não depende de nenhum mod de terceiros.** Ele lê tudo direto do jogo —
`Game1.netWorldState.Value.BundleData`, `Data/Crops`, `Data/NPCGiftTastes`, `LibraryMuseum` —
em runtime. Instalou o [SMAPI](https://smapi.io) 4.x, está pronto.

Isso é decisão de projeto, não acidente: um sistema próprio não quebra porque outra pessoa
abandonou o mod dela, e não fica preso à versão que ela suporta.

### O que ele faz

| | |
|---|---|
| **Item na mão** | Serve pra qual bundle, quem ama de presente, museu ainda aceita |
| **Cadeia de consequência** | Não *"é de um bundle"*, mas **"Enguia → fecha Tanque de Peixes → libera a Bateia"** |
| **Staging no baú** | Marque um baú como baú do CC; ele credita o que está guardado e avisa quando um bundle está **pronto pra entregar** |
| **Colheita** | Prontas hoje/amanhã e, principalmente, as que **não dão tempo** de amadurecer antes da virada |
| **Prazo de plantio** | Último dia útil pra plantar cada cultura da estação |
| **Fim de estação** | O que some na virada |
| **Clima × pendências** | "Chove hoje **e** você precisa de Enguia, que só aparece na chuva" |
| **Sorte do dia** | Só quando é extrema o bastante pra mudar sua decisão |
| **Aniversários e festivais** | Hoje e nos próximos dias |
| **Perdíveis** | Avaliação do Vovô, Old Master Cannoli, Strange Capsule, Journal Scraps, Winter Mystery — avisa **antes** do ponto de não retorno |
| **Quadro de co-op** | F9 → print → WhatsApp |

### Mods opcionais (não são pré-requisito)

Se quiser conforto extra, estes convivem sem conflito — e o Pelican tem config pra desligar
qualquer sobreposição. **Ignorar todos não tira nada do que está na tabela acima.**

- **Lookup Anything** — F1 em qualquer coisa, com profundidade de enciclopédia
- **UI Info Suite 2** — alcance de aspersor, barra de XP, indicador de animais

---

## O que este projeto NÃO faz

Lista congelada. Cada item aqui foi cortado de propósito — reabrir qualquer um exige
reabrir o plano.

- ❌ **Editar save.** Nunca. Somente leitura, sempre.
- ❌ **Adicionar conteúdo** (item, NPC, mapa, receita). É o que quebraria o cliente dela.
- ❌ **Enviar mensagem de rede.** Nada de `IMultiplayer.SendMessage`.
- ❌ **Score unificado / ranking de "melhor atividade".** É chute com aparência de ciência.
- ❌ **Roteirizador de rota com horários.** Não sobrevive ao jogo real.
- ❌ **OCR.** Digitar é mais rápido.
- ❌ **LLM.** O mod tem um HUD e um painel.
- ❌ **Scraping de wiki.** Os dados vêm do próprio jogo, em runtime.
- ❌ **App de navegador / servidor local.** Você joga no PC; o HUD já é "a mesma tela".
- ❌ **Enciclopédia completa de todo item.** Isso é o Lookup Anything, e ele é melhor nisso.
  O Pelican responde *"o que faço com isto agora"*, não *"me conte tudo sobre isto"*.

---

## Estrutura

```
stardew-guide/
├─ MULTIPLAYER.md          # ★ as 5 camadas de garantia + procedimento de reversão
├─ README.md               # este arquivo
└─ Pelican/
   ├─ manifest.json
   ├─ Pelican.csproj
   ├─ ModEntry.cs
   ├─ Config/              # ModConfig
   ├─ Model/               # Insight, GameSnapshot
   ├─ Data/                # leitura dos dados do jogo (bundles etc.)
   ├─ Advisors/            # um arquivo por tipo de conselho
   ├─ Hud/                 # HudRenderer, CoopBoard
   ├─ Instrumentation/     # UsageLog — mede se o mod vale a pena
   └─ assets/              # os 2 únicos arquivos curados à mão
```

**Os dados vêm do jogo, não de arquivo curado.** `Game1.netWorldState.Value.BundleData`,
`Data/Crops`, `Data/Fish` etc. são lidos em runtime. Por isso a 1.7 não invalida o trabalho:
o jogo muda os dados, o mod lê os novos. Só dois arquivos são escritos à mão
(`room-rewards.json` e `secrets.json`), porque o jogo não codifica "isto é um segredo".

---

## Build

Precisa do .NET SDK 6 e do jogo instalado. O `Pathoschild.Stardew.ModBuildConfig` acha o jogo
sozinho e faz o deploy pra pasta `Mods/`.

```bash
cd Pelican
dotnet build
```

Abra o jogo pelo SMAPI e carregue um save. Se o HUD aparecer no canto, a toolchain está de pé.

### Verificação de segurança (rode antes de todo commit)

```bash
./scripts/verificar-seguranca.sh
```

Confere no código as 4 invariantes que protegem o multiplayer: zero rede, zero conteúdo novo,
zero patch, somente leitura. Isso torna a garantia da Camada 3 verificável em vez de prometida.

### Diagnóstico

Se algo parecer errado, rode no console do SMAPI:

```
pelican_dump
```

Ele despeja o estado que o mod está lendo (bundles parseados, itens que faltam, advisors ativos).
É a forma de descobrir se o problema é leitura de dados ou lógica.

---

## Portões

O projeto tem critérios de morte escritos antes de começar. Honre-os.

| Portão | Critério | Se falhar |
|---|---|---|
| **0** | Ela entra pelo celular com SMAPI instalado | Mata o projeto |
| **MP** | Ela entra pelo celular ao fim de **toda** fase | Reverte a fase |
| **1** | `usage.jsonl` mostra ≥ 10 insights agidos em 14 dias de jogo | Encerra — você fica com os mods prontos |
| **2** | Um bundle detectado como pronto no baú antes de você perceber | Corta a Fase 3 |
| **3** | Ela olha o print e entende sem legenda | Refaz a tela, não documenta |

Regras de encerramento:

1. **Duas semanas sem commit = o projeto está morto.** Escreva no README que morreu, e por quê.
2. Teto de **15 noites**. Estourou, corta função — nunca estende prazo.
3. A 1.7 sair antes da Fase 3 → congele, jogue a atualização, reavalie depois.
