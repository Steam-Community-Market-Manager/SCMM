$ProgressPreference= "SilentlyContinue"
$password = 'applicationPassword'
$securePassword = ConvertTo-SecureString $password -AsPlainText -Force
$credentials = New-Object System.Management.Automation.PSCredential ("applicationId", $securePassword)
Add-AzureRmAccount -ServicePrincipal -Tenant 'tenantId' -Credential $credentials
Select-AzureRmSubscription -SubscriptionId 'subscriptionId'
Stop-AzureRmWebApp -Name 'scmm' -ResourceGroupName 'SCMM'
Start-AzureRmWebApp -Name 'scmm' -ResourceGroupName 'SCMM'