namespace Tests.System;

public record MsBuildOutput(Items Items);
public record Items(IEnumerable<GeneratedImage> GeneratedImages, IEnumerable<GeneratedContainer> GeneratedContainers);
public record GeneratedImage(string Identity, string ImageTag, string FullyQualifiedImageWithTag);
public record GeneratedContainer(string Identity, string ManifestDigest);