import sys
import os
import shutil

sys.path.append("../../Common/build/python")
import unitybuild
import unity

def main():
	
	buildParams = unitybuild.BuildParams()

	buildParams.projectPath = os.path.abspath("../PhotonExample")
	buildParams.projectName = "PhotonExample"
	buildParams.baseVersion = "1.0.0"
	
	# a few handy debugging features
	buildParams.skipClean = False
	buildParams.skipBuild = False
	buildParams.skipPackage = False
	
	# run the build
	buildParams.validate()

	shutil.rmtree(buildParams.artifactsPath, True)
	os.mkdir(buildParams.artifactsPath)

	unitybuild.stampVersionFile("../PhotonExample/Assets/Resources/Version.txt", buildParams._version)

	unity.buildFromEditorScript(buildParams.projectPath, buildParams.args.buildEditorScript, "/Applications/Unity5.0.1/Unity.app/Contents/MacOS/Unity")

	return

main()

