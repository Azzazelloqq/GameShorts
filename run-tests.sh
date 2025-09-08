#!/bin/bash
# ShortGames Core Test Runner - Linux/Mac Shell Script
# Usage: ./run-tests.sh [--filter "filter"] [--unity-path "path"]

# Default values
TEST_FILTER="Code.Core.ShotGamesCore.Tests"
UNITY_PATH="/Applications/Unity/Hub/Editor/2021.3.16f1/Unity.app/Contents/MacOS/Unity"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --filter)
            TEST_FILTER="$2"
            shift 2
            ;;
        --unity-path)
            UNITY_PATH="$2"
            shift 2
            ;;
        --help)
            echo "Usage: $0 [--filter \"filter\"] [--unity-path \"path\"]"
            echo "  --filter       Test filter (default: Code.Core.ShotGamesCore.Tests)"
            echo "  --unity-path   Path to Unity executable"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

echo "========================================"
echo "ShortGames Core Test Runner"
echo "========================================"

# Check if Unity exists
if [ ! -f "$UNITY_PATH" ]; then
    echo "Error: Unity not found at $UNITY_PATH"
    echo "Please specify the correct Unity path using --unity-path parameter"
    
    # Try to find Unity in common locations
    if [ -f "/Applications/Unity/Unity.app/Contents/MacOS/Unity" ]; then
        echo "Found Unity at: /Applications/Unity/Unity.app/Contents/MacOS/Unity"
    elif [ -f "/opt/Unity/Editor/Unity" ]; then
        echo "Found Unity at: /opt/Unity/Editor/Unity"
    fi
    
    exit 1
fi

PROJECT_PATH=$(pwd)
echo "Project Path: $PROJECT_PATH"
echo "Unity Path: $UNITY_PATH"
echo "Test Filter: $TEST_FILTER"
echo ""

# Run tests
echo "Running tests..."
"$UNITY_PATH" \
    -batchmode \
    -nographics \
    -silent-crashes \
    -projectPath "$PROJECT_PATH" \
    -runTests \
    -testPlatform EditMode \
    -testFilter "$TEST_FILTER" \
    -testResults TestResults.xml \
    -logFile TestLog.txt

# Check if test results file was created
if [ -f "TestResults.xml" ]; then
    echo "Tests completed. Results saved to TestResults.xml"
    
    # Parse XML and display summary (requires xmllint)
    if command -v xmllint &> /dev/null; then
        echo ""
        echo "Test Summary:"
        
        TOTAL=$(xmllint --xpath "string(/test-run/@total)" TestResults.xml 2>/dev/null)
        PASSED=$(xmllint --xpath "string(/test-run/@passed)" TestResults.xml 2>/dev/null)
        FAILED=$(xmllint --xpath "string(/test-run/@failed)" TestResults.xml 2>/dev/null)
        SKIPPED=$(xmllint --xpath "string(/test-run/@skipped)" TestResults.xml 2>/dev/null)
        DURATION=$(xmllint --xpath "string(/test-run/@duration)" TestResults.xml 2>/dev/null)
        
        echo "  Total: $TOTAL"
        echo "  Passed: $PASSED"
        echo "  Failed: $FAILED"
        echo "  Skipped: $SKIPPED"
        echo "  Duration: $DURATION seconds"
        
        # Show failed tests if any
        if [ "$FAILED" -gt 0 ]; then
            echo ""
            echo "Failed Tests:"
            xmllint --xpath "//test-case[@result='Failed']/@name" TestResults.xml 2>/dev/null | \
                sed 's/name="//g' | sed 's/"//g' | sed 's/^/  - /'
        fi
    else
        echo "Install xmllint for detailed test results parsing"
    fi
else
    echo "Warning: Test results file not found. Check TestLog.txt for details."
fi

# Display log file location
echo ""
echo "Log file: TestLog.txt"

# Exit with appropriate code
if [ -f "TestResults.xml" ]; then
    if command -v xmllint &> /dev/null; then
        FAILED=$(xmllint --xpath "string(/test-run/@failed)" TestResults.xml 2>/dev/null)
        if [ "$FAILED" -gt 0 ]; then
            exit 1
        fi
    fi
fi

exit 0