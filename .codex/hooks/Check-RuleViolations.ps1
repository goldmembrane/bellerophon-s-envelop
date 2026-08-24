param(
    [string]$HookInputJson,
    [string]$HookDirectory
)

try {
    $hookInput = Read-HookInput $HookInputJson
    $sessionId = [string](Get-HookValue $hookInput 'session_id' '')
    $turnId = [string](Get-HookValue $hookInput 'turn_id' 'noturn')
    $stopHookActive = [bool](Get-HookValue $hookInput 'stop_hook_active' $false)
    $lastMessage = [string](Get-HookValue $hookInput 'last_assistant_message' '')
    $state = Read-SessionState $sessionId

    if ($null -eq $state['manifest']) {
        $state = Sync-RuleManifest $state
    }

    $approvalIndex = $lastMessage.LastIndexOf('작업 승인 요청', [StringComparison]::Ordinal)
    if ($approvalIndex -ge 0) {
        $candidate = $lastMessage.Substring($approvalIndex).Trim()
        $parsedApproval = Parse-ApprovalRequest $candidate ([string]$state['lastInstructionPrompt'])
        if (-not [bool]$parsedApproval['valid']) {
            $state['pendingApproval'] = $null
            Write-SessionState $sessionId $state
            $approvalErrors = @($parsedApproval['errors'])
            if (@($approvalErrors | Where-Object { ([string]$_).StartsWith('[MODEL_ANIMATION_CLARIFICATION_REQUIRED]', [StringComparison]::Ordinal) }).Count -gt 0) {
                Write-HookJson @{
                    decision = 'block'
                    reason = '[RULE_HOOK_CLARIFY] 모델링·애니메이션 수정에 모호하거나 모르는 부분이 남아 있습니다. 승인 요청을 다시 작성하지 말고 사용자에게 구체적으로 질문한 뒤 답을 받아 계속하십시오.'
                }
                return
            }
            if (@($approvalErrors | Where-Object { ([string]$_).StartsWith('[MODEL_ANIMATION_DIRECT_VALIDATION_REQUIRED]', [StringComparison]::Ordinal) }).Count -gt 0) {
                Write-HookJson @{
                    decision = 'block'
                    reason = '[RULE_HOOK_DIRECT_CHECK_FIRST] 모델링·애니메이션 검증 순서를 1순위 직접 확인, 2순위 수치·스크립트 보조 검증으로 고친 뒤 같은 작업 범위의 승인 요청을 다시 작성하십시오.'
                }
                return
            }
            Write-HookValidationFailure $hookInput ('승인 요청 형식 검증 실패: ' + (@($parsedApproval['errors']) -join ' '))
            return
        }

        $state['pendingApproval'] = $parsedApproval
        $state['selfCheckedTurns'][$turnId] = $true
        Write-SessionState $sessionId $state
        return
    }

    if ($null -ne $state['pendingFailureReport']) {
        $failure = $state['pendingFailureReport']
        $reported = $lastMessage -match '(훅\s*)?검증\s*실패|규칙\s*검증\s*실패|작업을\s*중단'
        if (-not $reported -and -not [bool]$failure['reported']) {
            $failure['reported'] = $true
            $state['pendingFailureReport'] = $failure
            Write-SessionState $sessionId $state
            Write-HookJson @{
                decision = 'block'
                reason = ('[RULE_HOOK_REPORT] 먼저 사용자에게 훅 검증 실패를 보고하십시오. 추가 승인을 요청하지 말고 기존 승인 범위 안에서 다시 검증한 뒤 작업을 계속하십시오. 사유: ' + [string]$failure['reason'])
            }
            return
        }
        if ($reported) {
            $state['pendingFailureReport'] = $null
        }
    }

    $responseErrors = @()
    if (-not [string]::IsNullOrWhiteSpace($lastMessage) -and $lastMessage -notmatch '[가-힣]') {
        $responseErrors += '최종 응답은 한국어 존댓말이어야 합니다.'
    }
    $directPrompt = if ($null -ne $state['activeApproval']) {
        [string]$state['activeApproval']['directUserPrompt']
    }
    else {
        [string]$state['lastInstructionPrompt']
    }

    $isModelAnimationApproval = $null -ne $state['activeApproval'] -and
        $state['activeApproval'].ContainsKey('modelingAnimationModification') -and
        [bool]$state['activeApproval']['modelingAnimationModification']
    if ($isModelAnimationApproval) {
        $claimsCompletion = $lastMessage -match '(?is)(작업|수정|모델링|모델|애니메이션|모션|제거|교체|적용|검증).{0,60}(완료|통과|성공|끝났)' -or
            $lastMessage -match '(?is)(완료|통과|성공).{0,60}(모델링|모델|애니메이션|모션|수정|제거|교체|적용|검증)'
        $reportsIncomplete = $lastMessage -match '(?is)(미완료|실패|중단|대기|완료하지\s*못|완료되지\s*않|확인하지\s*못)'
        if ($claimsCompletion -and -not $reportsIncomplete) {
            $directEvidence = [regex]::Match($lastMessage, '(?i)(직접\s*(?:확인|검증|보았|살펴)|시각\s*(?:확인|검증)|육안\s*(?:확인|검증)|모델(?:링)?을\s*직접|애니메이션을\s*직접)')
            $supportEvidence = [regex]::Match($lastMessage, '(?i)(수치|정점|삼각형|프레임\s*수|로그|스크립트|자동\s*검증|테스트)')
            if (-not $directEvidence.Success) {
                $responseErrors += '모델링·애니메이션 완료 판정 전에 실제 결과를 직접 확인한 근거를 먼저 보고해야 합니다.'
            }
            elseif ($supportEvidence.Success -and $supportEvidence.Index -lt $directEvidence.Index) {
                $responseErrors += '모델링·애니메이션 완료 응답은 직접 확인을 1순위로 먼저 보고하고 수치·스크립트는 뒤의 보조 근거로만 보고해야 합니다.'
            }
        }
    }

    foreach ($fileName in @(
        'Run-HarnessValidation.ps1',
        'Run-EditModeTests.ps1',
        'Run-PlayModeTests.ps1',
        'Build-WindowsDev.ps1'
    )) {
        if ($lastMessage.IndexOf($fileName, [StringComparison]::OrdinalIgnoreCase) -ge 0 -and
            $directPrompt.IndexOf($fileName, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
            $responseErrors += ($fileName + '을 선제적인 검증·권장·후속 단계에 포함할 수 없습니다.')
        }
    }

    if ($responseErrors.Count -gt 0) {
        Write-SessionState $sessionId $state
        Write-HookValidationFailure $hookInput ('최종 응답 규칙 검증 실패: ' + ($responseErrors -join ' '))
        return
    }

    if (-not $stopHookActive -and -not $state['selfCheckedTurns'].ContainsKey($turnId)) {
        $state['selfCheckedTurns'][$turnId] = $true
        Write-SessionState $sessionId $state
        Write-HookJson @{
            decision = 'block'
            reason = '[RULE_HOOK_SELF_CHECK] 현재 응답과 수행 내역을 활성 규칙 및 기존 승인 범위와 한 번 더 대조하십시오. 새 작업이나 승인 범위를 추가하지 말고, 문제가 없으면 같은 범위 안에서 최종 응답을 다시 제출하십시오.'
        }
        return
    }

    Write-SessionState $sessionId $state
}
catch {
    $fallbackInput = @{}
    try {
        $fallbackInput = Read-HookInput $HookInputJson
    }
    catch {
        $fallbackInput = @{ hook_event_name = 'Stop'; session_id = '' }
    }
    Write-HookValidationFailure $fallbackInput ('응답 종료 규칙 검증에 실패했습니다: ' + $_.Exception.Message)
}
