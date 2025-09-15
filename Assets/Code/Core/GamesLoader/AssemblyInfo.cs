using System.Runtime.CompilerServices;

// Allow test assembly to access internal members
[assembly: InternalsVisibleTo("Core.ShortGame.Tests")]

// Optionally allow mock assembly access too
[assembly: InternalsVisibleTo("Core.ShortGame.Tests.Mocks")]

// For Unity dynamic proxy generation (if needed for mocking)
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
