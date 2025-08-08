# Velzon FullStack Solution

🚀 **Projeto Full-Stack com .NET 8, Angular 18 e Micro-frontends usando NX**

## 🛠️ Tecnologias

### Backend
- **.NET 9** - Framework principal
- **ASP.NET Core Web API** - API REST
- **Dapper** - ORM (opcional)
- **xUnit** - Testes unitários

### Frontend  
- **Angular 18** - Framework SPA
- **NX Workspace** - Monorepo e ferramentas
- **Module Federation** - Micro-frontends
- **SCSS** - Estilização
- **TypeScript** - Linguagem

## 📋 Pré-requisitos

### Windows
- **Windows 10/11**
- **PowerShell 5.1+** (incluído no Windows)
- **.NET 9 SDK** - [Download](https://dotnet.microsoft.com/download)
- **Node.js 18+ LTS** - [Download](https://nodejs.org/)
- **Visual Studio Code** - [Download](https://code.visualstudio.com/)

### Verificação de Instalação
```powershell
# Verificar versões instaladas
dotnet --version  # Deve ser 9.x.x
node --version    # Deve ser 18.x.x ou superior
npm --version     # Incluído com Node.js
code --version    # Visual Studio Code
```

## 🚀 Instalação e Configuração

### 1. Clonar/Baixar o Projeto
```powershell
# Se usando Git
git clone <url-do-repositorio>
cd Velzon

# Ou criar nova solução
mkdir Velzon
cd Velzon
```

### 2. Executar Configuração Automática
```powershell
# Instalar todas as dependências
.\scripts\install-dependencies.ps1

# Configurar workspace do VS Code
.\scripts\setup-workspace.ps1
```

### 3. Iniciar Desenvolvimento
```powershell
# Iniciar todos os serviços
.\scripts\auto-start.ps1

# Ou usar VS Code
# Ctrl+Shift+P -> "Tasks: Run Task" -> "🚀 Iniciar Full Stack"
```

## 🌐 URLs de Desenvolvimento

Após iniciar os serviços:

| Serviço | URL | Descrição |
|---------|-----|-----------|
| 🔧 **Backend API** | https://localhost:5001 | API REST principal |
| 🌐 **Frontend Shell** | http://localhost:4200 | Aplicação principal |
| 📊 **Controle de Acesso** | http://localhost:4201 | Micro-frontend Dashboard |
| 👥 **Users MF** | http://localhost:4202 | Micro-frontend Usuários |

## 🎯 Scripts Disponíveis

### PowerShell Scripts

| Script | Descrição |
|--------|-----------|
| `install-dependencies.ps1` | Instala todas as dependências |
| `setup-workspace.ps1` | Configura workspace VS Code |
| `auto-start.ps1` | Inicia desenvolvimento completo |

### VS Code Tasks

| Task | Atalho | Descrição |
|------|--------|-----------|
| 🚀 Iniciar Full Stack | `Ctrl+Shift+P` | Inicia backend + frontend |
| 🔨 Build Full Stack | - | Build completo |
| 🧪 Executar Testes | - | Testes backend + frontend |
| 📦 Instalar Dependências | - | Instala/atualiza dependências |

## 🐛 Debug e Desenvolvimento

### VS Code Debug
1. **F5** - Debug Full Stack (Backend + Frontend)
2. **Ctrl+Shift+D** - Painel de Debug
3. Escolher configuração desejada

### Breakpoints
- ✅ **Backend**: Breakpoints em arquivos `.cs`
- ✅ **Frontend**: Breakpoints em arquivos `.ts`
- ✅ **Source Maps**: Mapeamento automático

## 📁 Estrutura do Projeto

```
Velzon-Solution/
├── 🔧 backend/                    # Projetos .NET
│   ├── Velzon.sln                 # Solution principal
│   ├── src/
│   │   ├── Velzon.Api/            # Web API
│   │   ├── Velzon.Core/           # Domain/Business
│   │   └── Velzon.Infra/          # Data Access
│   └── tests/
│       └── Velzon.Tests/          # Testes unitários
├── 🌐 frontend/                   # Workspace Angular NX
│   ├── apps/
│   │   ├── velzon-app/            # App principal (Host)
│   │   ├── velzon-cta/            # Micro-frontend Controle de Acesso
│   │   └── mf-users/              # Micro-frontend Users
│   └── libs/
│       └── shared/                # Biblioteca compartilhada
├── 📜 scripts/                    # Scripts PowerShell
│   ├── install-dependencies.ps1
│   ├── setup-workspace.ps1
│   └── auto-start.ps1
└── 🛠️ .vscode/                   # Configurações VS Code
    ├── settings.json
    ├── tasks.json
    ├── launch.json
    └── extensions.json
```

## ⚡ Comandos Úteis

### Backend (.NET)
```powershell
cd backend

# Restaurar pacotes
dotnet restore

# Build
dotnet build

# Executar API
dotnet run --project src/MyApp.Api

# Testes
dotnet test

# Limpar
dotnet clean
```

### Frontend (Angular NX)
```powershell
cd frontend

# Instalar dependências
npm install

# Servir aplicação principal
npx nx serve shell

# Servir todos micro-frontends
npx nx run-many --target=serve --projects=shell,mf-dashboard,mf-users --parallel=3

# Build de produção
npx nx build shell --prod

# Testes
npx nx test shell

# Lint
npx nx lint shell
```

## 🔧 Configuração de CORS

O backend já está configurado para aceitar requisições do frontend:

```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:4201", "http://localhost:4202")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

## 🚨 Solução de Problemas

### PowerShell Execution Policy
```powershell
# Se scripts não executarem, ajustar policy:
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Porta em Uso
```powershell
# Verificar processos usando portas
netstat -ano | findstr :5001
netstat -ano | findstr :4200

# Parar processo específico
taskkill /PID <PID> /F
```

### Limpar Dependências
```powershell
# Backend
cd backend
dotnet clean
dotnet nuget locals all --clear

# Frontend
cd frontend
Remove-Item node_modules -Recurse -Force
Remove-Item package-lock.json -Force
npm install
```

## 📚 Próximos Passos

1. **Implementar autenticação JWT**
2. **Configurar Entity Framework**
3. **Adicionar testes de integração**
4. **Configurar CI/CD**
5. **Deploy para Azure/AWS**

## 🤝 Contribuição

1. Fork o projeto
2. Crie sua feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abra um Pull Request

## 📝 Licença

Este projeto está sob a licença MIT. Veja o arquivo [LICENSE](LICENSE) para mais detalhes.
```

### 6.2 .gitignore Otimizado
```gitignore
# Arquivos de Sistema Operacional
.DS_Store
.DS_Store?
._*
.Spotlight-V100
.Trashes
ehthumbs.db
Thumbs.db
*~

# Visual Studio / VS Code
.vs/
.vscode/settings.json
.vscode/tasks.json
.vscode/launch.json
.vscode/extensions.json
*.suo
*.user
*.userosscache
*.sln.docstates
*.userprefs

# Backend .NET
backend/bin/
backend/obj/
backend/**/*.cache
backend/**/*.dll
backend/**/*.exe
backend/**/*.pdb
backend/**/*.dll.config
backend/**/*.cache
backend/**/*.tlb
backend/**/*.tlh
backend/**/*.tmp
backend/**/*.tmp_proj
backend/**/*.log
backend/**/*.vspscc
backend/**/*.vssscc
backend/.builds
backend/**/*.pidb
backend/**/*.svclog
backend/**/*.scc

# Frontend Node.js / Angular
frontend/node_modules/
frontend/npm-debug.log*
frontend/yarn-debug.log*
frontend/yarn-error.log*
frontend/lerna-debug.log*
frontend/.pnpm-debug.log*
frontend/dist/
frontend/.nx/
frontend/.angular/
frontend/coverage/
frontend/**/.nyc_output
frontend/**/.grunt
frontend/**/bower_components
frontend/**/.lock-wscript
frontend/**/.npm
frontend/**/.eslintcache
frontend/**/.stylelintcache
frontend/**/.rpt2_cache/
frontend/**/.rts2_cache_cjs/
frontend/**/.rts2_cache_es/
frontend/**/.rts2_cache_umd/

# Logs
*.log
logs
pids
*.pid
*.seed
*.pid.lock

# Runtime data
pids
*.pid
*.seed
*.pid.lock

# Dependency directories
node_modules/
jspm_packages/

# Optional npm cache directory
.npm

# Optional eslint cache
.eslintcache

# Optional stylelint cache
.stylelintcache

# Microbundle cache
.rpt2_cache/
.rts2_cache_cjs/
.rts2_cache_es/
.rts2_cache_umd/

# Optional REPL history
.node_repl_history

# Output of 'npm pack'
*.tgz

# Yarn Integrity file
.yarn-integrity

# Environment variables
.env
.env.local
.env.development.local
.env.test.local
.env.production.local
.env.*.local

# Windows PowerShell
*.ps1.orig

# Temporary folders
tmp/
temp/
```

---

## 🚀 Como Executar Tudo (Windows)

### 🎯 Método 1: Setup Completo Automático
```powershell
# 1. Criar estrutura completa
mkdir MyFullStackSolution
cd MyFullStackSolution

# 2. Executar setup (criar todos os arquivos)
.\scripts\install-dependencies.ps1

# 3. Configurar workspace
.\scripts\setup-workspace.ps1

# 4. Iniciar desenvolvimento
# O VS Code abrirá automaticamente e iniciará os serviços
```

### 🎯 Método 2: VS Code Integrado
```powershell
# 1. Abrir workspace
code MyFullStackSolution.code-workspace

# 2. Executar task (Ctrl+Shift+P)
# "Tasks: Run Task" -> "🚀 Iniciar Full Stack"

# 3. Debug (F5)
# Escolher "🚀 Debug Full Stack"
```

### 🎯 Método 3: Scripts Individuais
```powershell
# Backend
cd backend
dotnet run --project src/MyApp.Api

# Frontend (novo terminal)
cd frontend  
npx nx run-many --target=serve --projects=shell,mf-dashboard,mf-users --parallel=3
```

---

## 💡 Recursos Especiais Windows

### ✅ **PowerShell Nativo**
- Scripts otimizados para Windows PowerShell 5.1+
- Não requer PowerShell Core (mas é compatível)
- Execução com `-ExecutionPolicy Bypass`

### ✅ **Detecção Automática**
- Verifica .NET SDK e Node.js automaticamente  
- Instala dependências NPM se necessário
- Abre VS Code automaticamente se disponível

### ✅ **Gerenciamento de Processos**
- Controle adequado de processos background
- Cleanup automático com Ctrl+C
- Monitoramento de status dos serviços

### ✅ **Integração VS Code**  
- Tasks otimizadas para Windows
- Debug configurations para Edge/Chrome
- Terminal PowerShell como padrão
- File nesting para organização

### ✅ **Error Handling Robusto**
- Verificação de pré-requisitos
- Mensagens de erro claras em português
- Fallbacks para diferentes cenários
- Logs detalhados de execução

Esta configuração fornece uma experiência completa de desenvolvimento Full-Stack otimizada especificamente para Windows com PowerShell!