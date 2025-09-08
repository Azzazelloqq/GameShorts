using UnityEngine;

namespace Code.Core.ShotGamesCore.Tests
{
    /// <summary>
    /// Configuration for ShortGames system testing
    /// </summary>
    public static class TestConfiguration
    {
        /// <summary>
        /// Performance test settings
        /// </summary>
        public static class Performance
        {
            public const float MaxPoolOperationTime = 1.0f;
            public const float MaxGameLoadFromPoolTime = 10.0f;
            public const float MaxGameLoadNewInstanceTime = 100.0f;
            public const float MaxGameSwitchTime = 50.0f;
            public const float MaxPreloadTimePerGame = 200.0f;
            public const int DefaultPoolSize = 3;
            public const int MaxPoolSize = 10;
        }
        
        /// <summary>
        /// Integration test settings
        /// </summary>
        public static class Integration
        {
            public const int TestTimeout = 5000;
            public const int QuickSwitchCount = 10;
            public const int StressTestGameCount = 100;
        }
        
        /// <summary>
        /// Unit test settings
        /// </summary>
        public static class Unit
        {
            public const bool EnableDetailedLogging = false;
            public const bool CleanupAfterEachTest = true;
            public const bool RunPerformanceTests = true;
        }
        
        /// <summary>
        /// CI/CD settings
        /// </summary>
        public static class CI
        {
            public const string TestResultsPath = "TestResults/";
            public const string CoverageReportPath = "Coverage/";
            public const bool GenerateHTMLReport = true;
            public const bool FailOnWarnings = false;
            public const float MinimumCoverage = 0.90f;
        }
        
        /// <summary>
        /// Validates test environment
        /// </summary>
        public static bool ValidateTestEnvironment()
        {
            bool isValid = true;
            
            #if !UNITY_2021_3_OR_NEWER
            Debug.LogWarning("Tests require Unity 2021.3 or newer");
            isValid = false;
            #endif
            
            #if !UNITY_INCLUDE_TESTS
            Debug.LogError("Test framework is not included");
            isValid = false;
            #endif
            
            #if !UNITY_EDITOR && !UNITY_STANDALONE
            Debug.LogWarning("Tests are optimized for Editor and Standalone platforms");
            #endif
            
            return isValid;
        }
        
        /// <summary>
        /// Gets configuration for current environment
        /// </summary>
        public static string GetEnvironmentInfo()
        {
            return $@"
Test Environment Configuration:
================================
Unity Version: {Application.unityVersion}
Platform: {Application.platform}
Is Editor: {Application.isEditor}
Is Playing: {Application.isPlaying}
Target Frame Rate: {Application.targetFrameRate}
System Memory: {SystemInfo.systemMemorySize} MB
Processor Count: {SystemInfo.processorCount}
================================";
        }
    }
}
