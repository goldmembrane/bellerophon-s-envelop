param(
    [string]$HookInputJson,
    [string]$HookDirectory
)

try {
    $hookInput = Read-HookInput $HookInputJson
    $sessionId = [string](Get-HookValue $hookInput 'session_id' '')
    $state = Read-SessionState $sessionId
    if ($null -eq $state['activeApproval']) {
        $state = Restore-ApprovalFromTranscript $hookInput $state
    }

    if ($null -eq $state['manifest']) {
        $state = Sync-RuleManifest $state
        Write-SessionState $sessionId $state
        return
    }

    $changes = @(Compare-RuleManifest $state['manifest'])
    if ($changes.Count -eq 0) {
        Write-SessionState $sessionId $state
        return
    }

    $configuration = Read-RuleSourceConfiguration
    $ruleBypass = $null -ne $state['activeApproval'] -and
        [bool]$state['activeApproval']['ruleMaintenanceBypass'] -and
        (Test-AllChangesAreRuleDocuments $changes $configuration)
    $withinApproval = Test-ChangesWithinApproval $changes $state['activeApproval']

    if ($ruleBypass -or $withinApproval) {
        $state = Sync-RuleManifest $state
        if (-not $ruleBypass) {
            $state['restartRequired'] = $true
        }
        Write-SessionState $sessionId $state
        if (-not $ruleBypass) {
            Write-HookJson @{
                hookSpecificOutput = @{
                    hookEventName = 'PostToolUse'
                    additionalContext = '승인된 하네스 변경을 기록했습니다. 현재 유지보수 작업을 마친 뒤 새 세션에서 훅을 검토하고 다시 신뢰해야 합니다.'
                }
            }
        }
        return
    }

    Write-SessionState $sessionId $state
    $paths = @($changes | ForEach-Object { [string]$_['path'] }) -join ', '
    Write-HookValidationFailure $hookInput ('도구 실행 뒤 승인 범위 밖의 규칙 또는 하네스 변경을 감지했습니다. 이미 발생한 부작용은 자동 복구하지 않습니다: ' + $paths)
}
catch {
    $fallbackInput = @{}
    try {
        $fallbackInput = Read-HookInput $HookInputJson
    }
    catch {
        $fallbackInput = @{ hook_event_name = 'PostToolUse'; session_id = '' }
    }
    $fallbackInput['hook_event_name'] = 'PostToolUse'
    Write-HookValidationFailure $fallbackInput ('도구 결과 규칙 감사에 실패했습니다: ' + $_.Exception.Message)
}
