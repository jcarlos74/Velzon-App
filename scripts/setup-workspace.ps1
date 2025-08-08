<#
.SYNOPSIS
    Script de configuração inicial para o workspace Full-Stack
.DESCRIPTION
    Este script configura todo o ambiente de desenvolvimento, instala dependências
    e prepara o workspace para desenvolvimento em Windows
.AUTHOR
    Gerado automaticamente para desenvolvimento Full-Stack
#>

# Configurar política de execução para o usuário atual (se necessário)
Write-Host "🔧 Configurando ambiente de desenvolvimento..." -ForegroundColor Green

# Verificar se estamos no diretório correto
if (-not (Test-Path "Velzon-Solution.code-workspace")) {
    Write-Host "❌ Execute este script no diretório raiz do projeto!" -ForegroundColor Red
    exit 1
}

try {
    # Tornar scripts executáveis (dar permissões)
    Write-Host "📝 Configurando permissões dos scripts..." -ForegroundColor Yellow
    
    # Verificar se .NET SDK está instalado
    Write-Host "🔍 Verificando .NET SDK..." -ForegroundColor Cyan
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        Write-Host "❌ .NET SDK não encontrado. Instale o .NET 9 SDK." -ForegroundColor Red
        Start-Process "https://dotnet.microsoft.com/download"
        exit 1
    }
    
    # Verificar versão do .NET
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET SDK versão: $dotnetVersion" -ForegroundColor Green
    
    # Verificar se Node.js está instalado  
    Write-Host "🔍 Verificando Node.js..." -ForegroundColor Cyan
    if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
        Write-Host "❌ Node.js não encontrado. Instale o Node.js LTS." -ForegroundColor Red
        Start-Process "https://nodejs.org/"
        exit 1
    }
    
    # Verificar versão do Node.js
    $nodeVersion = node --version
    Write-Host "✅ Node.js versão: $nodeVersion" -ForegroundColor Green
    
    # Instalar dependências do frontend se necessário
    if (Test-Path "frontend/velzon.web/package.json") {
        Write-Host "📦 Instalando dependências do frontend..." -ForegroundColor Yellow
        Set-Location "frontend"
        
        if (-not (Test-Path "node_modules")) {
            npm install --legacy-peer-deps
            if ($LASTEXITCODE -ne 0) {
                Write-Host "❌ Erro ao instalar dependências do frontend!" -ForegroundColor Red
                Set-Location ".."
                exit 1
            }
        }
        
        Set-Location ".."
        Write-Host "✅ Dependências do frontend instaladas!" -ForegroundColor Green
    }
    
    # Verificar se VS Code está instalado
    Write-Host "🔍 Verificando Visual Studio Code..." -ForegroundColor Cyan
    if (Get-Command code -ErrorAction SilentlyContinue) {
        Write-Host "✅ VS Code encontrado!" -ForegroundColor Green
        
        # Abrir workspace no VS Code
        Write-Host "📂 Abrindo workspace no VS Code..." -ForegroundColor Yellow
        Start-Process code -ArgumentList "Velzon-Solution.code-workspace"
        
        Write-Host "🎉 Workspace configurado com sucesso!" -ForegroundColor Green
        Write-Host "💡 O VS Code foi aberto. Use Ctrl+Shift+P -> 'Tasks: Run Task' para executar tarefas." -ForegroundColor Cyan
        
    } else {
        Write-Host "⚠️  VS Code não encontrado no PATH." -ForegroundColor Yellow
        Write-Host "💡 Instale o VS Code ou abra manualmente: Velzon-Solution.code-workspace" -ForegroundColor Cyan
    }
    
} catch {
    Write-Host "❌ Erro durante a configuração: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "🚀 Para iniciar o desenvolvimento, execute:" -ForegroundColor Cyan
Write-Host "   .\scripts\auto-start.ps1" -ForegroundColor White