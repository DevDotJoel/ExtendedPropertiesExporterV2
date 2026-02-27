# Extended Properties Exporter

Aplicação de consola que exporta as **Extended Properties** de uma base de dados SQL Server e atualiza os ficheiros `.sql` de um projeto.

## O que faz

1. Liga-se a uma base de dados SQL Server
2. Obtém todas as Extended Properties (ao nível das tabelas e das colunas) numa única query
3. Pesquisa recursivamente ficheiros `.sql` a partir de uma pasta raiz
4. Para cada ficheiro `.sql` que corresponda ao nome de uma tabela e contenha `CREATE TABLE`:
   - Remove todos os blocos `GO` + `EXECUTE sp_addextendedproperty` existentes
   - Adiciona as Extended Properties atualizadas, com o schema correto

## Como usar

1. Compilar e executar o projeto
2. Introduzir:
   - **Server Name** — nome do servidor SQL
   - **Database Name** — nome da base de dados
   - **Root Path** — caminho raiz onde se encontram os ficheiros `.sql` (pesquisa recursiva)
3. A aplicação processa todos os ficheiros e apresenta o resultado

## Funcionalidades

- Pesquisa recursiva de ficheiros `.sql` em toda a árvore de diretórios
- Suporte para múltiplos schemas (dbo, com, etc.)
- Extended Properties ao nível da tabela e ao nível da coluna
- Verificação de conteúdo (`CREATE TABLE`) antes de atualizar
- Templates configuráveis em `Templates/template.json`
- Query SQL externa e configurável em `Queries/GetAllExtendedProperties.sql`

## Estrutura

```
├── Program.cs                  — Ponto de entrada
├── Models/                     — Modelos de dados
├── Services/
│   ├── DatabaseService.cs      — Leitura da base de dados
│   └── SqlFileUpdater.cs       — Atualização dos ficheiros .sql
├── Settings/
│   └── AppSettings.cs          — Configuração
├── Queries/                    — Query SQL
└── Templates/                  — Templates das Extended Properties
```

## Importante

**Verificar sempre o resultado após a execução.**

Confirmar que os ficheiros `.sql` ficaram corretos antes de fazer commit, especialmente:

- Se os blocos `CREATE TABLE`, índices e constraints se mantêm intactos
- Se as Extended Properties foram adicionadas corretamente
- Se não ficaram `GO` soltos ou linhas em branco a mais
