namespace CafeMenu.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class EnvironmentMutatingTestCollection
{
    public const string Name = "EnvironmentMutatingTests";
}
