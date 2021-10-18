cd C:\dev\CSharpSqlTests

coverlet ".\CSharpSqlTests.FrameworkTests\bin\Debug\net5.0\CSharpSqlTests.FrameworkTests.dll" --target "dotnet" --targetargs "test CSharpSqlTests.FrameworkTests\CSharpSqlTests.FrameworkTests.csproj --no-build" --format cobertura

del "./coverageReport/*.*?"

reportgenerator -reports:.\coverage.cobertura.xml -targetdir:./coverageReport

start C:\dev\CSharpSqlTests\coverageReport\index.html