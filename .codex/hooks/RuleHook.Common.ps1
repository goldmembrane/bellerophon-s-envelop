$script:HookDirectory = [IO.Path]::GetFullPath($HookDirectory)
$script:CodexDirectory = Split-Path -Parent $script:HookDirectory
$script:RepositoryRoot = Split-Path -Parent $script:CodexDirectory
$script:StateDirectory = Join-Path $script:CodexDirectory 'state'
$script:Utf8NoBom = New-Object Text.UTF8Encoding($false)

function ConvertTo-RuleHashtable {
    param($InputObject)

    if ($null -eq $InputObject) {
        return $null
    }
    if ($InputObject -is [Collections.IDictionary]) {
        $table = @{}
        foreach ($key in $InputObject.Keys) {
            $table[[string]$key] = ConvertTo-RuleHashtable $InputObject[$key]
        }
        return $table
    }
    if ($InputObject -is [Management.Automation.PSCustomObject]) {
        $table = @{}
        foreach ($property in $InputObject.PSObject.Properties) {
            $table[$property.Name] = ConvertTo-RuleHashtable $property.Value
        }
        return $table
    }
    if (($InputObject -is [Collections.IEnumerable]) -and -not ($InputObject -is [string])) {
        $items = @()
        foreach ($item in $InputObject) {
            $items += ,(ConvertTo-RuleHashtable $item)
        }
        return $items
    }
    return $InputObject
}

function Read-HookInput {
    param([string]$Json)

    if ([string]::IsNullOrWhiteSpace($Json)) {
        return @{}
    }
    return ConvertTo-RuleHashtable ($Json | ConvertFrom-Json)
}

function Write-HookJson {
    param([hashtable]$Value)

    [Console]::Out.WriteLine(($Value | ConvertTo-Json -Depth 40 -Compress))
}

function Get-HookValue {
    param(
        [hashtable]$InputObject,
        [string]$Name,
        $DefaultValue = $null
    )

    if ($null -ne $InputObject -and $InputObject.ContainsKey($Name)) {
        return $InputObject[$Name]
    }
    return $DefaultValue
}

function Get-SafeSessionId {
    param([string]$SessionId)

    if ([string]::IsNullOrWhiteSpace($SessionId)) {
        return 'nosession'
    }
    return ($SessionId -replace '[^A-Za-z0-9._-]', '_')
}

function Get-SessionStatePath {
    param([string]$SessionId)

    [void][IO.Directory]::CreateDirectory($script:StateDirectory)
    return Join-Path $script:StateDirectory ('session-' + (Get-SafeSessionId $SessionId) + '.json')
}

function New-DefaultSessionState {
    param([string]$SessionId)

    return @{
        version = 2
        sessionId = $SessionId
        createdAtUtc = [DateTime]::UtcNow.ToString('o')
        lastUserPrompt = ''
        lastInstructionPrompt = ''
        pendingApproval = $null
        activeApproval = $null
        manifest = $null
        rulesInjected = $false
        restartRequired = $false
        pendingFailureReport = $null
        selfCheckedTurns = @{}
    }
}

function Read-SessionState {
    param([string]$SessionId)

    $defaults = New-DefaultSessionState $SessionId
    $path = Get-SessionStatePath $SessionId
    if (-not [IO.File]::Exists($path)) {
        return $defaults
    }

    try {
        $raw = [IO.File]::ReadAllText($path, $script:Utf8NoBom)
        $state = ConvertTo-RuleHashtable ($raw | ConvertFrom-Json)
        foreach ($key in $defaults.Keys) {
            if (-not $state.ContainsKey($key)) {
                $state[$key] = $defaults[$key]
            }
        }
        return $state
    }
    catch {
        return $defaults
    }
}

function Write-SessionState {
    param(
        [string]$SessionId,
        [hashtable]$State
    )

    $path = Get-SessionStatePath $SessionId
    $temporaryPath = $path + '.' + [Guid]::NewGuid().ToString('N') + '.tmp'
    $State['updatedAtUtc'] = [DateTime]::UtcNow.ToString('o')
    [IO.File]::WriteAllText(
        $temporaryPath,
        ($State | ConvertTo-Json -Depth 40 -Compress),
        $script:Utf8NoBom
    )
    Move-Item -LiteralPath $temporaryPath -Destination $path -Force
}

function Get-Sha256Text {
    param([string]$Text)

    $hasher = [Security.Cryptography.SHA256]::Create()
    try {
        return [BitConverter]::ToString(
            $hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes($Text))
        ).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $hasher.Dispose()
    }
}

function Get-Sha256File {
    param([string]$Path)

    $hasher = [Security.Cryptography.SHA256]::Create()
    $stream = [IO.File]::OpenRead($Path)
    try {
        return [BitConverter]::ToString($hasher.ComputeHash($stream)).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $stream.Dispose()
        $hasher.Dispose()
    }
}

function Get-NormalizedRelativePath {
    param([string]$Path)

    $fullPath = [IO.Path]::GetFullPath($Path)
    $rootWithSeparator = $script:RepositoryRoot.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    if ($fullPath.StartsWith($rootWithSeparator, [StringComparison]::OrdinalIgnoreCase)) {
        return $fullPath.Substring($rootWithSeparator.Length).Replace('\', '/')
    }
    if ($fullPath.Equals($script:RepositoryRoot, [StringComparison]::OrdinalIgnoreCase)) {
        return ''
    }
    return $fullPath.Replace('\', '/')
}

function Read-RuleSourceConfiguration {
    $path = Join-Path $script:CodexDirectory 'rule-sources.json'
    $raw = [IO.File]::ReadAllText($path, $script:Utf8NoBom)
    return ConvertTo-RuleHashtable ($raw | ConvertFrom-Json)
}

function Test-ExcludedRulePath {
    param(
        [string]$RelativePath,
        [hashtable]$Configuration
    )

    $normalized = $RelativePath.Replace('\', '/').TrimStart('/')
    foreach ($prefix in @($Configuration['excludedPathPrefixes'])) {
        $candidate = ([string]$prefix).Replace('\', '/').TrimStart('/')
        if ($normalized.Equals($candidate.TrimEnd('/'), [StringComparison]::OrdinalIgnoreCase) -or
            $normalized.StartsWith($candidate, [StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }
    return $false
}

function Get-RepositoryFilesForRules {
    param([hashtable]$Configuration)

    $result = New-Object 'Collections.Generic.List[string]'
    $stack = New-Object Collections.Stack
    $stack.Push($script:RepositoryRoot)

    while ($stack.Count -gt 0) {
        $directory = [string]$stack.Pop()
        try {
            foreach ($childDirectory in [IO.Directory]::EnumerateDirectories($directory)) {
                $relativeDirectory = Get-NormalizedRelativePath $childDirectory
                if (-not (Test-ExcludedRulePath ($relativeDirectory + '/') $Configuration)) {
                    $stack.Push($childDirectory)
                }
            }
            foreach ($file in [IO.Directory]::EnumerateFiles($directory)) {
                $relativeFile = Get-NormalizedRelativePath $file
                if (-not (Test-ExcludedRulePath $relativeFile $Configuration)) {
                    $result.Add($relativeFile)
                }
            }
        }
        catch {
        }
    }

    return $result.ToArray()
}

function Test-IsRuleDocumentPath {
    param(
        [string]$RelativePath,
        [hashtable]$Configuration = $null
    )

    if ($null -eq $Configuration) {
        $Configuration = Read-RuleSourceConfiguration
    }
    $normalized = $RelativePath.Replace('\', '/').TrimStart('/')
    foreach ($pattern in @($Configuration['rulePathRegexes'])) {
        if ($normalized -match [string]$pattern) {
            return $true
        }
    }
    return $false
}

function Get-RuleDocumentPaths {
    param([hashtable]$Configuration)

    $paths = @{}
    foreach ($required in @($Configuration['requiredRuleDocuments'])) {
        $relative = ([string]$required).Replace('\', '/')
        $full = Join-Path $script:RepositoryRoot $relative
        if ([IO.File]::Exists($full)) {
            $paths[$relative.ToLowerInvariant()] = $relative
        }
    }

    foreach ($relative in Get-RepositoryFilesForRules $Configuration) {
        if (Test-IsRuleDocumentPath $relative $Configuration) {
            $paths[$relative.ToLowerInvariant()] = $relative
        }
    }
    return @($paths.Values | Sort-Object)
}

function Get-ProtectedPaths {
    param([hashtable]$Configuration)

    $paths = @{}
    foreach ($relative in Get-RuleDocumentPaths $Configuration) {
        $paths[$relative.ToLowerInvariant()] = $relative
    }
    foreach ($relativeValue in @($Configuration['protectedFiles'])) {
        $relative = ([string]$relativeValue).Replace('\', '/')
        $full = Join-Path $script:RepositoryRoot $relative
        if ([IO.File]::Exists($full)) {
            $paths[$relative.ToLowerInvariant()] = $relative
        }
    }
    foreach ($prefixValue in @($Configuration['protectedDirectoryPrefixes'])) {
        $prefix = ([string]$prefixValue).Replace('\', '/').TrimEnd('/') + '/'
        $directory = Join-Path $script:RepositoryRoot $prefix
        if ([IO.Directory]::Exists($directory)) {
            foreach ($file in [IO.Directory]::EnumerateFiles($directory, '*', [IO.SearchOption]::AllDirectories)) {
                $relative = Get-NormalizedRelativePath $file
                $paths[$relative.ToLowerInvariant()] = $relative
            }
        }
    }
    return @($paths.Values | Sort-Object)
}

function New-RuleManifest {
    $configuration = Read-RuleSourceConfiguration
    $entries = @{}
    foreach ($relative in Get-ProtectedPaths $configuration) {
        $full = Join-Path $script:RepositoryRoot $relative
        if ([IO.File]::Exists($full)) {
            $entries[$relative] = Get-Sha256File $full
        }
    }
    return @{
        createdAtUtc = [DateTime]::UtcNow.ToString('o')
        entries = $entries
    }
}

function Compare-RuleManifest {
    param([hashtable]$Manifest)

    if ($null -eq $Manifest -or -not $Manifest.ContainsKey('entries')) {
        return @()
    }
    $current = New-RuleManifest
    $expectedEntries = $Manifest['entries']
    $currentEntries = $current['entries']
    $allPaths = @{}
    foreach ($path in $expectedEntries.Keys) {
        $allPaths[[string]$path] = $true
    }
    foreach ($path in $currentEntries.Keys) {
        $allPaths[[string]$path] = $true
    }

    $changes = @()
    foreach ($path in $allPaths.Keys) {
        $expectedHash = if ($expectedEntries.ContainsKey($path)) { [string]$expectedEntries[$path] } else { '' }
        $currentHash = if ($currentEntries.ContainsKey($path)) { [string]$currentEntries[$path] } else { '' }
        if ($expectedHash -ne $currentHash) {
            $changes += @{
                path = [string]$path
                before = $expectedHash
                after = $currentHash
            }
        }
    }
    return $changes
}

function Sync-RuleManifest {
    param([hashtable]$State)

    $State['manifest'] = New-RuleManifest
    return $State
}

function Get-RuleContext {
    $configuration = Read-RuleSourceConfiguration
    $builder = New-Object Text.StringBuilder
    [void]$builder.AppendLine('Bellerophon active repository rules follow. Apply path-scoped AGENTS files only to their directory trees, with the nearest file taking precedence.')
    foreach ($relative in Get-RuleDocumentPaths $configuration) {
        $full = Join-Path $script:RepositoryRoot $relative
        $scope = 'repository-wide or document-defined scope'
        if ($relative -match '(^|/)AGENTS(?:\.override)?\.md$' -and $relative.Contains('/')) {
            $scope = (Split-Path -Parent $relative).Replace('\', '/') + '/**'
        }
        [void]$builder.AppendLine('')
        [void]$builder.AppendLine(('===== RULE DOCUMENT: ' + $relative + ' | applies to: ' + $scope + ' ====='))
        [void]$builder.AppendLine([IO.File]::ReadAllText($full, $script:Utf8NoBom))
    }
    return $builder.ToString()
}

function Get-CodeItems {
    param([string]$SectionText)

    $tick = [string][char]96
    $pattern = [regex]::Escape($tick) + '([^' + [regex]::Escape($tick) + '\r\n]+)' + [regex]::Escape($tick)
    $items = @()
    foreach ($match in [regex]::Matches($SectionText, $pattern)) {
        $value = $match.Groups[1].Value.Trim()
        if (-not [string]::IsNullOrWhiteSpace($value)) {
            $items += $value
        }
    }
    return $items
}

function Test-IsRuleHarnessMaintenanceScope {
    param([string]$Scope)

    $normalized = $Scope.Trim().Trim('"', "'").Replace('\', '/').TrimStart('./')
    return $normalized -match '(?i)(^|/)AGENTS(?:\.override)?\.md$|_RULES\.md$|(^|/)RULES\.md$|^\.codex/(?:hooks(?:/|$)|hooks\.json$|rule-sources\.json$|config\.toml$)|^docs/HARNESS\.md$'
}

function Test-IsModelingAnimationModificationApproval {
    param([hashtable]$Approval)

    $modifyScopes = @($Approval['modifyScopes'])
    if ($modifyScopes.Count -eq 0) {
        return $false
    }

    $hasNonMaintenanceScope = $false
    foreach ($scope in $modifyScopes) {
        if (-not (Test-IsRuleHarnessMaintenanceScope ([string]$scope))) {
            $hasNonMaintenanceScope = $true
            break
        }
    }
    if (-not $hasNonMaintenanceScope) {
        return $false
    }

    $scopeText = (@($modifyScopes | ForEach-Object { ([string]$_).Replace('\', '/') }) -join [Environment]::NewLine)
    $requestText = @(
        [string]$Approval['directUserPrompt'],
        [string]$Approval['requestText'],
        $scopeText
    ) -join [Environment]::NewLine

    $modelAnimationPathPattern = '(?i)(^|/)(?:Models?|Animations?|Animation|Controllers?|Rigging)(/|$)|\.(?:fbx|blend|obj|dae|gltf|glb|anim|controller|overridecontroller)$'
    $modelAnimationTextPattern = '(?i)모델링|모델\s*(?:수정|교체|형상|메시)|메시(?:\s|$)|mesh|fbx|blendshape|리깅|스킨\s*가중치|애니메이션|모션\s*(?:수정|교체|제작)|animator|animation'
    return $scopeText -match $modelAnimationPathPattern -or $requestText -match $modelAnimationTextPattern
}

function Get-CommandScopeExecutableToken {
    param([string]$Scope)

    $normalized = ($Scope -replace '\s+', ' ').Trim()
    if ([string]::IsNullOrWhiteSpace($normalized)) {
        return ''
    }
    $match = [regex]::Match($normalized, '^(?:"[^"]*"|''[^'']*''|\S+)')
    if (-not $match.Success) {
        return ''
    }
    return $match.Value.Trim('"', "'")
}

function Get-CommandScopeWildcardError {
    param([string]$Scope)

    $normalized = ($Scope -replace '\s+', ' ').Trim()
    if ($normalized.IndexOf('*', [StringComparison]::Ordinal) -lt 0 -and
        $normalized.IndexOf('?', [StringComparison]::Ordinal) -lt 0) {
        return ''
    }
    if ($normalized.Equals('*', [StringComparison]::Ordinal)) {
        return '단독 * 명령 범위는 모든 명령을 허용하므로 사용할 수 없습니다.'
    }
    $executableToken = Get-CommandScopeExecutableToken $normalized
    if ([string]::IsNullOrWhiteSpace($executableToken) -or $executableToken -match '[*?]') {
        return '실행 파일 토큰에는 * 또는 ? 와일드카드를 사용할 수 없습니다.'
    }
    return ''
}

function Convert-CommandScopeToRegex {
    param([string]$Scope)

    $normalized = ($Scope -replace '\s+', ' ').Trim()
    $pattern = [regex]::Escape($normalized)
    $pattern = $pattern.Replace('\*', '.*').Replace('\?', '.')
    return '^' + $pattern + '$'
}

function Parse-ApprovalRequest {
    param(
        [string]$Text,
        [string]$DirectUserPrompt = ''
    )

    $result = @{
        valid = $false
        errors = @()
        requestText = $Text
        directUserPrompt = $DirectUserPrompt
        sections = @{}
        readScopes = @()
        modifyScopes = @()
        commandScopes = @()
        ruleMaintenanceBypass = $false
        modelingAnimationModification = $false
        ambiguityResolved = $false
        directValidationFirst = $false
    }
    if ([string]::IsNullOrWhiteSpace($Text) -or $Text -notmatch '(?m)^\s*작업 승인 요청\s*$') {
        $result['errors'] += '승인 요청 제목이 없습니다.'
        return $result
    }

    $expectedHeaders = @(
        '작업 목표',
        '읽을 파일/범위',
        '수정할 파일/범위',
        '실행할 명령',
        '검증 범위',
        '실행하지 않을 항목'
    )
    $headerPattern = '(?m)^(' + (($expectedHeaders | ForEach-Object { [regex]::Escape($_) }) -join '|') + '):\s*$'
    $matches = [regex]::Matches($Text, $headerPattern)
    if ($matches.Count -ne $expectedHeaders.Count) {
        $result['errors'] += '승인 요청은 여섯 섹션을 각각 한 번씩 포함해야 합니다.'
        return $result
    }

    for ($index = 0; $index -lt $expectedHeaders.Count; $index++) {
        if ($matches[$index].Groups[1].Value -ne $expectedHeaders[$index]) {
            $result['errors'] += '승인 요청 섹션 순서가 규칙과 다릅니다.'
            return $result
        }
        $contentStart = $matches[$index].Index + $matches[$index].Length
        $contentEnd = if ($index + 1 -lt $matches.Count) { $matches[$index + 1].Index } else { $Text.Length }
        $between = $Text.Substring($contentStart, $contentEnd - $contentStart)
        $result['sections'][$expectedHeaders[$index]] = $between.Trim()
    }

    foreach ($name in @('읽을 파일/범위', '수정할 파일/범위', '실행할 명령')) {
        $section = [string]$result['sections'][$name]
        if ($section -notmatch '^\s*(?:-\s*)?없음\s*$') {
            $items = @(Get-CodeItems $section)
            if ($items.Count -eq 0) {
                $result['errors'] += ($name + ' 섹션에는 승인 범위를 식별할 수 있는 백틱 경로나 명령이 최소 하나 필요합니다.')
            }
        }
    }

    $result['readScopes'] = @(Get-CodeItems ([string]$result['sections']['읽을 파일/범위']))
    $result['modifyScopes'] = @(Get-CodeItems ([string]$result['sections']['수정할 파일/범위']))
    $result['commandScopes'] = @(Get-CodeItems ([string]$result['sections']['실행할 명령']))

    foreach ($commandScope in $result['commandScopes']) {
        $wildcardError = Get-CommandScopeWildcardError ([string]$commandScope)
        if (-not [string]::IsNullOrWhiteSpace($wildcardError)) {
            $result['errors'] += ('실행할 명령의 와일드카드 범위가 안전하지 않습니다: ' + $wildcardError)
        }
    }

    if ($result['modifyScopes'].Count -gt 0) {
        $hasEditTool = $false
        foreach ($commandScope in $result['commandScopes']) {
            if ([string]$commandScope -match '(?i)(^|[^a-z])(apply_patch|edit|write)([^a-z]|$)') {
                $hasEditTool = $true
                break
            }
        }
        if (-not $hasEditTool) {
            $result['errors'] += '수정 범위가 있으므로 실행할 명령에 수정 도구를 명시해야 합니다.'
        }
    }

    if ([string]$result['sections']['실행하지 않을 항목'] -notmatch 'Run-HarnessValidation\.ps1') {
        $result['errors'] += '실행하지 않을 항목에 Run-HarnessValidation.ps1 및 Harness 검증 관련 작업을 명시해야 합니다.'
    }

    $instructionMentionsRuleEdit =
        $DirectUserPrompt -match '(?is)(AGENTS(?:\.override)?\.md|규칙\s*문서|_RULES\.md|docs/.+RULES\.md).{0,120}(추가|수정|변경|편집)' -or
        $DirectUserPrompt -match '(?is)(추가|수정|변경|편집).{0,120}(AGENTS(?:\.override)?\.md|규칙\s*문서|_RULES\.md|docs/.+RULES\.md)'
    $modifyMentionsRuleDocument = $false
    foreach ($scope in $result['modifyScopes']) {
        if ([string]$scope -match '(?i)(^|[\\/])AGENTS(?:\.override)?\.md$|_RULES\.md$|[\\/]RULES\.md$|^\.codex[\\/]rules[\\/]') {
            $modifyMentionsRuleDocument = $true
            break
        }
    }
    $result['ruleMaintenanceBypass'] = $instructionMentionsRuleEdit -and $modifyMentionsRuleDocument

    $result['modelingAnimationModification'] = Test-IsModelingAnimationModificationApproval $result
    if ([bool]$result['modelingAnimationModification']) {
        $validationSection = [string]$result['sections']['검증 범위']
        $ambiguityMatch = [regex]::Match(
            $validationSection,
            '(?im)^\s*(?:-\s*)?모호성\s*확인\s*:\s*(?<status>[^\r\n]+?)\s*$'
        )
        if (-not $ambiguityMatch.Success) {
            $result['errors'] += '[MODEL_ANIMATION_CLARIFICATION_REQUIRED] 모델링·애니메이션 수정 승인 요청에는 모호성 확인을 명시해야 합니다. 모르는 부분이 있으면 승인 요청을 작성하지 말고 먼저 사용자에게 질문하십시오.'
        }
        else {
            $ambiguityStatus = $ambiguityMatch.Groups['status'].Value.Trim()
            $resolvedPattern = '^(?:없음|사용자(?:의)?\s*(?:답변|확인|지정|설명)(?:으로|을\s*통해)?\s*(?:해소|확정|확인)(?:됨)?)(?:\s*[\(\[\-–—:].*)?$'
            if ($ambiguityStatus -notmatch $resolvedPattern) {
                $result['errors'] += '[MODEL_ANIMATION_CLARIFICATION_REQUIRED] 모호성이 남아 있습니다. 승인 요청을 작성하지 말고 사용자에게 질문해 답을 받은 뒤 모호성 확인을 갱신하십시오.'
            }
            else {
                $result['ambiguityResolved'] = $true
            }
        }

        $priorityMatch = [regex]::Match(
            $validationSection,
            '(?im)^\s*(?:-\s*)?검증\s*우선순위\s*:\s*(?<priority>[^\r\n]+?)\s*$'
        )
        if ($priorityMatch.Success) {
            $priorityText = $priorityMatch.Groups['priority'].Value
            $directMarker = [regex]::Match($priorityText, '(?i)1\s*순위.{0,160}(?:직접|육안|시각).{0,160}(?:모델링|모델|애니메이션|모션|결과)')
            $supportMarker = [regex]::Match($priorityText, '(?i)2\s*순위.{0,160}(?:수치|스크립트|로그|자동)')
            if ($directMarker.Success -and $supportMarker.Success -and $directMarker.Index -lt $supportMarker.Index) {
                $result['directValidationFirst'] = $true
            }
        }
        if (-not [bool]$result['directValidationFirst']) {
            $result['errors'] += '[MODEL_ANIMATION_DIRECT_VALIDATION_REQUIRED] 검증 우선순위를 1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증 순서로 명시해야 합니다.'
        }
    }

    $result['valid'] = $result['errors'].Count -eq 0
    return $result
}

function Test-IsApprovalResponse {
    param([string]$Prompt)

    if ([string]::IsNullOrWhiteSpace($Prompt) -or $Prompt.Length -gt 100) {
        return $false
    }
    return $Prompt.Trim() -match '^(진행해|승인할게)[.!。]?$'
}

function New-ActiveApproval {
    param([hashtable]$ParsedApproval)

    return @{
        id = [Guid]::NewGuid().ToString('N')
        approvedAtUtc = [DateTime]::UtcNow.ToString('o')
        requestText = $ParsedApproval['requestText']
        directUserPrompt = $ParsedApproval['directUserPrompt']
        readScopes = @($ParsedApproval['readScopes'])
        modifyScopes = @($ParsedApproval['modifyScopes'])
        commandScopes = @($ParsedApproval['commandScopes'])
        ruleMaintenanceBypass = [bool]$ParsedApproval['ruleMaintenanceBypass']
        modelingAnimationModification = [bool]$ParsedApproval['modelingAnimationModification']
        ambiguityResolved = [bool]$ParsedApproval['ambiguityResolved']
        directValidationFirst = [bool]$ParsedApproval['directValidationFirst']
        started = $false
    }
}

function Add-DeepStrings {
    param(
        $Value,
        [Collections.Generic.List[string]]$List,
        [int]$Depth = 0
    )

    if ($null -eq $Value -or $Depth -gt 20) {
        return
    }
    if ($Value -is [string]) {
        if (-not [string]::IsNullOrWhiteSpace($Value)) {
            $List.Add($Value)
        }
        return
    }
    if ($Value -is [Collections.IDictionary]) {
        foreach ($key in $Value.Keys) {
            Add-DeepStrings $Value[$key] $List ($Depth + 1)
        }
        return
    }
    if ($Value -is [Management.Automation.PSCustomObject]) {
        foreach ($property in $Value.PSObject.Properties) {
            Add-DeepStrings $property.Value $List ($Depth + 1)
        }
        return
    }
    if (($Value -is [Collections.IEnumerable]) -and -not ($Value -is [string])) {
        foreach ($item in $Value) {
            Add-DeepStrings $item $List ($Depth + 1)
        }
    }
}

function Restore-ApprovalFromTranscript {
    param(
        [hashtable]$HookInput,
        [hashtable]$State
    )

    $transcriptPath = [string](Get-HookValue $HookInput 'transcript_path' '')
    if ([string]::IsNullOrWhiteSpace($transcriptPath) -or -not [IO.File]::Exists($transcriptPath)) {
        return $State
    }

    try {
        $lines = [IO.File]::ReadAllLines($transcriptPath, $script:Utf8NoBom)
        $start = [Math]::Max(0, $lines.Length - 1200)
        $strings = New-Object 'Collections.Generic.List[string]'
        for ($index = $start; $index -lt $lines.Length; $index++) {
            try {
                $parsedLine = $lines[$index] | ConvertFrom-Json
                Add-DeepStrings $parsedLine $strings
            }
            catch {
            }
        }

        $latestPending = $null
        foreach ($text in $strings) {
            $approvalIndex = $text.LastIndexOf('작업 승인 요청', [StringComparison]::Ordinal)
            if ($approvalIndex -ge 0) {
                $candidate = $text.Substring($approvalIndex).Trim()
                $parsedApproval = Parse-ApprovalRequest $candidate ([string]$State['lastInstructionPrompt'])
                if ($parsedApproval['valid']) {
                    $latestPending = $parsedApproval
                    $State['pendingApproval'] = $parsedApproval
                }
                else {
                    $latestPending = $null
                    $State['pendingApproval'] = $null
                }
            }
            if ((Test-IsApprovalResponse $text) -and $null -ne $latestPending) {
                $State['activeApproval'] = New-ActiveApproval $latestPending
                $State['pendingFailureReport'] = $null
            }
        }
    }
    catch {
    }
    return $State
}

function Convert-ScopeToRegex {
    param([string]$Scope)

    $normalized = $Scope.Trim().Trim('"', "'").Replace('\', '/')
    if ($normalized.StartsWith('./')) {
        $normalized = $normalized.Substring(2)
    }
    $pattern = [regex]::Escape($normalized)
    $pattern = $pattern.Replace('\*\*', '.*')
    $pattern = $pattern.Replace('\*', '[^/]*')
    $pattern = $pattern.Replace('\?', '.')
    return '^' + $pattern.TrimEnd('/') + '(?:/.*)?$'
}

function Test-PathWithinScopes {
    param(
        [string]$Path,
        [object[]]$Scopes
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $true
    }
    $candidate = $Path.Trim().Trim('"', "'").TrimEnd(',', ';', ')', ']').Replace('\', '/')
    if ($candidate.StartsWith('./')) {
        $candidate = $candidate.Substring(2)
    }
    if ([IO.Path]::IsPathRooted($candidate)) {
        try {
            $candidate = (Get-NormalizedRelativePath $candidate).Replace('\', '/')
        }
        catch {
        }
    }

    foreach ($scopeValue in @($Scopes)) {
        $scope = ([string]$scopeValue).Trim().Trim('"', "'").Replace('\', '/')
        if ($scope.StartsWith('./')) {
            $scope = $scope.Substring(2)
        }
        if ($candidate -match (Convert-ScopeToRegex $scope)) {
            return $true
        }
        $candidatePrefix = $candidate.TrimEnd('/') + '/'
        if ($scope.StartsWith($candidatePrefix, [StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }
    return $false
}

function Get-PatchPaths {
    param([string]$PatchText)

    $paths = @()
    foreach ($match in [regex]::Matches($PatchText, '(?m)^\*\*\* (?:Add|Update|Delete|Move to) File:\s*(.+?)\s*$')) {
        $paths += $match.Groups[1].Value.Trim()
    }
    return @($paths | Select-Object -Unique)
}

function Get-CommandPathCandidates {
    param([string]$Command)

    $paths = @()
    $patterns = @(
        '(?i)(?<path>[A-Z]:[\\/][^"''\r\n|;]+)',
        '(?i)(?<path>(?:\.{1,2}[\\/]|\.codex[\\/]|docs[\\/]|scripts[\\/]|AGENTS(?:\.override)?\.md)[^\s"''|;]*)'
    )
    foreach ($pattern in $patterns) {
        foreach ($match in [regex]::Matches($Command, $pattern)) {
            $rawCandidate = $match.Groups['path'].Value.Trim()
            $candidateParts = [regex]::Split(
                $rawCandidate,
                ',(?=(?:\.{1,2}[\\/]|\.codex[\\/]|docs[\\/]|scripts[\\/]|AGENTS(?:\.override)?\.md))'
            )
            foreach ($candidatePart in $candidateParts) {
                $candidate = $candidatePart.Trim().TrimEnd(',', ';', ')', ']')
                if (-not [string]::IsNullOrWhiteSpace($candidate)) {
                    $paths += $candidate
                }
            }
        }
    }
    return @($paths | Select-Object -Unique)
}

function Test-CommandScopeMatch {
    param(
        [string]$Command,
        [object[]]$CommandScopes
    )

    $normalizedCommand = ($Command -replace '\s+', ' ').Trim()
    foreach ($scopeValue in @($CommandScopes)) {
        $scope = (([string]$scopeValue) -replace '\s+', ' ').Trim()
        $hasWildcard = $scope.IndexOf('*', [StringComparison]::Ordinal) -ge 0 -or
            $scope.IndexOf('?', [StringComparison]::Ordinal) -ge 0
        if (-not $hasWildcard -and $normalizedCommand.Equals($scope, [StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
        if ($hasWildcard) {
            $wildcardError = Get-CommandScopeWildcardError $scope
            if (-not [string]::IsNullOrWhiteSpace($wildcardError)) {
                continue
            }
            $options = [Text.RegularExpressions.RegexOptions]::IgnoreCase -bor
                [Text.RegularExpressions.RegexOptions]::CultureInvariant
            if ([regex]::IsMatch($normalizedCommand, (Convert-CommandScopeToRegex $scope), $options)) {
                return $true
            }
        }
    }
    return $false
}

function Test-IsMutationCommand {
    param([string]$Command)

    return $Command -match '(?i)(apply_patch|Set-Content|Add-Content|Out-File|Remove-Item|Move-Item|Copy-Item|New-Item|git\s+(?:add|commit|push|mv|rm)|>\s*[^&])'
}

function Test-IsNotificationCommand {
    param([string]$Command)

    $normalized = ($Command -replace '\\', '/' -replace '\s+', ' ').Trim()
    return $normalized -match '(?i)^powershell -NoProfile -ExecutionPolicy Bypass -File \./\.scripts/Notify-Task(?:Completion|Paused)\.ps1$'
}

function Test-ProhibitedCommand {
    param(
        [string]$Command,
        [hashtable]$Approval
    )

    if ($Command -match '(?i)\bgit\s+reset\s+--hard\b') {
        return '복구 불가능한 git reset --hard 명령은 허용되지 않습니다.'
    }
    $isUnityProcessLaunch = $Command -match '(?i)\bUnity\.exe\b' -or
        $Command -match '(?i)\bStart-Process\b.*(?:Unity|\.unity(?:\s|$))'
    $isDirectUnitySceneInvocation = $Command -match '(?i)^\s*(?:&\s*)?(?:"[^"]+\.unity"|''[^'']+\.unity''|\S+\.unity)(?:\s|$)'
    if (($isUnityProcessLaunch -or $isDirectUnitySceneInvocation) -and
        $Command -notmatch '(?i)scripts[\\/]Open-UnityProject\.ps1') {
        return 'Unity는 승인된 프로젝트 실행 스크립트를 통해서만 열 수 있습니다.'
    }

    $directPrompt = if ($null -ne $Approval -and $Approval.ContainsKey('directUserPrompt')) {
        [string]$Approval['directUserPrompt']
    }
    else {
        ''
    }
    foreach ($fileName in @(
        'Run-HarnessValidation.ps1',
        'Run-EditModeTests.ps1',
        'Run-PlayModeTests.ps1',
        'Build-WindowsDev.ps1'
    )) {
        if ($Command.IndexOf($fileName, [StringComparison]::OrdinalIgnoreCase) -ge 0 -and
            $directPrompt.IndexOf($fileName, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
            return ($fileName + '은 사용자가 승인 요청 전에 명령명과 대상을 직접 지정하지 않아 실행할 수 없습니다.')
        }
    }
    return ''
}

function Test-ToolAgainstApproval {
    param(
        [hashtable]$HookInput,
        [hashtable]$Approval
    )

    $toolName = [string](Get-HookValue $HookInput 'tool_name' '')
    $toolInput = Get-HookValue $HookInput 'tool_input' @{}
    if ($null -eq $toolInput) {
        $toolInput = @{}
    }
    $command = if ($toolInput -is [Collections.IDictionary] -and $toolInput.ContainsKey('command')) {
        [string]$toolInput['command']
    }
    else {
        ''
    }

    if ($toolName -eq 'Bash' -and (Test-IsNotificationCommand $command)) {
        return @{ allowed = $true; reason = '' }
    }
    if ($null -eq $Approval) {
        return @{ allowed = $false; reason = '현재 작업에 연결된 승인 토큰이 없습니다.' }
    }

    if ($toolName -eq 'Bash') {
        $prohibitedReason = Test-ProhibitedCommand $command $Approval
        if (-not [string]::IsNullOrWhiteSpace($prohibitedReason)) {
            return @{ allowed = $false; reason = $prohibitedReason }
        }
        if (-not (Test-CommandScopeMatch $command @($Approval['commandScopes']))) {
            return @{ allowed = $false; reason = '실행하려는 명령이 승인된 실행 명령 목록에 없습니다.' }
        }

        $pathScopes = if (Test-IsMutationCommand $command) {
            @($Approval['modifyScopes'])
        }
        else {
            @($Approval['readScopes']) + @($Approval['modifyScopes'])
        }
        foreach ($path in Get-CommandPathCandidates $command) {
            if (-not (Test-PathWithinScopes $path $pathScopes)) {
                return @{ allowed = $false; reason = ('명령이 승인되지 않은 경로를 참조합니다: ' + $path) }
            }
        }
        return @{ allowed = $true; reason = '' }
    }

    if ($toolName -eq 'apply_patch') {
        $toolListed = $false
        foreach ($scope in @($Approval['commandScopes'])) {
            if ([string]$scope -match '(?i)^apply_patch(?:\s|$)') {
                $toolListed = $true
                break
            }
        }
        if (-not $toolListed) {
            return @{ allowed = $false; reason = 'apply_patch 도구가 승인 요청의 실행 목록에 없습니다.' }
        }
        $patchPaths = @(Get-PatchPaths $command)
        if ($patchPaths.Count -eq 0) {
            return @{ allowed = $false; reason = 'apply_patch 대상 경로를 확인할 수 없습니다.' }
        }
        foreach ($path in $patchPaths) {
            if ($path.Replace('\', '/').StartsWith('.codex/state/', [StringComparison]::OrdinalIgnoreCase)) {
                return @{ allowed = $false; reason = '.codex/state는 훅만 관리하며 직접 수정할 수 없습니다.' }
            }
            if (-not (Test-PathWithinScopes $path @($Approval['modifyScopes']))) {
                return @{ allowed = $false; reason = ('패치 대상이 승인된 수정 범위 밖입니다: ' + $path) }
            }
        }
        return @{ allowed = $true; reason = '' }
    }

    foreach ($scopeValue in @($Approval['commandScopes'])) {
        $scope = [string]$scopeValue
        if ($scope.Equals($toolName, [StringComparison]::OrdinalIgnoreCase) -or
            $scope.StartsWith($toolName + ' ', [StringComparison]::OrdinalIgnoreCase)) {
            return @{ allowed = $true; reason = '' }
        }
    }
    return @{ allowed = $false; reason = ('도구가 승인된 실행 목록에 없습니다: ' + $toolName) }
}

function Register-ValidationFailure {
    param(
        [hashtable]$State,
        [string]$Reason
    )

    $State['pendingFailureReport'] = @{
        reason = $Reason
        reported = $false
    }
    return $State
}

function Write-HookValidationFailure {
    param(
        [hashtable]$HookInput,
        [string]$Reason
    )

    $sessionId = [string](Get-HookValue $HookInput 'session_id' '')
    $eventName = [string](Get-HookValue $HookInput 'hook_event_name' '')
    $state = Read-SessionState $sessionId
    $state = Register-ValidationFailure $state $Reason
    Write-SessionState $sessionId $state

    $message = '[RULE_HOOK_RETRY] 훅 검증 실패를 사용자에게 즉시 보고하십시오. 추가 승인을 요청하지 말고 기존 승인 범위 안에서 보완한 뒤 다시 검증하고 작업을 계속하십시오. 사유: ' + $Reason

    switch ($eventName) {
        'UserPromptSubmit' {
            Write-HookJson @{
                systemMessage = $message
                hookSpecificOutput = @{
                    hookEventName = 'UserPromptSubmit'
                    additionalContext = $message + ' 사용자 프롬프트 자체는 차단하지 않았습니다.'
                }
            }
        }
        'PreToolUse' {
            Write-HookJson @{
                hookSpecificOutput = @{
                    hookEventName = 'PreToolUse'
                    permissionDecision = 'deny'
                    permissionDecisionReason = $message
                }
            }
        }
        'PermissionRequest' {
            Write-HookJson @{
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
            Write-HookJson @{
                decision = 'block'
                reason = $message
                hookSpecificOutput = @{
                    hookEventName = 'PostToolUse'
                    additionalContext = $message
                }
            }
        }
        'Stop' {
            Write-HookJson @{
                decision = 'block'
                reason = $message
            }
        }
        'SubagentStop' {
            Write-HookJson @{
                decision = 'block'
                reason = $message
            }
        }
        'SessionStart' {
            Write-HookJson @{
                systemMessage = $message
                hookSpecificOutput = @{
                    hookEventName = 'SessionStart'
                    additionalContext = $message
                }
            }
        }
        'SubagentStart' {
            Write-HookJson @{
                systemMessage = $message
                hookSpecificOutput = @{
                    hookEventName = 'SubagentStart'
                    additionalContext = $message
                }
            }
        }
        default {
            Write-HookJson @{ systemMessage = $message }
        }
    }
}

function Test-AllChangesAreRuleDocuments {
    param(
        [object[]]$Changes,
        [hashtable]$Configuration
    )

    if (@($Changes).Count -eq 0) {
        return $false
    }
    foreach ($change in @($Changes)) {
        if (-not (Test-IsRuleDocumentPath ([string]$change['path']) $Configuration)) {
            return $false
        }
    }
    return $true
}

function Test-ChangesWithinApproval {
    param(
        [object[]]$Changes,
        [hashtable]$Approval
    )

    if ($null -eq $Approval) {
        return $false
    }
    foreach ($change in @($Changes)) {
        if (-not (Test-PathWithinScopes ([string]$change['path']) @($Approval['modifyScopes']))) {
            return $false
        }
    }
    return $true
}
