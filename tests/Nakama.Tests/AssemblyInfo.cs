using Xunit;

// see: https://stackoverflow.com/questions/52389298/howto-resolve-net-test-hangs-on-starting-test-execution-please-wait
[assembly: CollectionBehavior(DisableTestParallelization = true)]
