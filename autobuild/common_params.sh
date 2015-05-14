#!/bin/bash

export build_internal_version="$GIT_REVISION"
if [ "$build_internal_version" == "" ]; then
  export build_internal_version="dev"
fi

export major_minor_version="2.9.0"
export build_external_version="$major_minor_version"
export bundle_version_short_string="$build_external_version"
export bundle_version="$build_internal_version"
export build_full_version="${build_external_version}.${build_internal_version}"

export project_dir="artifacts/generated_build/Unity-iPhone.xcodeproj"
export project_file="$project_dir/project.pbxproj"
export solution_dir="artifacts/generated_build"
export infoplist_dir="$solution_dir"
export info_plist_filename_no_ext="Info"
export build_version_file="$solution_dir/version.txt"
export scheme="Unity-iPhone"
export skip_stamp_version="true"
