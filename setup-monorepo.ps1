# Caminho base do repositório
$basePath = "E:\Siltec\Projetos\VELZON"
Set-Location $basePath

# Verificar se é um repositório Git
if (-not (Test-Path ".git")) {
    Write-Host "Inicializando repositório Git..."
    git init
    git remote add origin https://github.com/jcarlos74/Velzon-App.git
}

# Criar diretórios se não existirem
foreach ($dir in @("frontend", "backend", "scripts")) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir
        Write-Host "Criado diretório: $dir"
    }
}

# Git add, commit e push
git add .
git commit -m "feat: estrutura inicial monorepo com frontend, backend e scripts"
git push origin master
