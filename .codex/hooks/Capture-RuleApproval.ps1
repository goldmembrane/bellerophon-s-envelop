param(
    [string]$HookInputJson,
    [string]$HookDirectory
)

try {
    $hookInput = Read-HookInput $HookInputJson
    $sessionId = [string](Get-HookValue $hookInput 'session_id' '')
    $prompt = [string](Get-HookValue $hookInput 'prompt' '')
    $state = Read-SessionState $sessionId
    $state['lastUserPrompt'] = $prompt

    $isInternalContinuation = $prompt -match '^\[RULE_HOOK_(?:RETRY|STOP|SELF_CHECK|REPORT)\]'
    if (-not $isInternalContinuation -and -not (Test-IsApprovalResponse $prompt)) {
        $state['lastInstructionPrompt'] = $prompt
    }

    $activationMessage = ''
    if (Test-IsApprovalResponse $prompt) {
        if ($null -eq $state['pendingApproval']) {
            $state = Restore-ApprovalFromTranscript $hookInput $state
        }
        if ($null -ne $state['pendingApproval'] -and [bool]$state['pendingApproval']['valid']) {
            $state['activeApproval'] = New-ActiveApproval $state['pendingApproval']
            $state['pendingFailureReport'] = $null
            $activationMessage = '직전의 유효한 승인 요청 범위를 현재 작업 승인 토큰으로 활성화했습니다.'
        }
        else {
            $activationMessage = '연결할 유효한 직전 승인 요청이 없습니다. 도구를 사용하기 전에 새 승인 요청을 작성해야 합니다.'
        }
    }

    $contextParts = @(
        '사용자 프롬프트는 훅에서 차단하지 않았습니다.',
        '응답과 도구 호출 전에 활성 규칙, 승인 토큰, 승인된 경로와 명령을 확인하십시오.',
        '승인 요청에는 필요한 읽기 범위, 수정 범위, 도구와 명령을 식별할 수 있도록 각 관련 섹션에 백틱 항목을 명시하십시오.'
    )
    if (-not [string]::IsNullOrWhiteSpace($activationMessage)) {
        $contextParts += $activationMessage
    }
    if (-not [bool]$state['rulesInjected']) {
        $state = Sync-RuleManifest $state
        $state['rulesInjected'] = $true
        $contextParts += Get-RuleContext
    }

    Write-SessionState $sessionId $state
    Write-HookJson @{
        hookSpecificOutput = @{
            hookEventName = 'UserPromptSubmit'
            additionalContext = ($contextParts -join [Environment]::NewLine)
        }
    }
}
catch {
    $fallbackInput = @{}
    try {
        $fallbackInput = Read-HookInput $HookInputJson
    }
    catch {
        $fallbackInput = @{ hook_event_name = 'UserPromptSubmit'; session_id = '' }
    }
    $fallbackInput['hook_event_name'] = 'UserPromptSubmit'
    Write-HookValidationFailure $fallbackInput ('사용자 프롬프트 규칙 컨텍스트 처리에 실패했습니다: ' + $_.Exception.Message)
}
