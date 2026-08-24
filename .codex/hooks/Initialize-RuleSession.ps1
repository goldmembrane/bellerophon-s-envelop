param(
    [string]$HookInputJson,
    [string]$HookDirectory
)

try {
    $hookInput = Read-HookInput $HookInputJson
    $sessionId = [string](Get-HookValue $hookInput 'session_id' '')
    $state = Read-SessionState $sessionId
    $state = Sync-RuleManifest $state
    $state['rulesInjected'] = $true
    Write-SessionState $sessionId $state

    $eventName = [string](Get-HookValue $hookInput 'hook_event_name' 'SessionStart')
    $context = Get-RuleContext
    Write-HookJson @{
        hookSpecificOutput = @{
            hookEventName = $eventName
            additionalContext = $context
        }
    }
}
catch {
    $fallbackInput = @{}
    try {
        $fallbackInput = Read-HookInput $HookInputJson
    }
    catch {
        $fallbackInput = @{ hook_event_name = 'SessionStart'; session_id = '' }
    }
    Write-HookValidationFailure $fallbackInput ('세션 규칙 초기화에 실패했습니다: ' + $_.Exception.Message)
}
