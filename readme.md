# Extended Properties Exporter

Aplicação de consola que atualiza as **Extended Properties** nos ficheiros `.sql` de um projeto de base de dados.

## O que faz

1. Liga-se a uma base de dados SQL Server
2. Lê todas as tabelas e as respetivas Extended Properties (ao nível das colunas)
3. Acede à pasta de tabelas do projeto de destino (ex.: `DB_RiskLevel\dbo\Tables`)
4. Para cada ficheiro `.sql`:
   - Remove todos os blocos `GO` + `EXECUTE sp_addextendedproperty` existentes (independentemente do formato)
   - Adiciona as Extended Properties atualizadas, lidas diretamente da base de dados

## Como usar

1. Compilar e executar o projeto
2. Introduzir:
   - **Server Name** — nome do servidor SQL
   - **Database Name** — nome da base de dados
   - **Project Tables Path** — caminho completo para a pasta com os ficheiros `.sql` das tabelas
3. A aplicação processa todos os ficheiros e apresenta o resultado

## Estrutura

```
├── Program.cs                  — Ponto de entrada
├── Models/                     — Modelos de dados
├── Services/
│   ├── DatabaseService.cs      — Leitura da base de dados
│   └── SqlFileUpdater.cs       — Atualização dos ficheiros .sql
├── Settings/
│   └── AppSettings.cs          — Configuração
├── Queries/                    — Queries SQL (.sql)
└── Templates/                  — Template da Extended Property
```

## Importante

**Verificar sempre o resultado após a execução.**

Confirmar que os ficheiros `.sql` ficaram corretos antes de fazer commit, especialmente:

- Se os blocos `CREATE TABLE`, índices e constraints se mantêm intactos
- Se as Extended Properties foram adicionadas corretamente
- Se não ficaram `GO` soltos ou linhas em branco a mais
