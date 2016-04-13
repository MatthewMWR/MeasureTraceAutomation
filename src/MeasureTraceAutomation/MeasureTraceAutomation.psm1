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
        [Parameter(Mandatory=$true, ParameterSetName="PackageFileName")]
        [string]$PackageFileName,
        [Parameter(Mandatory=$true, ParameterSetName="FilterScript")]
        [scriptblock]$FilterScript,
        [Parameter(Mandatory=$true, ParameterSetName="ProcessingState")]
        [MeasureTraceAutomation.ProcessingState]$ProcessingState,
        [switch]$IncludeMeasurements
    )
    $store = New-Object -Type MeasureTraceAutomation.MeasurementStore -Arg $StoreConfig
    if($PSCmdlet.ParameterSetName -eq "PackageFileName"){
        $storeExt::GetTraceByFilter($store, {$args[0].PackageFileName -like $PackageFileName}, $IncludeMeasurements)
    }
    elseif($PSCmdlet.ParameterSetName -eq "FilterScript"){
        $storeExt::GetTraceByFilter($store, $FilterScript, $IncludeMeasurements)
    }
    elseif($PSCmdlet.ParameterSetName -eq "ProcessingState"){
        $storeExt::GetTraceByState($store, $ProcessingState)
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