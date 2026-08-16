// Test execution is deliberately SERIALIZED for this suite.
//
// The application is built around process-wide singletons - Program.DSP_Info, Program.ASIO and
// the static Debug error-reporting state (SuppressInteractiveDialogs / LastError / ErrorReported /
// LastSwallowedError). Many tests must mutate those singletons to set up a scenario, so tests in
// DIFFERENT classes race against each other whenever they run concurrently.
//
// The previous setting, [assembly: Parallelize(Scope = ExecutionScope.MethodLevel)], made the suite
// non-deterministic: repeated runs of an otherwise green suite produced 5, 8 or 9 unrelated failures
// (BuildStreamChains_Depth2/3/4, EventHandlers_AreInvoked_OnAudioAvailable, the Debug Error_WhenSuppressed_*
// tests, TestSampleRateChange_UpdatesFilter, TestTapsSampleRate_Divider10) purely from ordering.
// It also made the wall-clock *_IsFast performance tests trip their thresholds under test-host contention.
//
// Neither ExecutionScope.ClassLevel nor a per-class [DoNotParallelize] fixes this, because the races are
// BETWEEN classes, not within them - only full serialization does.
//
// Cost: the full suite runs in ~3s serialized versus ~1s parallel. That is a trivial price for a suite
// whose result can be trusted, and it makes the wall-clock performance assertions meaningful.
[assembly: DoNotParallelize]
