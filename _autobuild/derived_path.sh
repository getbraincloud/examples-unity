#!/bin/bash

# finds the derived data path for the given xcode workspace and spits it out to the console
# exits with non-zero if unable to find the dir

function usage()
{
  echo "derived_path.sh <xcode_project>"
  exit 1
}

function check_derived_path()
{
  local wspath=`defaults read "${PWD}/info" WorkspacePath`
  if [ "$project_dir" == "$wspath" ]; then
    echo "${PWD}"
    exit 0
  fi
}

#set -x
set -e

project_dir="$1"
project_name=""

#echo "Xcode project $project_dir"

if [ "$project_dir" == "" ]; then
  exit 1
fi

if [ ! -d "$project_dir" ]; then
  exit 1
fi

#get the project full path and the project name
pushd $project_dir >/dev/null
project_dir=`pwd`
project_name=`echo ${PWD##*/} | sed 's/.xcodeproj//'`
popd >/dev/null

# find the derived path
pushd "${HOME}/Library/Developer/Xcode/DerivedData" >/dev/null
for D in *; do
  if [ -d "$D" ]; then
    set +e
    dirtest=`echo "$D" | grep $project_name`
    set -e
    if [ "$dirtest" != "" ]; then
      pushd "$D" >/dev/null
      check_derived_path
      popd >/dev/null
    fi
  fi
done

popd >/dev/null

