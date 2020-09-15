$ProgressPreference= "SilentlyContinue"
$password = 'ca5A0---_Gq2DO6Wyo5s_D2uz9kCFrV0.v'
$securePassword = ConvertTo-SecureString $password -AsPlainText -Force
$credentials = New-Object System.Management.Automation.PSCredential ("30f85688-93d1-43b2-9567-bd1a34be3f68", $securePassword)
Add-AzureRmAccount -ServicePrincipal -Tenant '944dcbb3-dbcd-4c57-8167-be76da805171' -Credential $credentials
Select-AzureRmSubscription -SubscriptionId 'c42f13b2-3f25-4a32-a1c1-9bbc323dfa76'
Stop-AzureRmWebApp -Name 'scmm' -ResourceGroupName 'SCMM'
Start-AzureRmWebApp -Name 'scmm' -ResourceGroupName 'SCMM'