del "./coverageReport/*.*?"

dotnet test CSharpSqlTests.sln --collect:"XPlat Code Coverage"

reportgenerator -reports:./**/coverage.cobertura.xml -targetdir:./coverageReport -reporttypes:Cobertura

start C:\dev\CSharpSqlTests\coverageReport\index.html