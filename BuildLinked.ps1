# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/P5R.BatonPassRecovery/*" -Force -Recurse
dotnet publish "./P5R.BatonPassRecovery.csproj" -c Release -o "$env:RELOADEDIIMODS/P5R.BatonPassRecovery" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location