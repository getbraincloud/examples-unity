#!/bin/bash

source common_params.sh

export build_config="Release"
export bundle_identifier="com.playbrains.bcunityexample"
export bundle_display_name="BC Example"
export project_app="bcunityexample.app"
export delete_artifacts="false"
export mobile_provision="../../githubPrivate/Authentication/BrainCloud_Example_Unity_AdHoc.mobileprovision"
export provisioning_profile="iPhone Distribution: Playbrains Inc"
export package_for_appstore="true"
export generate_scheme="true"

source build_ios.sh
