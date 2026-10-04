$ErrorActionPreference = 'Stop'
$appDll = Join-Path $PSScriptRoot '../src/DesafioTarget/bin/Release/net10.0/DesafioTarget.dll'
$appDll = (Resolve-Path -LiteralPath $appDll).Path
$testDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ('target-console-tests-' + [guid]::NewGuid())
[System.IO.Directory]::CreateDirectory($testDirectory) | Out-Null
$script:total = 0

function Run-Console([string[]]$Lines) {
    $info = New-Object System.Diagnostics.ProcessStartInfo
    $info.FileName = (Get-Command dotnet).Source
    $info.Arguments = '"' + $appDll + '"'
    $info.WorkingDirectory = $testDirectory
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.RedirectStandardInput = $true
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $info.StandardOutputEncoding = [System.Text.Encoding]::UTF8
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $info
    $process.Start() | Out-Null
    $outputTask = $process.StandardOutput.ReadToEndAsync()
    $errorTask = $process.StandardError.ReadToEndAsync()
    foreach ($line in $Lines) { $process.StandardInput.WriteLine($line) }
    $process.StandardInput.Close()
    if (-not $process.WaitForExit(15000)) {
        $process.Kill()
        throw 'Console excedeu 15 segundos.'
    }
    $output = $outputTask.Result
    $errors = $errorTask.Result
    if ($process.ExitCode -ne 0 -or $errors) { throw "Erro no console: $errors" }
    $process.Dispose()
    return $output
}

function Check([string]$Name, [bool]$Condition) {
    if (-not $Condition) { throw "FALHA $Name" }
    $script:total++
    Write-Output "OK   $Name"
}

try {
    $output = Run-Console @('1', '2', '4', '0')
    Check 'Comissoes e total pelo console' ($output -match '1\.746,02' -and $output -match '495,69' -and $output -match '465,96' -and $output -match '379,38' -and $output -match '404,99')
    Check 'Estoque inicial pelo console' ($output -match '101 - Caneta Azul: 150 unidades' -and $output -match '105 - Marcador de Texto Amarelo: 90 unidades')
    Check 'Historico inicialmente vazio' ($output -match 'Nenhuma movimenta')
    $output = Run-Console @('3', '101', 'E', '20', 'Compra', '3', '101', 'S', '15', 'Venda', '4', '0')
    Check 'Entrada e saida retornam saldo' ($output -match 'Estoque final: 170 unidades' -and $output -match 'Estoque final: 155 unidades')
    Check 'IDs e descricoes no historico' ($output -match 'registrada: 1' -and $output -match 'registrada: 2' -and $output -match 'Entrada \| Compra' -and $output -match 'Saida \| Venda')
    $output = Run-Console @('2', '3', '101', 'S', '156', 'Venda', '2', '0')
    Check 'Persistencia apos reiniciar' ($output -match '101 - Caneta Azul: 155 unidades')
    Check 'Saldo insuficiente e recuperacao do menu' ($output -match 'Estoque insuficiente' -and ([regex]::Matches($output, '101 - Caneta Azul: 155 unidades').Count -eq 2))
    $today = [datetime]::Today
    $due = $today.AddDays(-10).ToString('dd/MM/yyyy')
    $output = Run-Console @('5', '1000,00', $due, '0')
    Check 'Juros com data atual real' ($output -match 'Dias em atraso: 10' -and $output -match 'Juros: R\$\s*250,00' -and $output -match 'Total: R\$\s*1\.250,00')
    $output = Run-Console @('5', '1000', $today.AddDays(1).ToString('dd/MM/yyyy'), '0')
    Check 'Vencimento futuro pelo console' ($output -match 'Dias em atraso: 0' -and $output -match 'Juros: R\$\s*0,00')
    $output = Run-Console @('9', '3', 'abc', '3', '101', 'X', '5', '1.000,50', '5', '100', '31/02/2026', '5', '-100', $due, '0')
    Check 'Opcao e codigo invalidos' ($output -match 'Op..o inv.lida' -and $output -match 'n.mero inteiro v.lido')
    Check 'Tipo e formato monetario invalidos' ($output -match 'Informe E ou S' -and $output -match 'usando v.rgula')
    Check 'Data impossivel e valor negativo' ($output -match 'data v.lida' -and $output -match 'valor n.o pode ser negativo')
    $output = Run-Console @('3', '101')
    Check 'Fim de entrada durante operacao' ($output -match 'Entrada encerrada')
    Write-Output "$script:total/$script:total testes de console passaram."
}
finally {
    # Apenas a pasta temporaria exclusiva criada acima e removida.
    [System.IO.Directory]::Delete($testDirectory, $true)
}
