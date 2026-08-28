# Pelican — assistente pessoal de Stardew Valley

Mod SMAPI que avisa, **na mesma tela do jogo**, o que você está prestes a perder e o que o
item na sua mão desbloqueia.

> **Requisito duro do projeto:** jogar multiplayer crossplay PC ↔ celular, sem que a outra
> pessoa instale nada. Leia **[MULTIPLAYER.md](MULTIPLAYER.md)** antes de qualquer coisa.
> Ele manda neste repositório.

---

## O princípio: instalar o que existe, construir só o que falta

Quase tudo que um assistente de Stardew precisa fazer **já existe de graça**. Reconstruir isso
seria semanas jogadas fora — e é o que mataria este projeto.

### Pré-requisitos (instale primeiro, são eles que fazem o grosso)

| Mod | O que resolve |
|---|---|
| [SMAPI](https://smapi.io) 4.x | Base. Não altera a versão do jogo |
| **UI Info Suite 2** | Sorte do dia, clima de amanhã, aniversários, dias até colheita, calendário em qualquer lugar |
| **Lookup Anything** | O "guia absoluto": F1 em qualquer item/NPC/peixe mostra tudo |
| **Community Center Companion** | Tooltip de bundle no inventário |

**Depois da Fase 0 você já tem ~70% do que queria, sem uma linha de código.**
Jogue uma semana assim antes de construir qualquer coisa.

### O que o Pelican adiciona (e que não existe em lugar nenhum)

1. **Staging no baú** — marque um baú como "baú do Centro Comunitário". O Pelican credita o
   que você guardou ali contra o que falta e avisa quando um bundle está **pronto pra entregar**.
2. **Cadeia de consequência** — os outros mods dizem *"este item é de um bundle"*. O Pelican diz
   **"Enguia → fecha Tanque de Peixes → libera a Bateia"**.
3. **Perdíveis de uma vez só** — avaliação do Vovô, Old Master Cannoli, Strange Capsule,
   Journal Scraps, Winter Mystery. Avisa **antes** do ponto de não retorno.
4. **Quadro de co-op printável** — estado da fazenda numa tela que você printa e manda no WhatsApp.

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
