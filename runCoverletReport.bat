del "./coverageReport/*.*?"

dotnet test CSharpSqlTests.sln --collect:"XPlat Code Coverage"

reportgenerator -reports:./**/coverage.cobertura.xml -targetdir:./coverageReport

start C:\dev\CSharpSqlTests\coverageReport\index.html