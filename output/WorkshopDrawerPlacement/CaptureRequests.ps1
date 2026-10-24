function Invoke-DrawerPreviewRequest([string]$drawerAction, [string]$drawerArgument) {
    $drawerRequestId = [guid]::NewGuid().ToString()
    @($drawerRequestId, $drawerAction, $drawerArgument) | Set-Content output/WorkshopDrawerPlacement/Request.txt -Encoding utf8
    $drawerDeadline = (Get-Date).AddSeconds(15)
    do {
        Start-Sleep -Milliseconds 200
        $drawerResult = Get-Content output/WorkshopDrawerPlacement/Result.txt -ErrorAction SilentlyContinue
        if ((Get-Date) -gt $drawerDeadline) { throw 'Preview request timeout' }
    } while ($drawerResult[0] -ne $drawerRequestId)
    if ($drawerResult[1] -ne 'OK') { throw ($drawerResult -join [Environment]::NewLine) }
    Start-Sleep -Milliseconds 600
    Write-Output $drawerAction
}
Invoke-DrawerPreviewRequest 'runtime-restore' ''
Invoke-DrawerPreviewRequest 'runtime-shot' 'PlayFocusClosed'
Invoke-DrawerPreviewRequest 'runtime-open' 'Drawer_02_Switches'
Invoke-DrawerPreviewRequest 'runtime-shot' 'PlayFocusSwitches'
Invoke-DrawerPreviewRequest 'runtime-restore' ''
Invoke-DrawerPreviewRequest 'runtime-open' 'Drawer_05_UpperHousing'
Invoke-DrawerPreviewRequest 'runtime-shot' 'PlayFocusUpperHousing'
Invoke-DrawerPreviewRequest 'runtime-restore' ''
