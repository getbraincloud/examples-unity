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
	
	# a few handy debugging features
	buildParams.skipClean = False
	buildParams.skipBuild = False
	buildParams.skipPackage = False
	
	# run the build
	buildParams.validate()

	shutil.rmtree(buildParams.artifactsPath, True)
	os.mkdir(buildParams.artifactsPath)

	unity.buildFromEditorScript(buildParams.projectPath, buildParams.args.buildEditorScript)
	return

main()

