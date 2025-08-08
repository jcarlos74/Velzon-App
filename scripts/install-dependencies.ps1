<#
.SYNOPSIS
    Script para instalar todas as dependências do projeto
.DESCRIPTION
    Instala dependências do backend (.NET) e frontend (Node.js/Angular)
    Verifica ferramentas necessárias e configura ambiente
.PARAMETER Force
    Força reinstalação das dependências mesmo se já existirem
.EXAMPLE
    .\install-dependencies.ps1
    .\install-dependencies.ps1 -Force
#>

param(
    [switch]$Force = $false
)

Write-Host "📦 Instalando Dependências do Projeto Full-Stack..." -ForegroundColor Green
Write-Host ""

# Função para verificar comando
function Test-Command {
    param([string]$Command)
    return (Get-Command $Command -ErrorAction SilentlyContinue) -ne $null
}

try {
    # Verificar ferramentas essenciais
    Write-Host "🔍 Verificando ferramentas necessárias..." -ForegroundColor Cyan
    
    # .NET SDK
    if (Test-Command "dotnet") {
        $dotnetVersion = dotnet --version
        Write-Host "✅ .NET SDK: $dotnetVersion" -ForegroundColor Green
    } else {
        Write-Host "❌ .NET SDK não encontrado!" -ForegroundColor Red
        Write-Host "💡 Instale em: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
        exit 1
    }
    
    # Node.js
    if (Test-Command "node") {
        $nodeVersion = node --version
        $npmVersion = npm --version
        Write-Host "✅ Node.js: $nodeVersion" -ForegroundColor Green
        Write-Host "✅ NPM: v$npmVersion" -ForegroundColor Green
    } else {
        Write-Host "❌ Node.js não encontrado!" -ForegroundColor Red
        Write-Host "💡 Instale em: https://nodejs.org/" -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host ""
    
    # Instalar dependências do Backend
    if (Test-Path "backend") {
        Write-Host "🔧 Processando Backend (.NET)..." -ForegroundColor Cyan
        Set-Location "backend"
        
        if ($Force) {
            Write-Host "   🧹 Limpando build anterior..." -ForegroundColor Yellow
            dotnet clean --verbosity quiet
        }
        
        Write-Host "   📥 Restaurando pacotes NuGet..." -ForegroundColor Yellow
        dotnet restore
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ Erro ao restaurar pacotes do backend!" -ForegroundColor Red
            Set-Location ".."
            exit 1
        }
        
        Write-Host "   🔨 Fazendo build do backend..." -ForegroundColor Yellow
        dotnet build --no-restore --verbosity quiet
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ Erro no build do backend!" -ForegroundColor Red
            Set-Location ".."
            exit 1
        }
        
        Set-Location ".."
        Write-Host "✅ Backend configurado com sucesso!" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Pasta backend não encontrada." -ForegroundColor Yellow
    }
    
    Write-Host ""
    
    # Instalar dependências do Frontend  
    if (Test-Path "frontend/velzon.web/package.json") {
        Write-Host "🌐 Processando Frontend (Angular/NX)..." -ForegroundColor Cyan
        Set-Location "frontend/velzon.web"
        
        # Verificar se NX está instalado globalmente
        if (-not (Test-Command "nx")) {
            Write-Host "   📥 Instalando NX CLI globalmente..." -ForegroundColor Yellow
            npm install -g nx@latest
            
            if ($LASTEXITCODE -ne 0) {
                Write-Host "❌ Erro ao instalar NX CLI!" -ForegroundColor Red
                Set-Location ".."
                exit 1
            }
        }
        
        # Limpar node_modules se Force
        if ($Force -and (Test-Path "node_modules")) {
            Write-Host "   🧹 Removendo node_modules existente..." -ForegroundColor Yellow
            Remove-Item "node_modules" -Recurse -Force
        }
        
        if ($Force -and (Test-Path "package-lock.json")) {
            Write-Host "   🧹 Removendo package-lock.json..." -ForegroundColor Yellow  
            Remove-Item "package-lock.json" -Force
        }
        
        # Instalar dependências
        Write-Host "   📥 Instalando dependências npm..." -ForegroundColor Yellow
        npm install --legacy-peer-deps
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ Erro ao instalar dependências do frontend!" -ForegroundColor Red
            Set-Location ".."
            exit 1
        }
        
        # Verificar se projetos foram criados corretamente
        Write-Host "   🔍 Verificando projetos NX..." -ForegroundColor Yellow
        $nxProjects = npx nx show projects 2>$null
        
        if ($nxProjects) {
            Write-Host "   ✅ Projetos NX encontrados:" -ForegroundColor Green
            $nxProjects | ForEach-Object { Write-Host "      - $_" -ForegroundColor Gray }
        }
        
        Set-Location ".."
        Write-Host "✅ Frontend configurado com sucesso!" -ForegroundColor Green
    } else {
        Write-Host "⚠️  package.json do frontend não encontrado." -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "🎉 Todas as dependências foram instaladas com sucesso!" -ForegroundColor Green
    Write-Host ""
    Write-Host "🚀 Próximos passos:" -ForegroundColor Cyan
    Write-Host "   1. Execute: .\scripts\setup-workspace.ps1" -ForegroundColor White
    Write-Host "   2. Ou execute: .\scripts\auto-start.ps1" -ForegroundColor White
    Write-Host ""
    
} catch {
    Write-Host "❌ Erro durante instalação: $_" -ForegroundColor Red
    exit 1
}
