[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet(
        'Initialize-RuleSession.ps1',
        'Capture-RuleApproval.ps1',
        'Guard-RuleCompliance.ps1',
        'Audit-RuleToolResult.ps1',
        'Check-RuleViolations.ps1'
    )]
    [string]$ScriptName
)

$ErrorActionPreference = 'Stop'
$utf8NoBom = New-Object Text.UTF8Encoding($false)
try {
    [Console]::InputEncoding = $utf8NoBom
    [Console]::OutputEncoding = $utf8NoBom
}
catch {
}
$OutputEncoding = $utf8NoBom
$hookInputJson = [Console]::In.ReadToEnd()
$hookDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path

function Write-BootstrapJson {
    param([hashtable]$Value)

    [Console]::Out.WriteLine(($Value | ConvertTo-Json -Depth 20 -Compress))
}

function Get-BootstrapFailureCount {
    param(
        [string]$SessionId,
        [string]$FailureKey
    )

    try {
        $safeSessionId = if ([string]::IsNullOrWhiteSpace($SessionId)) {
            'nosession'
        }
        else {
            $SessionId -replace '[^A-Za-z0-9._-]', '_'
        }

        $stateDirectory = Join-Path (Split-Path -Parent $hookDirectory) 'state'
        [void][IO.Directory]::CreateDirectory($stateDirectory)
        $statePath = Join-Path $stateDirectory ('bootstrap-failures-' + $safeSessionId + '.json')
        $state = @{}

        if ([IO.File]::Exists($statePath)) {
            $raw = [IO.File]::ReadAllText($statePath, (New-Object Text.UTF8Encoding($false)))
            if (-not [string]::IsNullOrWhiteSpace($raw)) {
                $parsed = $raw | ConvertFrom-Json
                foreach ($property in $parsed.PSObject.Properties) {
                    $state[$property.Name] = [int]$property.Value
                }
            }
        }

        $count = 1
        if ($state.ContainsKey($FailureKey)) {
            $count = [int]$state[$FailureKey] + 1
        }
        $state[$FailureKey] = $count

        $temporaryPath = $statePath + '.' + [Guid]::NewGuid().ToString('N') + '.tmp'
        [IO.File]::WriteAllText(
            $temporaryPath,
            ($state | ConvertTo-Json -Depth 5 -Compress),
            (New-Object Text.UTF8Encoding($false))
        )
        Move-Item -LiteralPath $temporaryPath -Destination $statePath -Force
        return $count
    }
    catch {
        return 1
    }
}

try {
    $targetPath = Join-Path $hookDirectory $ScriptName
    if (-not [IO.File]::Exists($targetPath)) {
        throw ('Hook handler is missing: ' + $ScriptName)
    }

    $utf8 = New-Object Text.UTF8Encoding($false, $true)
    $commonPath = Join-Path $hookDirectory 'RuleHook.Common.ps1'
    if (-not [IO.File]::Exists($commonPath)) {
        throw 'Hook common library is missing: RuleHook.Common.ps1'
    }
    $commonSource = [IO.File]::ReadAllText($commonPath, $utf8)
    $commonLibrary = [ScriptBlock]::Create($commonSource)
    . $commonLibrary

    $source = [IO.File]::ReadAllText($targetPath, $utf8)
    $handler = [ScriptBlock]::Create($source)
    & $handler -HookInputJson $hookInputJson -HookDirectory $hookDirectory
    exit 0
}
catch {
    $eventName = ''
    $sessionId = ''
    try {
        $inputObject = $hookInputJson | ConvertFrom-Json
        $eventName = [string]$inputObject.hook_event_name
        $sessionId = [string]$inputObject.session_id
    }
    catch {
    }

    $reason = 'Rule hook bootstrap validation failed for ' + $ScriptName + ': ' + $_.Exception.Message
    $hasher = [Security.Cryptography.SHA256]::Create()
    try {
        $failureBytes = [Text.Encoding]::UTF8.GetBytes($reason)
        $failureKey = [BitConverter]::ToString($hasher.ComputeHash($failureBytes)).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $hasher.Dispose()
    }

    $failureCount = Get-BootstrapFailureCount -SessionId $sessionId -FailureKey $failureKey
    $repeated = $failureCount -ge 2
    $message = if ($repeated) {
        $reason + ' The same validation failure occurred twice. Stop the task and report it to the user.'
    }
    else {
        $reason + ' Report the validation failure to the user, then retry once without expanding the approved scope.'
    }

    switch ($eventName) {
        'UserPromptSubmit' {
            Write-BootstrapJson @{
                systemMessage = $message
                hookSpecificOutput = @{
                    hookEventName = 'UserPromptSubmit'
                    additionalContext = $message + ' The user prompt was accepted and must not be blocked.'
                }
            }
        }
        'PreToolUse' {
            Write-BootstrapJson @{
                hookSpecificOutput = @{
                    hookEventName = 'PreToolUse'
                    permissionDecision = 'deny'
                    permissionDecisionReason = $message
                }
            }
        }
        'PermissionRequest' {
            Write-BootstrapJson @{
                hookSpecificOutput = @{
                    hookEventName = 'PermissionRequest'
                    decision = @{
                        behavior = 'deny'
                        message = $message
                    }
                }
            }
        }
        'PostToolUse' {
            Write-BootstrapJson @{
                decision = 'block'
                reason = $message
                hookSpecificOutput = @{
                    hookEventName = 'PostToolUse'
                    additionalContext = $message
                }
            }
        }
        'Stop' {
            if ($repeated) {
                Write-BootstrapJson @{
                    continue = $false
                    stopReason = $message
                    systemMessage = $message
                }
            }
            else {
                Write-BootstrapJson @{
                    decision = 'block'
                    reason = $message
                }
            }
        }
        'SubagentStop' {
            if ($repeated) {
                Write-BootstrapJson @{
                    continue = $false
                    stopReason = $message
                    systemMessage = $message
                }
            }
            else {
                Write-BootstrapJson @{
                    decision = 'block'
                    reason = $message
                }
            }
        }
        default {
            Write-BootstrapJson @{
                continue = (-not $repeated)
                stopReason = if ($repeated) { $message } else { $null }
                systemMessage = $message
            }
        }
    }

    exit 0
}
