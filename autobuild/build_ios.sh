#!/bin/bash

# https://gist.github.com/949831
# http://blog.carbonfive.com/2011/05/04/automated-ad-hoc-builds-using-xcode-4/

# command line OTA distribution references and examples
# http://nachbaur.com/blog/how-to-automate-your-iphone-app-builds-with-hudson
# http://nachbaur.com/blog/building-ios-apps-for-over-the-air-adhoc-distribution
# http://blog.octo.com/en/automating-over-the-air-deployment-for-iphone/
# http://www.neat.io/posts/2010/10/27/automated-ota-ios-app-distribution.html


#build machine is failing to find PackageApplication... adding this hack to get it in the path:
export PATH=$PATH:/Applications/Xcode.app/Contents/Developer/Platforms/iPhoneOS.platform/Developer/usr/bin

# path information

if [ "$project_dir" == "" ]; then
  echo "Must set project_dir"
  exit 1
fi
if [ "$solution_dir" == "" ]; then
  echo "Must set solution_dir"
  exit 1
fi
if [ "$infoplist_dir" == "" ]; then
  echo "Must set infoplist_dir"
  exit 1
fi

# other project vars 

if [ "$build_config" == "" ]; then
  echo "Must set build_config"
  exit 1
fi

if [ "$build_version_file" == "" ]; then
  echo "Must set build_version_file"
  exit 1
fi

#if [ "$sdk" == "" ]; then
#  echo "Must set sdk"
#  exit 1
#fi

if [ "$scheme" == "" ]; then
  echo "Must set scheme"
  exit 1
fi

if [ "$info_plist_filename_no_ext" == "" ]; then
  echo "Must set info_plist_filename_no_ext"
  exit 1
fi

if [ "$project_app" == "" ]; then
  echo "Must set project_app"
  exit 1
fi

if [ "$delete_artifacts" == "" ]; then
  echo "Must set delete_artifacts"
  exit 1
fi

if [ "$bundle_identifier" == "" ]; then
  echo "Must set bundle_identifier"
  exit 1
fi

if [ "$package_for_appstore" == "" ]; then
  echo "Must set package_for_appstore"
  exit 1
fi

if [ "$mobile_provision" == "" ]; then
  echo "Must set mobile_provision"
  exit 1
fi

if [ "$provisioning_profile" == "" ]; then
  echo "Must set provisioning_profile"
  exit 1
fi

if [ "$major_minor_version" == "" ]; then
  echo "Must set major_minor_version"
  exit 1
fi

if [ "$build_external_version" == "" ]; then
  echo "Must set build_external_version"
  exit 1
fi

if [ "$build_internal_version" == "" ]; then
  echo "Must set build_internal_version"
  exit 1
fi

if [ "$build_full_version" == "" ]; then
  echo "Must set build_full_version"
  exit 1
fi

build_cmds="clean build"
if [ "$package_for_appstore" == "true" ]; then
    build_cmds="clean archive"
fi

info_plist="$infoplist_dir/$info_plist_filename_no_ext.plist"
dd_build_path="Build/Products/$build_config-iphoneos"
project_ipa=${project_app}-${build_full_version}.ipa
project_dsym_zip=${project_app}-${build_full_version}.dSYM.zip
project_archive=$scheme-${build_full_version}.archive.zip

# hardcoded configuration...
workspace="project.xcworkspace"


echo "Building for device: $device scheme: $scheme config: $config"

function fix_unity_project()
{
  # add missing path to unity project
  tmp_project_file="$project_file.tmp"
  orig_project_file="$project_file.orig"
  rm -f "$tmp_project_file"
  cp -f "$project_file" "$orig_project_file"

  token_cnt=0
  search_codesign="*CODE_SIGN_IDENTITY*"

  while IFS='' read -r line || [[ -n "$line" ]]
  do
    if [ $token_cnt -ge 0 ] && [ $token_cnt -lt 2 ]; then
      if [[ $line == $search_codesign ]]; then
        token_cnt=$((token_cnt + 1))
        printf "CODE_SIGN_RESOURCE_RULES_PATH = \"\$(SDKROOT)/ResourceRules.plist\";\n" >>"$tmp_project_file"
      fi
    fi

    printf "%s\n" "$line" >>"$tmp_project_file"
  done < "$project_file"

  cp -f "$tmp_project_file" "$project_file"  
}

function failed()
{
    local error=${1:-Undefined error}
    echo "Failed: $error" >&2
    exit 1
}

function describe_sdks()
{
  #list the installed sdks
  echo "Available SDKs"
  xcodebuild -showsdks  
}

function install_provisioning()
{
  chmod u+x mobileprovisionParser
  uuid=`./mobileprovisionParser -f ${mobile_provision} -o uuid`
  type=`./mobileprovisionParser -f ${mobile_provision} -o type`

  echo "Found UUID $uuid type $type"

  # in case there are no profiles
  mkdir -p "${HOME}/Library/MobileDevice/Provisioning Profiles"

  local output="${HOME}/Library/MobileDevice/Provisioning Profiles/$uuid.mobileprovision"
  if [ ! -f "${output}" ]; then
    echo "Installing mobile provisioning file to $output"
    cp -f "${mobile_provision}" "${output}"
    chmod 644 "${output}"
  else
    echo "Mobile provisioning file already installed"
  fi
}

function increment_version()
{
  chmod +w "$project_file"
  pushd $solution_dir
  agvtool -noscm new-version -all $build_internal_version
  popd

#  cat $info_plist
  pushd $infoplist_dir

  # for some reason the tool above fails to set ver in plist...
  # so force it below
  defaults write `pwd`/$info_plist_filename_no_ext CFBundleVersion "$build_internal_version"
  defaults read `pwd`/$info_plist_filename_no_ext CFBundleVersion

  defaults write `pwd`/$info_plist_filename_no_ext CFBundleShortVersionString "$build_external_version"
  defaults read `pwd`/$info_plist_filename_no_ext CFBundleShortVersionString

  if [ "$bundle_identifier" != "" ]; then
    defaults write `pwd`/$info_plist_filename_no_ext CFBundleIdentifier "$bundle_identifier"
    defaults read `pwd`/$info_plist_filename_no_ext CFBundleIdentifier
  fi

  if [ "$bundle_display_name" != "" ]; then
    defaults write `pwd`/$info_plist_filename_no_ext CFBundleDisplayName "$bundle_display_name"
    defaults read `pwd`/$info_plist_filename_no_ext CFBundleDisplayName
  fi

  if [ "$facebook_id" != "" ]; then
    defaults write `pwd`/$info_plist_filename_no_ext FacebookAppID "$facebook_id"
    defaults read `pwd`/$info_plist_filename_no_ext FacebookAppID
    
    # need to write this to a file cause var substitution doesn't work inside single quotes...
    # defaults write `pwd`/$info_plist_filename_no_ext CFBundleURLTypes '( { CFBundleURLName = ""; CFBundleURLSchemes = ( fb$facebook_id ); } )'
    echo "#!/bin/bash" >tmp.sh
    echo "defaults write `pwd`/$info_plist_filename_no_ext CFBundleURLTypes '( { CFBundleURLName = \"\"; CFBundleURLSchemes = ( fb$facebook_id ); } )'" >>tmp.sh
    chmod +x tmp.sh
    ./tmp.sh
    rm tmp.sh
    defaults read `pwd`/$info_plist_filename_no_ext CFBundleURLTypes
  fi

  plutil -convert xml1 $info_plist_filename_no_ext.plist
  popd
# cat $info_plist
}

function stamp_version()
{
  chmod +w "$build_version_file"
  echo "${build_full_version}" >"$build_version_file"
}

function get_derived_path()
{
  project_derived_data_path=`./derived_path.sh $project_dir`

  if [ $? -ne 0 ]
  then
    failed get_derived_path
  fi

  if [ ! -d "$project_derived_data_path" ]
  then
    failed get_derived_path
  fi

  project_derived_data_path="$project_derived_data_path/$dd_build_path"
}

function sign_app()
{
  pushd "$project_derived_data_path"
  local abs_project_derived_data_path=`pwd`
  popd

  echo "Codesign as \"$provisioning_profile\", embedding provisioning profile $mobile_provision"

#  xcrun PackageApplication "$project_derived_data_path/$project_app" -o "$abs_project_derived_data_path/$project_ipa" --sign "$provisioning_profile" --embed "$mobile_provision" || failed codesign
}

function build_app()
{
  local devired_data_path="$HOME/Library/Developer/Xcode/DerivedData"

  # do a stupid hacky thing to get xcode to generate a scheme file
#  if [ "$generate_scheme" == "true" ]; then
#    open $project_dir
#    sleep 20
#    killall TERM Xcode
#    sleep 5 
#  fi

  echo "Running xcodebuild > xcodebuild_output ..."
#  xcodebuild -verbose -configuration "$build_config" -project "$project_dir" -scheme "$scheme" PROVISIONING_PROFILE=${uuid} $extra_build_cmds $build_cmds 2>&1 | tee tmp.log
  xcodebuild -verbose -configuration "$build_config" -project "$project_dir" -scheme "$scheme" $extra_build_cmds $build_cmds 2>&1 | tee tmp.log

  if [ $? -ne 0 ]
  then
    failed xcodebuild
  fi

  set +e
  grep "The following build commands failed" tmp.log
  grep_ret=$?
  set -e
  rm tmp.log
  if [ "$grep_ret" == "0" ]
  then
    failed xcodebuild
  fi
}

function verify_app()
{
  #verify the resulting app
  codesign -d -vvv --file-list - "$project_derived_data_path/$project_app" || failed verification  
}

function clean_artifacts
{
  if [ "$delete_artifacts" == "true" ]; then
    rm -rf ./artifacts
  fi
  if [ ! -d ./artifacts ]; then
    mkdir ./artifacts
  fi
}

function output_header
{
  echo
  echo "TeamCity Build # $build_full_version"
  echo
}

function copy_artifacts
{
  cp "$project_derived_data_path/$project_ipa" ./artifacts
  if [ -f "./artifacts/$project_dsym_zip" ]; then
    rm -f "./artifacts/$project_dsym_zip"
  fi
  zip -r "./artifacts/$project_dsym_zip" "$project_derived_data_path/$project_app.dSYM"
}

function copy_artifacts_appstore()
{
  curdir=`pwd`

  #potential bug if the script rolls over a day while executing
  archive_folder=~/Library/Developer/Xcode/Archives/`date "+%Y-%m-%d"`
  pushd "$archive_folder"

  #find the most recent archive and zip as build artifact
  archive_name=`ls -ct1 | head -n 1`
  if [ ! -d "${archive_name}" ]; then
    failed copy_artifacts_appstore
  fi

  if [ -f "$curdir/artifacts/$project_archive" ]; then
    rm -f "$curdir/artifacts/$project_archive"
  fi
  zip -r "$curdir/artifacts/$project_archive" "$archive_name"

  echo "Codesign as \"$provisioning_profile\", embedding provisioning profile $mobile_provision"

  #sign build for distribution and package as an .ipa
#  xcrun PackageApplication "$archive_name/Products/Applications/$project_app" -o "$curdir/artifacts/$project_ipa" --sign "$provisioning_profile" --embed "$curdir/$mobile_provision" || failed copy_artifacts_appstore

  if [ -f "$curdir/artifacts/$project_dsym_zip" ]; then
    rm -f "$curdir/artifacts/$project_dsym_zip"
  fi
  zip -r "$curdir/artifacts/$project_dsym_zip" "$archive_name/dSYMs/$project_app.dSYM"

  #clean up the archive so we don't end up with a huge number
  rm -rf "$archive_name"

  popd
}

# turn on shell command logging
set -x

# turn on exit on fail 
set -e

# turn on fail if something in pipeline fails
set -o pipefail

echo
echo "**** Fixing unity project"
fix_unity_project

output_header
echo "**** Clean artifacts"
clean_artifacts

echo
echo "**** Increment Bundle Version"
increment_version

echo
echo "**** Stamp version file"
if [ "$skip_stamp_version" != "true" ]; then
  stamp_version
fi

#echo
#echo "**** Install Provisioning"
#install_provisioning

echo
echo "**** Build"
build_app

echo
echo "**** Get Derived Path"
get_derived_path

if [ "$package_for_appstore" == "true" ]; then
  echo
  echo "**** Copy archive"
  copy_artifacts_appstore
  echo
else
  echo
  echo "**** Package Application"
  sign_app
  echo
  echo "**** Verify"
  verify_app
  echo
  echo "**** Copy artifacts"
  copy_artifacts
  echo
fi

echo
echo "**** Complete!"

