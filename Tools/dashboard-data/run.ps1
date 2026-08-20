# Gatekeeper's Protocol - regenerate Docs/dashboard-data.js for the project dashboard.
# Session logs stay picker-only in Docs/dashboard.html; this feed is milestones,
# commits, questions, and a rule-based next-focus card (no LLM, no dates-as-ETA).
#
#   Tools/dashboard-data/run.ps1
#   Tools/dashboard-data/run.sh

$ErrorActionPreference = "Stop"
$Root = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$OutJs = Join-Path $Root "Docs\dashboard-data.js"
$MilestonesRoot = Join-Path $Root "Docs\Milestones"
$RoadmapPath = Join-Path $Root "Docs\Roadmap.md"
$VerificationPath = Join-Path $Root "Docs\GatekeeperZo Notes\verification.md"
$DecisionsPath = Join-Path $Root "Docs\Decisions.md"
$BrainstormsRoot = Join-Path $Root "Docs\Brainstorms"
$HygieneRoot = Join-Path $Root "Docs\Hygiene"

function Rel-Path([string]$abs) {
    $rel = $abs.Substring($Root.Length).TrimStart('\', '/')
    return ($rel -replace '\\', '/')
}

function Parse-MilestoneFile([System.IO.FileInfo]$file, [string]$folderStatus) {
    $text = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
    $id = $null
    if ($file.BaseName -match '^(M\d+(?:-\d+)?)') { $id = $Matches[1] }
    $title = $file.BaseName
    if ($text -match '(?m)^#\s+(M[\d-]+)\s+(.+)$') {
        $title = ($Matches[2] -replace '^[\p{Pd}\-\s]+', '').Trim()
    }
    $lastUpdated = $null
    if ($text -match '(?m)^Last updated:\s*(\d{4}-\d{2}-\d{2})') {
        $lastUpdated = $Matches[1]
    }
    $closedOn = $null
    if ($text -match '(?m)^Closed on:\s*(.+)$') {
        $closedOn = $Matches[1].Trim()
    }
    return [pscustomobject]@{
        id           = $id
        title        = $title
        statusFolder = $folderStatus
        path         = (Rel-Path $file.FullName)
        lastUpdated  = $lastUpdated
        closedOn     = $closedOn
        gateOneLiner = $null
        roadmapRank  = $null
    }
}

$folderMap = @{
    "1. planned"   = "planned"
    "2. active"    = "active"
    "3. completed" = "completed"
    "4. deferred"  = "deferred"
    "5. cancelled" = "cancelled"
}
$byId = @{}
$planned = @()
$active = @()
$completedCount = 0
$cancelledCount = 0
$deferredCount = 0

Get-ChildItem -LiteralPath $MilestonesRoot -Directory | ForEach-Object {
    $folderStatus = $folderMap[$_.Name]
    if (-not $folderStatus) { return }
    Get-ChildItem -LiteralPath $_.FullName -Filter "M*.md" -File | ForEach-Object {
        $m = Parse-MilestoneFile $_ $folderStatus
        if (-not $m.id) { return }
        $byId[$m.id] = $m
        switch ($folderStatus) {
            "planned"   { $planned += $m }
            "active"    { $active += $m }
            "completed" { $completedCount++ }
            "cancelled" { $cancelledCount++ }
            "deferred"  { $deferredCount++ }
        }
    }
}

function Parse-HygieneFile([System.IO.FileInfo]$file, [string]$folderStatus) {
    $text = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
    $id = $null
    if ($file.BaseName -match '^(HP-\d+)') { $id = $Matches[1] }
    $title = $file.BaseName
    if ($text -match '(?m)^#\s+(HP-\d+)\s+(.+)$') {
        $title = ($Matches[2] -replace '^[\p{Pd}\-\s]+', '').Trim()
    }
    $closableBy = ""
    if ($text -match '(?sm)^Closable by:\s*(.+?)(?=\r?\n[A-Za-z][A-Za-z ]*:|\r?\n-->)') {
        $closableBy = (($Matches[1] -replace '\s+', ' ').Trim())
    }
    $lastUpdated = $null
    if ($text -match '(?m)^Last updated:\s*(\d{4}-\d{2}-\d{2})') {
        $lastUpdated = $Matches[1]
    }
    $itemCount = 0
    if ($text -match '(?sm)^##\s+Items\s*\r?\n(.*?)(?=\r?\n##\s|\z)') {
        $itemCount = ([regex]::Matches($Matches[1], '(?m)^\s*\d+\.\s+\*\*(TH-\d+)\*\*')).Count
    }
    return [pscustomobject]@{
        id          = $id
        title       = $title
        status      = $folderStatus
        closableBy  = $closableBy
        itemCount   = $itemCount
        path        = (Rel-Path $file.FullName)
        lastUpdated = $lastUpdated
    }
}

# Docs/Hygiene: folder is status, exactly like Docs/Milestones (_definitions/hygiene.md).
$hygieneFolderMap = @{
    "1. open"    = "open"
    "2. settled" = "settled"
}
$hygieneOpen = @()
$hygieneSettled = @()
if (Test-Path -LiteralPath $HygieneRoot) {
    Get-ChildItem -LiteralPath $HygieneRoot -Directory | ForEach-Object {
        $hStatus = $hygieneFolderMap[$_.Name]
        if (-not $hStatus) { return }
        Get-ChildItem -LiteralPath $_.FullName -Filter "HP-*.md" -File | ForEach-Object {
            $h = Parse-HygieneFile $_ $hStatus
            if (-not $h.id) { return }
            if ($hStatus -eq "open") { $hygieneOpen += $h } else { $hygieneSettled += $h }
        }
    }
}
function Sort-Hygiene($items) {
    return , @($items | Sort-Object @{ Expression = { [int](($_.id -replace '\D', '')) } })
}
$hygieneOpen = Sort-Hygiene $hygieneOpen
$hygieneSettled = Sort-Hygiene $hygieneSettled
$hygieneOpenItems = 0
foreach ($h in $hygieneOpen) { $hygieneOpenItems += $h.itemCount }

$roadmapText = Get-Content -LiteralPath $RoadmapPath -Raw -Encoding UTF8
$roadmapOrder = [System.Collections.Generic.List[string]]::new()
$rank = 0
foreach ($line in ($roadmapText -split "`n")) {
    if ($line -match '^\|\s*(M[\d-]+)\s*\|\s*([^|]+)\|\s*([^|]+)\|\s*([^|]+)\|') {
        $id = $Matches[1].Trim()
        $gate = $Matches[4].Trim()
        $rank++
        if (-not $roadmapOrder.Contains($id)) { [void]$roadmapOrder.Add($id) }
        if ($byId.ContainsKey($id)) {
            $byId[$id].gateOneLiner = $gate
            $byId[$id].roadmapRank = $rank
        }
    }
}

$friendsSummary = "two friends install a build, join without the developer touching a keyboard, play a full five-wave run with both participating, and the run ends with a screen that tells them what happened."
if ($roadmapText -match '(?s)\*\*Friends alpha gate\*\*[^:\n]*:\s*(.+?)(?=\n\n|\n\*\*)') {
    $friendsSummary = (($Matches[1] -replace '\s+', ' ').Trim())
}

$recommendedNext = [System.Collections.Generic.List[string]]::new()
if ($roadmapText -match '(?s)\*\*Recommended next\*\*[^\n]*\n(.*?)(?=\nPlanned count|\n## )') {
    $recBlock = $Matches[1]
    [regex]::Matches($recBlock, '\b(M\d+(?:-\d+)?)\b') | ForEach-Object {
        $id = $_.Groups[1].Value
        if (-not $recommendedNext.Contains($id)) { [void]$recommendedNext.Add($id) }
    }
}

function Sort-Lane($items) {
    return , @($items | Sort-Object @{
            Expression = { if ($null -eq $_.roadmapRank) { 9999 } else { $_.roadmapRank } }
        }, id)
}
$planned = Sort-Lane $planned
$active = Sort-Lane $active

$commits = @()
Push-Location $Root
try {
    $log = & git log -50 --pretty=format:"%H|%h|%ad|%s" --date=short 2>$null
    foreach ($line in $log) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $parts = $line -split '\|', 4
        if ($parts.Count -lt 4) { continue }
        $commits += [pscustomobject]@{
            hash       = $parts[0]
            short      = $parts[1]
            authoredAt = $parts[2]
            subject    = $parts[3]
        }
    }
}
finally { Pop-Location }

$questions = [System.Collections.Generic.List[object]]::new()
function Add-Question([string]$priority, [string]$text, [string]$sourcePath, [string]$heading, [string]$milestoneId) {
    $t = ($text -replace '\s+', ' ').Trim()
    if ([string]::IsNullOrWhiteSpace($t)) { return }
    if ($t.Length -gt 280) { $t = $t.Substring(0, 277) + "..." }
    $qid = "q" + ($questions.Count + 1)
    [void]$questions.Add([pscustomobject]@{
            id            = $qid
            priority      = $priority
            text          = $t
            sourcePath    = $sourcePath
            sourceHeading = $heading
            milestoneId   = $milestoneId
        })
}

$vText = Get-Content -LiteralPath $VerificationPath -Raw -Encoding UTF8
$vRel = Rel-Path $VerificationPath
if ($vText -match '(?s)## Next Run[^\n]*\n(.*?)(?=\n## When convenient|\n## Not play|\n## Closed)') {
    $block = $Matches[1]
    [regex]::Matches($block, '(?m)^###\s+\d+\.\s+(.+?)\s+[\p{Pd}\-]+\s*(M[\d-]+)') | ForEach-Object {
        Add-Question "blocking" $_.Groups[1].Value.Trim() $vRel "Next Run" $_.Groups[2].Value
    }
}
if ($vText -match '(?s)## When convenient\s*\n(.*?)(?=\n## Not play|\n## Closed)') {
    $block = $Matches[1]
    [regex]::Matches($block, '(?m)^\|\s*([^|]+)\|\s*(M[\d-]+)\s*\|') | ForEach-Object {
        $item = $_.Groups[1].Value.Trim()
        if ($item -eq "Item" -or $item -match '^-{2,}') { return }
        Add-Question "play" $item $vRel "When convenient" $_.Groups[2].Value
    }
}
if ($vText -match '(?s)## Not play[^\n]*\n(.*?)(?=\n## Closed)') {
    $block = $Matches[1]
    [regex]::Matches($block, '(?m)^\|\s*([^|]+)\|\s*(M[\d-]+)\s*\|\s*([^|]+)\|') | ForEach-Object {
        $item = $_.Groups[1].Value.Trim()
        if ($item -eq "Item" -or $item -match '^-{2,}') { return }
        $blocker = $_.Groups[3].Value.Trim()
        Add-Question "deferred" ($item + " - " + $blocker) $vRel "Not play" $_.Groups[2].Value
    }
}

$dText = Get-Content -LiteralPath $DecisionsPath -Raw -Encoding UTF8
$dRel = Rel-Path $DecisionsPath
if ($dText -match '(?s)## Deferred, with intent\s*\n(.*?)(?=\n## )') {
    $block = $Matches[1]
    [regex]::Matches($block, '(?m)^-\s+\*\*([^*]+)\*\*\s*[\p{Pd}\-:]*\s*(.+)$') | ForEach-Object {
        Add-Question "deferred" ($_.Groups[1].Value.Trim() + ": " + $_.Groups[2].Value.Trim()) $dRel "Deferred, with intent" $null
    }
    [regex]::Matches($block, '(?m)^-\s+([^:].+)$') | ForEach-Object {
        $line = $_.Groups[1].Value.Trim()
        if ($line.StartsWith("**")) { return }
        Add-Question "deferred" $line $dRel "Deferred, with intent" $null
    }
}

Get-ChildItem -LiteralPath $BrainstormsRoot -Filter "*.md" -File -Recurse | Where-Object {
    $_.FullName -notmatch '[\\/]closed[\\/]'
} | ForEach-Object {
    $bText = Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8
    $bRel = Rel-Path $_.FullName
    if ($bText -match '(?s)## Open questions\s*\n(.*?)(?=\n## |\z)') {
        $block = $Matches[1]
        [regex]::Matches($block, '(?m)^-\s+\*\*([^*]+)\*\*\s*(.*)$') | ForEach-Object {
            $head = $_.Groups[1].Value.Trim()
            $rest = ($_.Groups[2].Value -replace '\s+', ' ').Trim()
            Add-Question "brainstorm" ($head + " " + $rest).Trim() $bRel "Open questions" $null
        }
    }
}

$warnings = [System.Collections.Generic.List[object]]::new()
$insights = [System.Collections.Generic.List[object]]::new()
$plannedMax = 10
$activeMax = 3

function Add-Insight([string]$sev, [string]$headline, [string]$why, [string]$action, [string[]]$evidence) {
    [void]$insights.Add([pscustomobject]@{
            severity = $sev
            headline = $headline
            why      = $why
            action   = $action
            evidence = @($evidence)
        })
}

# Head of the queue = the first Recommended next id that is still open, else the first
# active file, else the first planned one. Ordering comes from Roadmap.md; never a literal.
$friendsHead = ""
foreach ($id in $recommendedNext) {
    if ($byId.ContainsKey($id) -and ($byId[$id].statusFolder -eq "active" -or $byId[$id].statusFolder -eq "planned")) {
        $friendsHead = $id
        break
    }
}
if (-not $friendsHead -and $active.Count -gt 0) { $friendsHead = $active[0].id }
if (-not $friendsHead -and $planned.Count -gt 0) { $friendsHead = $planned[0].id }
$headStillOpen = ($byId.ContainsKey($friendsHead) -and
    ($byId[$friendsHead].statusFolder -eq "planned" -or $byId[$friendsHead].statusFolder -eq "active"))
$headTitle = if ($headStillOpen) { $byId[$friendsHead].title } else { $null }
$headGate = if ($headStillOpen -and $byId[$friendsHead].gateOneLiner) { $byId[$friendsHead].gateOneLiner } else { "" }

# Friends-alpha filter beats "whatever is in active/" when the head is still open.
# Everything below is derived from the folder listing, Roadmap.md and the milestone files.
# Do not name a milestone id in this section - a literal here is a claim that cannot expire.
$primary = $null
$reasonCodes = [System.Collections.Generic.List[string]]::new()
$evidence = [System.Collections.Generic.List[string]]::new()
$doNowAction = ""

if ($headStillOpen) {
    $primary = $byId[$friendsHead]
    [void]$reasonCodes.Add("friends_alpha")
    [void]$evidence.Add("Docs/Roadmap.md Friends alpha gate")
    [void]$evidence.Add("Docs/Roadmap.md ## Current Recommended next")
    [void]$evidence.Add($primary.path)
    $doNowAction = ("Ship {0} ({1}): {2}" -f $primary.id, $primary.title, $(if ($headGate) { $headGate } else { "close its acceptance criteria" }))
}
elseif ($active.Count -gt 0) {
    $primary = $active[0]
    [void]$reasonCodes.Add("active")
    [void]$evidence.Add(("Docs/Milestones/2. active/{0}" -f (Split-Path $primary.path -Leaf)))
    $doNowAction = ("Finish active {0} ({1})." -f $primary.id, $primary.title)
}
elseif ($recommendedNext.Count -gt 0) {
    foreach ($id in $recommendedNext) {
        if ($byId.ContainsKey($id) -and $byId[$id].statusFolder -eq "planned") {
            $primary = $byId[$id]
            [void]$reasonCodes.Add("roadmap_order")
            [void]$evidence.Add("Docs/Roadmap.md ## Current Recommended next")
            $doNowAction = ("Start {0} ({1})." -f $primary.id, $primary.title)
            break
        }
    }
}
if (-not $primary -and $planned.Count -gt 0) {
    $primary = $planned[0]
    [void]$reasonCodes.Add("roadmap_order")
    [void]$evidence.Add("Docs/Roadmap.md Order table")
    $doNowAction = ("Start {0} ({1})." -f $primary.id, $primary.title)
}

# --- Insights (judgment, ordered by leverage) ---
if ($planned.Count -gt $plannedMax) {
    [void]$warnings.Add([pscustomobject]@{
            code     = "planned_over_cap"
            detail   = ("Planned is {0}/{1} - stop generating milestones; build or cut one." -f $planned.Count, $plannedMax)
            evidence = @("Docs/Working-Rules.md (lane caps)", "Docs/Roadmap.md ## Current")
        })
    Add-Insight "critical" `
        ("Planning is ahead of shipping: {0} planned files against a cap of {1}." -f $planned.Count, $plannedMax) `
        "Working-Rules treats that ceiling as the check that expansion has not become documentation." `
        "Freeze new milestones. Close or cancel one planned file before any brainstorm graduates." `
        @("Docs/Working-Rules.md (lane caps)", "Docs/Roadmap.md ## Current", ("generator count planned={0}" -f $planned.Count))
    [void]$reasonCodes.Add("cap_breach")
}
if ($active.Count -gt $activeMax) {
    [void]$warnings.Add([pscustomobject]@{
            code     = "active_over_cap"
            detail   = ("Active is {0}/{1}." -f $active.Count, $activeMax)
            evidence = @("Docs/Working-Rules.md (lane caps)")
        })
}

if ($headStillOpen) {
    $activeIds = @($active | ForEach-Object { $_.id })
    $activeTitles = (@($active | ForEach-Object { "{0} ({1})" -f $_.id, $_.title }) -join ", ")
    if ($active.Count -gt 0 -and ($activeIds -notcontains $friendsHead)) {
        Add-Insight "critical" `
            ("Friends-alpha is blocked on {0}, while open actives are {1} - off that path." -f $friendsHead, $activeTitles) `
            "Fellowship stays incomplete until a stranger installs and finishes five waves without Zo at the keyboard." `
            ("Promote {0} into the work session. Finish or demote {1} so active slots match the queue sentence." -f $friendsHead, $activeTitles) `
            @("Docs/Roadmap.md Friends alpha gate", "Docs/Design/1-Experience.md (Fellowship)", "Docs/Milestones/2. active/")
    }
    else {
        Add-Insight "critical" `
            ("Queue head is {0}: {1}." -f $friendsHead, $headTitle) `
            "Everything else is ordered behind the Roadmap's friends-alpha sentence; the head is what moves it." `
            $doNowAction `
            @("Docs/Roadmap.md Friends alpha gate", $primary.path)
    }
}

$blockingQs = @($questions | Where-Object { $_.priority -eq "blocking" })
if ($blockingQs.Count -gt 0) {
    $names = (@($blockingQs | ForEach-Object {
                if ($_.milestoneId) { "{0} ({1})" -f $_.text, $_.milestoneId } else { $_.text }
            } | Select-Object -First 4) -join "; ")
    Add-Insight "attention" `
        ("{0} shipped claims still lack a clean play log: {1}." -f $blockingQs.Count, $names) `
        "Expression and Challenge already treat those systems as served; without a log they are confident prose." `
        "One dedicated Run closes the Next Run list; write pass/fail into playthrough.md and the owning milestone the same day." `
        @("Docs/GatekeeperZo Notes/verification.md ## Next Run", ("blocking question count={0}" -f $blockingQs.Count))
}

$brainstorms = @()
Get-ChildItem -LiteralPath $BrainstormsRoot -Filter "*.md" -File | ForEach-Object {
    $t = Get-Content -LiteralPath $_.FullName -TotalCount 40 -Encoding UTF8 | Out-String
    $full = Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8
    $topic = $_.BaseName
    if ($t -match '(?m)^#\s+(.+)$') { $topic = $Matches[1].Trim() }
    $status = "open"
    if ($t -match '(?i)Status:\s*closed|closed record') { $status = "closed" }
    if ($full -match '(?i)Graduated by direction') { $status = "graduating" }
    $brainstorms += [pscustomobject]@{
        file   = (Rel-Path $_.FullName)
        title  = $topic
        status = $status
    }
}

$graduating = @($brainstorms | Where-Object { $_.status -eq "graduating" })
if ($graduating.Count -gt 0 -and $planned.Count -ge $plannedMax) {
    $gNames = ($graduating | ForEach-Object { $_.title }) -join ", "
    $gFiles = @($graduating | ForEach-Object { $_.file }) + @("Docs/Working-Rules.md (lane caps)")
    Add-Insight "attention" `
        ("Brainstorm pressure ({0}) is graduating while planned is already at or over cap." -f $gNames) `
        "A graduating brainstorm at cap turns planning into the session's default output." `
        "Leave specialist sheets in brainstorm. Do not cut new protocol milestones until planned is under cap and the friends head is under way." `
        $gFiles
}

# A file that already carries a "Closed on:" stamp but still sits in 2. active/ is holding
# a slot the lane cap counts. Derived from the folder plus the file, never from an id list.
$stampedActive = @($active | Where-Object { $_.closedOn })
if ($stampedActive.Count -gt 0) {
    $stampedNames = (@($stampedActive | ForEach-Object { "{0} ({1})" -f $_.id, $_.title }) -join ", ")
    $stampedEvidence = @($stampedActive | ForEach-Object { $_.path }) + @("Docs/Changelog.md", "Docs/GatekeeperZo Notes/verification.md ## Closed")
    Add-Insight "attention" `
        ("{0} still occupies an active slot after carrying a Closed on stamp." -f $stampedNames) `
        "Active capacity looking open while a stamped file sits in the lane is how a milestone list lies about progress." `
        ("Move {0} to 3. completed, or strip the stamp and leave only the criteria that are still open, then fill the slot on purpose." -f $stampedNames) `
        $stampedEvidence
}

$playQs = @($questions | Where-Object { $_.priority -eq "play" }).Count
$brainQs = @($questions | Where-Object { $_.priority -eq "brainstorm" }).Count
if ($questions.Count -gt 15) {
    Add-Insight "attention" `
        ("Question index is {0} items ({1} play-blocking, {2} play, {3} brainstorm) - most of it is not decision debt." -f $questions.Count, $blockingQs.Count, $playQs, $brainQs) `
        "A long list looks like design fog; the real risk is the four Next Run claims and any missing pin logs." `
        "Ignore brainstorm Open until after the friends head. Run Next Run; restore or retire absent pin logs." `
        @("Docs/GatekeeperZo Notes/verification.md", ("question counts blocking={0} play={1} brainstorm={2}" -f $blockingQs.Count, $playQs, $brainQs))
}

Add-Insight "healthy" `
    "Goals are Fellowship, Expression, Fantasy. Challenge is the vehicle. The five-Wave friends run is still the alpha sentence." `
    "Ordering work against the top three stops treating Wave TTK as the aesthetic." `
    "Take Recommended next in order. Specialist Protocols for Expression. Thicken Fantasy via kinds/destinations, not Default HP." `
    @("Docs/Design/1-Experience.md 1.2", "Docs/Roadmap.md Strategy")

# Executive synthesis (templated from the same facts)
$activeTitles = (@($active | ForEach-Object { "{0}" -f $_.id }) -join "/")
$activeIdsForSynth = @($active | ForEach-Object { $_.id })
if ($headStillOpen) {
    if ($activeIdsForSynth -contains $friendsHead) {
        $synthesis = ("Fellowship is the top aesthetic and the install-and-join sentence still fails. {0} is the active head. Planned {1}/{2}. {3} Next Run claims still lack a clean play log." -f $friendsHead, $planned.Count, $plannedMax, $blockingQs.Count)
    }
    else {
        $synthesis = ("Fellowship is the top aesthetic. Friends-alpha still fails; {0} heads the queue against that sentence. Planned sits at {1}/{2} while actives {3} sit off that path. What remains is play proof of {4} Next Run claims and whether protocols earn another planning slot after the cap clears." -f $friendsHead, $planned.Count, $plannedMax, $(if ($activeTitles) { $activeTitles } else { "(none)" }), $blockingQs.Count)
    }
}
else {
    $activeClause = if ($activeTitles) { ("Active is {0}." -f $activeTitles) } else { "Nothing is active." }
    $synthesis = ("{0} Planned {1}/{2}; active {3}/{4}. The Roadmap friends-alpha sentence is still the gate. Next work follows recommended order, then play debt; {5} Next Run claims still lack a clean play log." -f $activeClause, $planned.Count, $plannedMax, $active.Count, $activeMax, $blockingQs.Count)
}

$queue = [System.Collections.Generic.List[object]]::new()
$seen = @{}
function Enqueue-Focus([string]$id, [string]$blockedBy) {
    if ([string]::IsNullOrWhiteSpace($id)) { return }
    if ($seen.ContainsKey($id)) { return }
    if (-not $byId.ContainsKey($id)) { return }
    $m = $byId[$id]
    if ($m.statusFolder -ne "planned" -and $m.statusFolder -ne "active") { return }
    $seen[$id] = $true
    [void]$queue.Add([pscustomobject]@{
            id        = $m.id
            title     = $m.title
            status    = $m.statusFolder
            blockedBy = $blockedBy
        })
}

if ($headStillOpen) {
    $blockLater = $friendsHead
    Enqueue-Focus $friendsHead $null
}
else { $blockLater = $null }
foreach ($id in $recommendedNext) {
    $bb = $null
    if ($blockLater -and $id -ne $blockLater -and $byId.ContainsKey($id) -and $byId[$id].statusFolder -eq "planned") {
        $bb = $blockLater
    }
    Enqueue-Focus $id $bb
}
foreach ($m in $active) { Enqueue-Focus $m.id $null }
foreach ($m in $planned) {
    $bb = $null
    if ($blockLater -and $m.id -ne $blockLater) { $bb = $blockLater }
    Enqueue-Focus $m.id $bb
}

$generatedAt = [DateTime]::UtcNow.ToString("o")
$generatedAtCommit = ""
Push-Location $Root
try {
    $headSha = & git rev-parse --short HEAD 2>$null
    if (-not [string]::IsNullOrWhiteSpace($headSha)) { $generatedAtCommit = ([string]$headSha).Trim() }
}
finally { Pop-Location }

function Hygiene-Rows($items, [string]$folder) {
    $rows = [System.Collections.Generic.List[object]]::new()
    foreach ($h in $items) {
        [void]$rows.Add([ordered]@{
                id           = $h.id
                title        = $h.title
                statusFolder = $folder
                closableBy   = $h.closableBy
                itemCount    = $h.itemCount
                path         = $h.path
                lastUpdated  = $h.lastUpdated
            })
    }
    return , $rows.ToArray()
}

function Lane-Rows($items, [string]$folder) {
    $rows = [System.Collections.Generic.List[object]]::new()
    foreach ($m in $items) {
        [void]$rows.Add([ordered]@{
                id           = $m.id
                title        = $m.title
                statusFolder = $folder
                gateOneLiner = $(if ($m.gateOneLiner) { $m.gateOneLiner } else { "" })
                path         = $m.path
                roadmapRank  = $m.roadmapRank
                lastUpdated  = $m.lastUpdated
            })
    }
    return , $rows.ToArray()
}

$primaryObj = $null
if ($primary) {
    $primaryObj = [ordered]@{
        id          = $primary.id
        title       = $primary.title
        status      = $primary.statusFolder
        action      = $doNowAction
        reasonCodes = @($reasonCodes)
        evidence    = @($evidence)
    }
}

$payload = [ordered]@{
    generatedAt     = $generatedAt
    generatedAtCommit = $generatedAtCommit
    caps            = [ordered]@{ plannedMax = $plannedMax; activeMax = $activeMax }
    friendsGate     = [ordered]@{ headId = $friendsHead; summary = $friendsSummary }
    roadmapOrder    = @($roadmapOrder)
    recommendedNext = @($recommendedNext | Where-Object { $byId.ContainsKey($_) -and ($byId[$_].statusFolder -eq "planned" -or $byId[$_].statusFolder -eq "active") })
    synthesis       = $synthesis
    insights        = @($insights | ForEach-Object {
            [ordered]@{
                severity = $_.severity
                headline = $_.headline
                why      = $_.why
                action   = $_.action
                evidence = @($_.evidence)
            }
        })
    lanes           = [ordered]@{
        planned        = (Lane-Rows $planned "planned")
        active         = (Lane-Rows $active "active")
        completedCount = $completedCount
        cancelledCount = $cancelledCount
        deferredCount  = $deferredCount
    }
    hygiene     = [ordered]@{
        open          = (Hygiene-Rows $hygieneOpen "open")
        settled       = (Hygiene-Rows $hygieneSettled "settled")
        openCount     = $hygieneOpen.Count
        settledCount  = $hygieneSettled.Count
        openItemCount = $hygieneOpenItems
    }
    commits     = @($commits | ForEach-Object {
            [ordered]@{ hash = $_.hash; short = $_.short; authoredAt = $_.authoredAt; subject = $_.subject }
        })
    questions   = @($questions | ForEach-Object {
            [ordered]@{
                id            = $_.id
                priority      = $_.priority
                text          = $_.text
                sourcePath    = $_.sourcePath
                sourceHeading = $_.sourceHeading
                milestoneId   = $_.milestoneId
            }
        })
    brainstorms = @($brainstorms | ForEach-Object {
            [ordered]@{ file = $_.file; title = $_.title; status = $_.status }
        })
    focus       = [ordered]@{
        primary  = $primaryObj
        queue    = @($queue | ForEach-Object {
                [ordered]@{ id = $_.id; title = $_.title; status = $_.status; blockedBy = $_.blockedBy }
            })
        warnings = @($warnings | ForEach-Object {
                [ordered]@{ code = $_.code; detail = $_.detail; evidence = @($_.evidence) }
            })
    }
}

$json = ($payload | ConvertTo-Json -Depth 8 -Compress:$false)
$js = @"
/* Generated by Tools/dashboard-data/run.ps1 - do not hand-edit.
 * Regenerate: Tools/dashboard-data/run.ps1
 * generatedAt: $generatedAt
 * generatedAtCommit: $generatedAtCommit
 */
window.GKB_PROJECT = $json;
"@

Set-Content -LiteralPath $OutJs -Value $js -Encoding UTF8
Write-Host ("dashboard-data: wrote {0}" -f $OutJs)
Write-Host ("  planned={0}/{1} active={2}/{3} questions={4} commits={5} insights={6}" -f $planned.Count, $plannedMax, $active.Count, $activeMax, $questions.Count, $commits.Count, $insights.Count)
Write-Host ("  hygiene: open={0} settled={1} openItems={2}" -f $hygieneOpen.Count, $hygieneSettled.Count, $hygieneOpenItems)
if ($primary) {
    Write-Host ("  do-now={0}: {1}" -f $primary.id, $doNowAction)
}
foreach ($w in $warnings) { Write-Host ("  warning: {0}" -f $w.detail) }
exit 0
