namespace LocalisationGenerator;

public record Config {
	public string ProjectPath { get; init; } = string.Empty;
	public string Namespace { get; init; } = string.Empty;
	public string RootNamespace { get; init; } = string.Empty;
	public string L12NFilesLocation { get; init; } = string.Empty;
	public string DefaultLocale { get; init; } = string.Empty;
}
