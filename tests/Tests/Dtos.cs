namespace Tests;

public record MsBuildOutput(Items Items, Properties Properties);

public record Items(IEnumerable<GeneratedImage> GeneratedImages, IEnumerable<GeneratedContainer> GeneratedContainers);

public record GeneratedImage(string Identity, string ImageTag, string FullyQualifiedImageWithTag);

public record GeneratedContainer(string Identity, string ManifestDigest);

public record Properties(string ComputedFullyQualifiedImageName);