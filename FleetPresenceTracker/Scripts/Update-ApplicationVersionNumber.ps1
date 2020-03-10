# Updates the application manifest to include the latest version number as specified by
# the latest git tag and commit hash.
$version = git describe --tags
(Get-Content -path "..\ApplicationPackageRoot\ApplicationManifest.xml.template") -replace '{VERSION}',$version | Out-File -Encoding ASCII "..\ApplicationPackageRoot\ApplicationManifest.xml"
