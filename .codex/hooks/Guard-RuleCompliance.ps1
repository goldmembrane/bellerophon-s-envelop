param(
    [string]$HookInputJson,
    [string]$HookDirectory
)

try {
    $hookInput = Read-HookInput $HookInputJson
    $sessionId = [string](Get-HookValue $hookInput 'session_id' '')
    $eventName = [string](Get-HookValue $hookInput 'hook_event_name' 'PreToolUse')
    $state = Read-SessionState $sessionId

    $toolInput = Get-HookValue $hookInput 'tool_input' @{}
    $command = if ($toolInput -is [Collections.IDictionary] -and $toolInput.ContainsKey('command')) {
        [string]$toolInput['command']
    }
    else {
        ''
    }
    if ([string](Get-HookValue $hookInput 'tool_name' '') -eq 'Bash' -and (Test-IsNotificationCommand $command)) {
        if ($eventName -eq 'PermissionRequest') {
            Write-HookJson @{
                hookSpecificOutput = @{
                    hookEventName = 'PermissionRequest'
                    decision = @{ behavior = 'allow' }
                }
            }
        }
        return
    }

    if ($null -eq $state['activeApproval']) {
        $state = Restore-ApprovalFromTranscript $hookInput $state
    }
    if ($null -eq $state['manifest']) {
        $state = Sync-RuleManifest $state
    }
    else {
        $changes = @(Compare-RuleManifest $state['manifest'])
        if ($changes.Count -gt 0) {
            $configuration = Read-RuleSourceConfiguration
            $ruleBypass = $null -ne $state['activeApproval'] -and
                [bool]$state['activeApproval']['ruleMaintenanceBypass'] -and
                (Test-AllChangesAreRuleDocuments $changes $configuration)
            if ($ruleBypass) {
                $state = Sync-RuleManifest $state
            }
            else {
                Write-SessionState $sessionId $state
                $paths = @($changes | ForEach-Object { [string]$_['path'] }) -join ', '
                Write-HookValidationFailure $hookInput ('도구 실행 전에 규칙 또는 하네스 파일의 승인되지 않은 변경을 감지했습니다: ' + $paths)
                return
            }
        }
    }

    $decision = Test-ToolAgainstApproval $hookInput $state['activeApproval']
    if (-not [bool]$decision['allowed']) {
        Write-SessionState $sessionId $state
        Write-HookValidationFailure $hookInput ([string]$decision['reason'])
        return
    }

    if ($null -ne $state['activeApproval'] -and -not [bool]$state['activeApproval']['started']) {
        $state['activeApproval']['started'] = $true
        $state['activeApproval']['startedAtUtc'] = [DateTime]::UtcNow.ToString('o')
    }
    Write-SessionState $sessionId $state

    if ($eventName -eq 'PermissionRequest') {
        Write-HookJson @{
            hookSpecificOutput = @{
                hookEventName = 'PermissionRequest'
                decision = @{ behavior = 'allow' }
            }
        }
    }
}
catch {
    $fallbackInput = @{}
    try {
        $fallbackInput = Read-HookInput $HookInputJson
    }
    catch {
        $fallbackInput = @{ hook_event_name = 'PreToolUse'; session_id = '' }
    }
    Write-HookValidationFailure $fallbackInput ('도구 실행 전 규칙 검증에 실패했습니다: ' + $_.Exception.Message)
}
