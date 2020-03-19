def packageAndArchive(buildMode, packageName, exclude) {
	zipFile = "bin\\${packageName}.zip"
	packageDir = "bin\\${packageName}\\"

	bat "IF EXIST ${zipFile} DEL ${zipFile}"
	bat "IF EXIST ${packageDir} RMDIR /S /Q ${packageDir}"

	bat "xcopy bin\\x64\\${buildMode} ${packageDir}"
	if (exclude.length() > 0) {
		bat "del ${packageDir}${exclude}"
	}
	powershell "Add-Type -Assembly System.IO.Compression.FileSystem; [System.IO.Compression.ZipFile]::CreateFromDirectory(\"\$PWD\\${packageDir}\", \"\$PWD\\${zipFile}\")"
	archiveArtifacts artifacts: zipFile, caseSensitive: false, onlyIfSuccessful: true
}

node {
	stage('Checkout') {
		checkout scm
		bat 'git pull --tags'
	}

	stage('Acquire SE') {
		bat 'powershell -File Jenkins/jenkins-grab-se.ps1'
		bat 'IF EXIST GameBinaries RMDIR GameBinaries'
		bat 'mklink /J GameBinaries "C:/Steam/Data/DedicatedServer64/"'
		bat 'dir GameBinaries'
	}

	stage('Acquire NuGet Packages') {
		bat 'nuget restore Torch.sln'
	}

	stage('Build') {
		currentBuild.description = bat(returnStdout: true, script: '@powershell -File Versioning/version.ps1').trim()
		if (env.BRANCH_NAME == "master" || env.BRANCH_NAME == "Patron" || env.BRANCH_NAME == "publictest") {
			buildMode = "Release"
		} else {
			buildMode = "Debug"
		}
		bat "IF EXIST \"bin\" rmdir /Q /S \"bin\""
		bat "IF EXIST \"bin-test\" rmdir /Q /S \"bin-test\""
		bat "\"${tool 'MSBuild'}msbuild\" Torch.sln /p:Configuration=${buildMode} /p:Platform=x64 /t:Clean"
		bat "\"${tool 'MSBuild'}msbuild\" Torch.sln /p:Configuration=${buildMode} /p:Platform=x64"
	}

	stage('Archive') {
		archiveArtifacts artifacts: "bin/x64/${buildMode}/Torch*", caseSensitive: false, fingerprint: true, onlyIfSuccessful: true

		packageAndArchive(buildMode, "torch-server", "Torch.Client*")

		/*packageAndArchive(buildMode, "torch-client", "Torch.Server*")*/
	}

	/* Disabled because they fail builds more often than they detect actual problems
	stage('Test') {
		bat 'IF NOT EXIST reports MKDIR reports'
		bat "\"packages/xunit.runner.console.2.2.0/tools/xunit.console.exe\" \"bin-test/x64/${buildMode}/Torch.Tests.dll\" \"bin-test/x64/${buildMode}/Torch.Server.Tests.dll\" \"bin-test/x64/${buildMode}/Torch.Client.Tests.dll\" -parallel none -xml \"reports/Torch.Tests.xml\""

	    step([
	        $class: 'XUnitBuilder',
	        thresholdMode: 1,
	        thresholds: [[$class: 'FailedThreshold', failureThreshold: '1']],
	        tools: [[
	            $class: 'XUnitDotNetTestType',
	            deleteOutputFiles: true,
	            failIfNotNew: true,
	            pattern: 'reports/*.xml',
	            skipNoTestFiles: false,
	            stopProcessingIfError: true
	        ]]
	    ])
	}
	*/
}
