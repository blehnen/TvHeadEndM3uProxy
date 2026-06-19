pipeline {
    agent none

    options {
        timestamps()
        buildDiscarder(logRotator(numToKeepStr: '10'))
        disableConcurrentBuilds()
        timeout(time: 30, unit: 'MINUTES')
    }

    environment {
        DOTNET_CLI_TELEMETRY_OPTOUT = '1'
        DOTNET_NOLOGO               = 'true'
    }

    stages {
        stage('Build & Test') {
            // The 'docker'-labeled agent image already ships the .NET 10 SDK, so we
            // run dotnet directly via sh (matching the reference DotNetWorkQueue
            // Jenkinsfile). We do NOT use docker.image().inside{} or `docker build`
            // here — the agent has no Docker CLI, and image building/publishing is
            // handled entirely by GitHub Actions (.github/workflows/publish.yml).
            agent { label 'docker' }
            steps {
                sh 'dotnet restore Source/TvHeadEndM3uProxy.sln'
                sh 'dotnet build Source/TvHeadEndM3uProxy.sln -c Release --no-restore'
                sh 'dotnet test Source/TvHeadEndM3uProxy.sln -c Release --no-build --logger "console;verbosity=normal"'
            }
            post {
                always {
                    echo 'Build & Test stage complete'
                }
            }
        }
    }

    post {
        success {
            echo 'Pipeline succeeded: solution built and all tests passed.'
        }
        failure {
            echo 'Pipeline failed. Check stage logs above.'
        }
        always {
            cleanWs()
        }
    }
}
