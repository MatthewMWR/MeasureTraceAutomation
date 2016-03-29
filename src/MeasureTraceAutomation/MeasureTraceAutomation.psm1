[Reflection.Assembly]::LoadFrom($(Join-Path $psscriptroot "MeasureTraceAutomation.dll"))

Set-StrictMode -Version Latest

function New-MtaStoreConfig{
    param(
        [Parameter(Mandatory=$true)]
        $ConnectionString,
        [MeasureTraceAutomation.StoreType]$StoreType = [MeasureTraceAutomation.StoreType]::MicrosoftSqlServer
    )
    $storeConfig = New-Object -Type MeasureTraceAutomation.MeasurementStoreConfig
    $storeConfig.StoreType = $StoreType
    $storeCOnfig.ConnectionString = $ConnectionString
    $storeConfig
}

function New-MtaProcessingConfig{
    param(
        [string]$DestinationDataPath,
        [string[]]$IncomingDataPath
    )
    $pc = New-Object -TypeName MeasureTraceAutomation.ProcessingConfig
    $pc.DestinationDataPath = $DestinationDataPath
    foreach($path in $IncomingDataPath){
        $pc.IncomingDataPaths.Add($IncomingDataPath)
    }
    $pc
}

function Get-MtaTrace{
    param(
        [Parameter(Mandatory=$true)]
        [MeasureTraceAutomation.MeasurementStoreConfig]$StoreConfig,
        [string]$PackageFileName,
        [switch]$IncludeProcessingRecords,
        [switch]$IncludeMeasurements
    )
    $store = New-Object -Type MeasureTraceAutomation.MeasurementStore -Arg $StoreConfig
    if( [string]::IsNullOrEmpty($PackageFileName)){
        $store.Traces.ForEach( {[MeasureTraceAutomation.MeasuredTrace]$_} )
    }
    else{
        $store.Traces.Where( {
        ($_.PackageFileName -eq $PackageFileName )
        }).Foreach( {[MeasureTraceAutomation.MeasuredTrace]$_} )
    }
    
    $store.Dispose()
}

function Set-MtaTrace{
    param(
        [Parameter(Mandatory=$true)]
        [MeasureTraceAutomation.MeasurementStoreConfig]$StoreConfig,
        [Parameter(Mandatory=$true)]
        [MeasureTrace.TraceModel.Trace]$Trace
    )
    $store = New-Object -Type MeasureTraceAutomation.MeasurementStore -Arg $StoreConfig
    $null = $store.Traces.Update( $Trace )
    $null = $store.SaveChanges()
    $store.Dispose()
}

function Reset-MtaStore{
    param(
        [Parameter(Mandatory=$true)]
        [MeasureTraceAutomation.MeasurementStoreConfig]$StoreConfig
    )
    if(-not $PSCmdlet.ShouldContinue("Delete and recreate dabase?","Delete and recreate dabase?")){
        return
    }
    $store = New-Object -Type MeasureTraceAutomation.MeasurementStore -Arg $StoreConfig
    $store.Database.EnsureCreated()
    $store.Database.EnsureDeleted()
    $store.Database.EnsureCreated()
    $store.Dispose()
}

function Invoke-MtaProcessing{
    param(
        [Parameter(Mandatory=$true)]
        [MeasureTraceAutomation.MeasurementStoreConfig]$StoreConfig,
        [Parameter(Mandatory=$true)]
        [MeasureTraceAutomation.ProcessingConfig]$ProcessingConfig

    )
    [MeasureTraceAutomation.DoWork]::InvokeProcessingOnce($ProcessingConfig, $StoreConfig)
}

function Get-MtaLog{
    param($MaxEvents = 30)
    Get-WinEvent -ProviderName MeasureTraceAutomation -MaxEvents $MaxEvents
}