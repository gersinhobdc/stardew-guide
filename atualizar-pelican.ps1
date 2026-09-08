# Baixa a versao mais recente do Pelican e compila direto para a pasta Mods.
#
# Use quando eu avisar que tem mudanca nova. Substitui o ciclo manual de
# "baixar ZIP no navegador -> extrair -> abrir PowerShell -> dotnet build".
#
# Como rodar: botao direito no arquivo -> "Executar com o PowerShell".
# Se o Windows reclamar de politica de execucao, rode uma vez:
#   Set-ExecutionPolicy -Scope CurrentUser RemoteSigned

$ErrorActionPreference = "Stop"

$repo   = "gersinhobdc/stardew-guide"
$branch = "claude/game-multiplayer-assistant-7zeczo"
$url    = "https://github.com/$repo/archive/refs/heads/$branch.zip"

$work = Join-Path $env:TEMP "pelican-build"
$zip  = Join-Path $env:TEMP "pelican.zip"

Write-Host ""
Write-Host "=== Atualizando o Pelican ===" -ForegroundColor Cyan

# O jogo trava a DLL enquanto roda; compilar por cima falha com erro confuso.
if (Get-Process -Name "Stardew Valley", "StardewModdingAPI" -ErrorAction SilentlyContinue) {
    Write-Host ""
    Write-Host "FECHE O JOGO PRIMEIRO." -ForegroundColor Red
    Write-Host "O Stardew esta aberto e mantem o arquivo do mod travado."
    Write-Host ""
    Read-Host "Feche o jogo e aperte Enter para tentar de novo"
}

Write-Host "[1/3] Baixando a versao mais recente..."
Invoke-WebRequest -Uri $url -OutFile $zip -UseBasicParsing

Write-Host "[2/3] Extraindo..."
if (Test-Path $work) { Remove-Item $work -Recurse -Force }
Expand-Archive -Path $zip -DestinationPath $work -Force

$proj = Get-ChildItem -Path $work -Filter "Pelican.csproj" -Recurse | Select-Object -First 1
if (-not $proj) {
    Write-Host "Nao achei o Pelican.csproj dentro do ZIP." -ForegroundColor Red
    Read-Host "Enter para sair"
    exit 1
}

Write-Host "[3/3] Compilando e instalando na pasta Mods..."
Write-Host ""
dotnet build $proj.FullName

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "PRONTO. Abra o jogo pelo SMAPI." -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "A compilacao falhou. Copie o texto vermelho acima e mande para o Claude." -ForegroundColor Red
}

Write-Host ""
Read-Host "Enter para fechar"
