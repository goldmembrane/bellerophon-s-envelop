[CmdletBinding()]
param(
    # Keep the defaults ASCII-only so Windows PowerShell 5.1 parses them correctly
    # whether this script is stored as UTF-8 with or without a BOM.
    [string]$Title = ('Bellerophon ' + (-join [char[]](0xC791, 0xC5C5, 0x0020, 0xC644, 0xB8CC))),
    [string]$Message = (-join [char[]](
        0xC694, 0xCCAD, 0xD558, 0xC2E0, 0x0020,
        0xC791, 0xC5C5, 0xC774, 0x0020,
        0xC644, 0xB8CC, 0xB418, 0xC5C8, 0xC2B5, 0xB2C8, 0xB2E4, 0x002E))
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$popup = [System.Windows.Forms.Form]::new()
$notificationIcon = [System.Windows.Forms.NotifyIcon]::new()
try {
    $popup.Text = $Title
    $popup.Size = [System.Drawing.Size]::new(440, 150)
    $popup.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::FixedToolWindow
    $popup.StartPosition = [System.Windows.Forms.FormStartPosition]::Manual
    $popup.ShowInTaskbar = $false
    $popup.TopMost = $true
    $popup.BackColor = [System.Drawing.Color]::FromArgb(32, 35, 42)

    $workingArea = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
    $popup.Location = [System.Drawing.Point]::new(
        $workingArea.Right - $popup.Width - 24,
        $workingArea.Bottom - $popup.Height - 24)

    $titleLabel = [System.Windows.Forms.Label]::new()
    $titleLabel.Text = $Title
    $titleLabel.ForeColor = [System.Drawing.Color]::White
    $titleLabel.Font = [System.Drawing.Font]::new('Malgun Gothic', 13, [System.Drawing.FontStyle]::Bold)
    $titleLabel.Location = [System.Drawing.Point]::new(20, 18)
    $titleLabel.Size = [System.Drawing.Size]::new(390, 30)
    $popup.Controls.Add($titleLabel)

    $messageLabel = [System.Windows.Forms.Label]::new()
    $messageLabel.Text = $Message
    $messageLabel.ForeColor = [System.Drawing.Color]::Gainsboro
    $messageLabel.Font = [System.Drawing.Font]::new('Malgun Gothic', 10)
    $messageLabel.Location = [System.Drawing.Point]::new(22, 57)
    $messageLabel.Size = [System.Drawing.Size]::new(388, 48)
    $popup.Controls.Add($messageLabel)

    $notificationIcon.Icon = [System.Drawing.SystemIcons]::Information
    $notificationIcon.BalloonTipTitle = $Title
    $notificationIcon.BalloonTipText = $Message
    $notificationIcon.BalloonTipIcon = [System.Windows.Forms.ToolTipIcon]::Info
    $notificationIcon.Visible = $true

    $popup.Show()
    $popup.Activate()
    $popup.BringToFront()
    [System.Media.SystemSounds]::Exclamation.Play()
    $notificationIcon.ShowBalloonTip(5000)
    $visibleUntil = [DateTime]::UtcNow.AddMilliseconds(5500)
    while ([DateTime]::UtcNow -lt $visibleUntil) {
        [System.Windows.Forms.Application]::DoEvents()
        Start-Sleep -Milliseconds 50
    }
}
finally {
    $popup.Close()
    $popup.Dispose()
    $notificationIcon.Visible = $false
    $notificationIcon.Dispose()
}
