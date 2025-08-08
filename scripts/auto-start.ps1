<#
.SYNOPSIS
    Script para iniciar automaticamente o ambiente de desenvolvimento Full-Stack
.DESCRIPTION
    Inicia o backend .NET e frontend Angular com micro-frontends simultaneamente
    Monitora os processos e permite parada controlada
.PARAMETER SkipBuild
    Pula a etapa de build dos projetos
.EXAMPLE
    .\auto-start.ps1
    .\auto-start.ps1 -SkipBuild
#>

param(
    [switch]$SkipBuild = $false
)

# Configurar cores para output
$Host.UI.RawUI.WindowTitle = "Full-Stack Development - Velzon"

Write-Host "🚀 Iniciando Ambiente de Desenvolvimento Full-Stack..." -ForegroundColor Green
Write-Host "⏰ $(Get-Date -Format 'HH:mm:ss')" -ForegroundColor Gray
Write-Host ""

# Verificar se estamos no diretório correto
if (-not (Test-Path "backend") -or -not (Test-Path "frontend\velzon.web")) {
    Write-Host "❌ Execute este script no diretório raiz do projeto!" -ForegroundColor Red
    Write-Host "💡 Certifique-se que as pastas 'backend' e 'frontend\velzon.web' existem." -ForegroundColor Yellow
    exit 1
}

# Arrays para armazenar processos
$processes = @()

# Função para cleanup dos processos
function Stop-AllProcesses {
    Write-Host ""
    Write-Host "🛑 Parando todos os serviços..." -ForegroundColor Yellow
    
    foreach ($process in $processes) {
        if ($process -and -not $process.HasExited) {
            try {
                Write-Host "   Parando processo $($process.ProcessName) (ID: $($process.Id))" -ForegroundColor Gray
                $process.Kill()
                $process.WaitForExit(5000)  # Aguardar até 5 segundos
            } catch {
                Write-Host "   ⚠️  Erro ao parar processo: $_" -ForegroundColor Yellow
            }
        }
    }
    
    Write-Host "✅ Todos os serviços foram parados." -ForegroundColor Green
    exit 0
}

# Registrar handler para Ctrl+C
Register-EngineEvent -SourceIdentifier PowerShell.Exiting -Action { Stop-AllProcesses }

try {
    # Build do Backend (opcional)
    if (-not $SkipBuild) {
        Write-Host "🔧 Fazendo build do backend..." -ForegroundColor Cyan
        Set-Location "backend"
        $buildResult = dotnet build --verbosity quiet
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ Erro no build do backend!" -ForegroundColor Red
            Set-Location ".."
            exit 1
        }
        Set-Location ".."
        Write-Host "✅ Build do backend concluído!" -ForegroundColor Green
    }
    
    # Iniciar Backend API
    Write-Host "🔧 Iniciando Backend API (.NET)..." -ForegroundColor Cyan
    Set-Location "backend"
    
    $backendProcess = Start-Process -FilePath "dotnet" -ArgumentList @(
        "run", 
        "--project", "src\Velzon.Api",
        "--urls", "https://localhost:5001;http://localhost:5000"
    ) -PassThru -NoNewWindow
    
    $processes += $backendProcess
    Set-Location ".."
    
    Write-Host "✅ Backend iniciado (PID: $($backendProcess.Id))" -ForegroundColor Green
    
    # Aguardar backend inicializar
    Write-Host "⏳ Aguardando backend inicializar..." -ForegroundColor Yellow
    Start-Sleep -Seconds 8
    
    # Verificar se frontend existe e tem dependências
    if (Test-Path "frontend\velzon.web\package.json") {
        Set-Location "frontend\velzon.web"
        
        # Verificar se node_modules existe
        if (-not (Test-Path "node_modules")) {
            Write-Host "📦 Instalando dependências do frontend..." -ForegroundColor Yellow
            npm install --legacy-peer-deps
            if ($LASTEXITCODE -ne 0) {
                Write-Host "❌ Erro ao instalar dependências!" -ForegroundColor Red
                Set-Location "..\\.."
                Stop-AllProcesses
                exit 1
            }
        }
        
        # Iniciar Frontend com todos os micro-frontends
        Write-Host "🌐 Iniciando Frontend (Angular NX)..." -ForegroundColor Cyan
        
        # Verificar se Node.js e NPX estão disponíveis
        $nodeVersion = node --version 2>$null
        if (-not $nodeVersion) {
            Write-Host "❌ Node.js não encontrado! Instale o Node.js primeiro." -ForegroundColor Red
            Set-Location "..\\.."
            Stop-AllProcesses
            exit 1
        }
        
        # Tentar diferentes métodos para executar o comando
        try {
            # Método 1: Usar PowerShell para executar o comando completo
            $frontendProcess = Start-Process -FilePath "powershell" -ArgumentList @(
                "-Command", "nx serve velzon-app "
            ) -PassThru -NoNewWindow
        } catch {
            Write-Host "⚠️  Método 1 falhou, tentando método alternativo..." -ForegroundColor Yellow
            try {
                # Método 2: Usar cmd com npx
                $frontendProcess = Start-Process -FilePath "cmd" -ArgumentList @(
                    "/c", "nx serve velzon-app "
                ) -PassThru -NoNewWindow
            } catch {
                Write-Host "❌ Não foi possível iniciar o frontend. Verifique se o NX está instalado:" -ForegroundColor Red
                Write-Host "   npm install -g @nrwl/cli" -ForegroundColor Yellow
                Set-Location "..\\.."
                Stop-AllProcesses
                exit 1
            }
        }
        
        $processes += $frontendProcess
        Set-Location "..\\.."
        
        Write-Host "✅ Frontend iniciado (PID: $($frontendProcess.Id))" -ForegroundColor Green
        
    } else {
        Write-Host "⚠️  Frontend não encontrado ou package.json ausente." -ForegroundColor Yellow
    }
    
    # Informações dos serviços
    Write-Host ""
    Write-Host "🎉 Ambiente de desenvolvimento iniciado com sucesso!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 Serviços disponíveis:" -ForegroundColor Cyan
    Write-Host "   🔧 Backend API: " -NoNewline -ForegroundColor White
    Write-Host "https://localhost:5001" -ForegroundColor Yellow
    Write-Host "   🌐 Frontend Shell: " -NoNewline -ForegroundColor White  
    Write-Host "http://localhost:4200" -ForegroundColor Yellow
    Write-Host "   📊 Controle de Acesso: " -NoNewline -ForegroundColor White
    Write-Host "http://localhost:4201" -ForegroundColor Yellow
    # Write-Host "   👥 Users MF: " -NoNewline -ForegroundColor White
    # Write-Host "http://localhost:4202" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "💡 Pressione Ctrl+C para parar todos os serviços" -ForegroundColor Cyan
    Write-Host "🔍 Monitore os logs nos terminais ou use o VS Code" -ForegroundColor Gray
    Write-Host ""
    
    # Loop principal - manter script executando
    while ($true) {
        Start-Sleep -Seconds 2
        
        # Verificar se processos ainda estão rodando
        $runningProcesses = $processes | Where-Object { $_ -and -not $_.HasExited }
        
        if ($runningProcesses.Count -eq 0) {
            Write-Host "⚠️  Todos os processos terminaram. Saindo..." -ForegroundColor Yellow
            break
        }
    }
    
} catch {
    Write-Host "❌ Erro durante a inicialização: $_" -ForegroundColor Red
    Stop-AllProcesses
    exit 1
} finally {
    # Cleanup final caso não tenha sido executado
    if ($processes.Count -gt 0) {
        Stop-AllProcesses
    }
}