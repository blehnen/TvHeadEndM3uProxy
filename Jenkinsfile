pipeline {
    agent none

    options {
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
            agent { label 'docker' }
            steps {
                script {
                    docker.image('mcr.microsoft.com/dotnet/sdk:10.0').inside {
                        sh 'dotnet restore Source/TvHeadEndM3uProxy.sln'
                        sh 'dotnet test Source/TvHeadEndM3uProxy.sln -c Release --logger "console;verbosity=normal"'
                    }
                }
            }
            post {
                always {
                    echo 'Build & Test stage complete'
                }
            }
        }

        stage('Docker Build') {
            agent { label 'docker' }
            steps {
                sh "docker build -t tvheadend-m3u-proxy:${BUILD_NUMBER} ."
            }
            post {
                always {
                    sh "docker image rm -f tvheadend-m3u-proxy:${BUILD_NUMBER} || true"
                }
            }
        }
    }

    post {
        success {
            echo "Pipeline succeeded. Image tvheadend-m3u-proxy:${BUILD_NUMBER} built and tested."
        }
        failure {
            echo 'Pipeline failed. Check stage logs above.'
        }
        always {
            cleanWs()
        }
    }
}
