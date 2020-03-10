#!/bin/bash
# Updates the application manifest to include the latest version number as specified by
# the latest git tag and commit hash.
cat "../ApplicationPackageRoot/ApplicationManifest.xml.template" | sed -i 's/{VERSION}/'"${git describe --tags}"'/g' > "../ApplicationPackageRoot/ApplicationManifest.xml"
