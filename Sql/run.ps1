#requires -Version 5.1
[CmdletBinding()]
param(
 [ValidateSet('status','validate','plan','apply')][string]$Mode='validate',
 [string]$Server, [string]$Database, [string]$Authorization,
 [switch]$Offline
)
$ErrorActionPreference='Stop'
$manifest=Get-Content -LiteralPath "$PSScriptRoot/manifest.json" -Raw | ConvertFrom-Json
$known=@{baseline='UGP_Database_Schema.sql';UGP_0002='UGP_0002_ReservasPagos.sql'}
if(@($manifest.scripts).Count -ne 2){throw 'Manifiesto incompleto o desconocido'}
if((Get-FileHash "$PSScriptRoot/Gobernanza.sql" -Algorithm SHA256).Hash -ne $manifest.bootstrapSha256){throw 'Bootstrap alterado'}
$ids=@{}; $previous=0
foreach($s in $manifest.scripts){
 if(!$known.ContainsKey($s.id) -or $known[$s.id] -cne $s.file){throw 'Identidad de script desconocida'}
 if($ids.ContainsKey($s.id) -or $s.order -le $previous){throw 'Orden o identificador duplicado'}
 if($s.file -notmatch '^[A-Za-z0-9_]+\.sql$' -or $manifest.blocked -contains $s.file){throw 'Script no permitido'}
 $hash=(Get-FileHash -LiteralPath "$PSScriptRoot/$($s.file)" -Algorithm SHA256).Hash
 if($hash -ne $s.sha256){throw "Checksum distinto: $($s.id)"}
 $content=Get-Content -LiteralPath "$PSScriptRoot/$($s.file)" -Raw
 if(([regex]::Matches($content,'(?im)^COMMIT;\s*$')).Count -ne 1){throw 'Contrato transaccional desconocido'}
 $ids[$s.id]=$true; $previous=$s.order
}
if($Mode -eq 'validate' -or $Offline){
 if($Mode -eq 'apply'){throw 'apply no admite Offline'}
 $manifest.scripts | Select-Object id,file,order
 Write-Output 'Validación local correcta; estado de SQL desconocido.'
 return
}
$integrationTarget=$Server -ceq '(localdb)\MSSQLLocalDB' -and $Database -ceq 'UGP_Integracion_Test'
if(!$Server -or (!$integrationTarget -and $Database -notmatch '^UGP_Reservas_Test_[A-Za-z0-9_]+$')){throw 'Destino incompleto o no reconocido'}
if(@($manifest.targets | Where-Object {$_.server -ceq $Server -and $_.database -ceq $Database}).Count -ne 1){throw 'Destino no registrado en manifiesto revisado'}
if($Mode -eq 'apply' -and $Authorization -cne "APPLY:$Server/$Database"){throw 'Autorización explícita incorrecta'}
Add-Type -AssemblyName System.Data
$builder=New-Object System.Data.SqlClient.SqlConnectionStringBuilder
$builder['Data Source']=$Server; $builder['Initial Catalog']=$Database
$builder['Integrated Security']=$true; $builder['Encrypt']=$true; $builder['TrustServerCertificate']=$false
# Excepción autorizada solo para desarrollo local, sin credenciales SQL.
if($integrationTarget -and $builder.IntegratedSecurity){$builder['Encrypt']=$false}
$connection=New-Object System.Data.SqlClient.SqlConnection $builder.ConnectionString
function Command([string]$sql){$c=$connection.CreateCommand();$c.CommandTimeout=120;$c.CommandText=$sql;return $c}
function Execute([string]$sql){$c=Command $sql;try{[void]$c.ExecuteNonQuery()}finally{$c.Dispose()}}
try{
 $connection.Open()
 $c=Command 'SELECT DB_NAME()'; if($c.ExecuteScalar() -cne $Database){throw 'Base efectiva distinta'}; $c.Dispose()
 if($Mode -eq 'apply'){
  Execute "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'UGP:Governance',@LockMode='Exclusive',@LockOwner='Session',@LockTimeout=0; IF @r<0 THROW 51000,'Otro despliegue activo',1;"
 }
 $c=Command "SELECT COUNT(*) FROM sys.tables WHERE object_id=OBJECT_ID(N'dbo.SchemaVersion')"
 $exists=[int]$c.ExecuteScalar();$c.Dispose()
 if(!$exists -and $Mode -eq 'apply'){
  $c=Command 'SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0';$count=[int]$c.ExecuteScalar();$c.Dispose()
  if($count -ne 0){throw 'Bootstrap permitido solo en base vacía; adopción existente requiere revisión'}
  Execute (Get-Content "$PSScriptRoot/Gobernanza.sql" -Raw)
  $exists=1
 }
 $applied=@{}
 if($exists){
  $c=Command 'SELECT ScriptId,Checksum FROM dbo.SchemaVersion';$reader=$c.ExecuteReader()
  while($reader.Read()){$applied[$reader.GetString(0)]=$reader.GetString(1)};$reader.Close();$c.Dispose()
 }
 foreach($key in $applied.Keys){if(!$ids.ContainsKey($key)){throw 'Versión SQL desconocida'}}
 $gap=$false
 foreach($s in $manifest.scripts){
  if($applied.ContainsKey($s.id)){
   if($gap -or $applied[$s.id] -ne $s.sha256){throw 'Historial alterado o fuera de orden'}
   Write-Output "$($s.id): aplicada";continue
  }
  $gap=$true; Write-Output "$($s.id): pendiente"
  if($Mode -ne 'apply'){continue}
  $sql=Get-Content -LiteralPath "$PSScriptRoot/$($s.file)" -Raw
  # Ambos scripts revisados tienen exactamente un COMMIT. El registro entra en él.
  if(([regex]::Matches($sql,'(?im)^COMMIT;\s*$')).Count -ne 1){throw 'Contrato transaccional desconocido'}
  $record="INSERT dbo.SchemaVersion VALUES (@script,@hash,SYSUTCDATETIME(),N'Applied',DATEDIFF_BIG(millisecond,@started,SYSUTCDATETIME()));`nCOMMIT;"
  $sql=[regex]::Replace($sql,'(?im)^COMMIT;\s*$',$record)
  $c=Command 'SELECT SYSUTCDATETIME()';$started=$c.ExecuteScalar();$c.Dispose()
  try{
   $c=Command "EXEC sys.sp_set_session_context N'UGP.BaseRevisada',@db; EXEC sys.sp_set_session_context N'UGP.AutorizacionDDL',N'UGP_0002_REVISION_2';"
   [void]$c.Parameters.AddWithValue('@db',$Database);[void]$c.ExecuteNonQuery();$c.Dispose()
   foreach($batch in [regex]::Split($sql,'(?im)^GO\s*\r?$')){
    if(!$batch.Trim()){continue}
    $c=Command $batch
    [void]$c.Parameters.AddWithValue('@script',[string]$s.id);[void]$c.Parameters.AddWithValue('@hash',[string]$s.sha256)
    [void]$c.Parameters.AddWithValue('@started',$started)
    try{[void]$c.ExecuteNonQuery()}finally{$c.Dispose()}
   }
  }catch{
   try{
    Execute 'IF @@TRANCOUNT>0 ROLLBACK;'
    $c=Command 'INSERT dbo.SchemaExecutionError VALUES(NEWID(),@script,SYSUTCDATETIME(),@number)'
    [void]$c.Parameters.AddWithValue('@script',[string]$s.id)
    [void]$c.Parameters.AddWithValue('@number',[int]$(if($_.Exception -is [System.Data.SqlClient.SqlException]){$_.Exception.Number}else{-1}))
    [void]$c.ExecuteNonQuery();$c.Dispose()
   }catch{Write-Warning 'No fue posible persistir el error; revisar conexión y resultado del commit.'}
   throw 'Aplicación detenida; revisar historial y registro de error antes de reintentar.'
  }
 }
}finally{$connection.Dispose()}
