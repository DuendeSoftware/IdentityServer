$certThumbprint = "CCBBF9E1485AF63CE47ABF8E9E648C2504FC319D"
$certStore = New-Object System.Security.Cryptography.X509Certificates.X509Store("Root", "LocalMachine")
$certStore.Open("ReadWrite")
$cert = $certStore.Certificates | Where-Object { $_.Thumbprint -eq $certThumbprint }
if ($cert) {
    $certStore.Remove($cert)
    Write-Output "Certificate removed successfully."
} else {
    Write-Output "Certificate not found."
}
$certStore.Close()