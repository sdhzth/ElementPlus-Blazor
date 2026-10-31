# Element-Blazor 2.14 Release Checklist

Use this checklist before publishing `Element` or companion packages such as `Element.Markdown` and `Element.X`.

## Version and Source

- [ ] Confirm the package version in each packable project.
- [ ] Confirm `CHANGELOG.md` has a section for the release.
- [ ] Confirm the release tag name, for example `v0.10.0`.
- [ ] Confirm the working tree only contains intended release changes.

## Security and Dependencies

- [ ] Run `dotnet list src/Components/Element.csproj package --vulnerable --include-transitive`.
- [ ] Run `dotnet list src/Markdown/Element.Markdown.csproj package --vulnerable --include-transitive`.
- [ ] Run `dotnet list src/X/Element.X.csproj package --vulnerable --include-transitive`.
- [ ] Review `dotnet list src/Components/Element.csproj package --outdated`.
- [ ] Document any accepted dependency risk in the release notes.
- [ ] Review suppressed historical analyzer warnings in `src/Components/Element.csproj` before removing `NoWarn` entries such as `BL0005` and `BL0007`.

## NuGet Metadata

- [ ] `PackageId` is correct.
- [ ] `Title`, `Authors`, `Company`, and `Description` are readable.
- [ ] `PackageProjectUrl`, `RepositoryUrl`, and `RepositoryType` are set.
- [ ] `PackageLicenseExpression` is set to `MIT`.
- [ ] `PackageIcon` points to `LOGO.png` and the icon is included in the package.
- [ ] `PackageReadmeFile` points to an included package readme file.
- [ ] `PackageTags` use searchable package terms.
- [ ] `PackageReleaseNotes` summarize the release.

## Symbols and Source Link

- [ ] `IncludeSymbols` is enabled.
- [ ] `SymbolPackageFormat` is `snupkg`.
- [ ] `Microsoft.SourceLink.GitHub` is referenced with `PrivateAssets="All"`.
- [ ] `PublishRepositoryUrl` is enabled.
- [ ] `EmbedUntrackedSources` is enabled.
- [ ] The `.snupkg` file is produced during `dotnet pack`.

## Package Contents

- [ ] Version comes from each csproj's `<Version>` (currently `0.10.0`); do not pass `-p:Version` to `dotnet pack`.
- [ ] Build first with `--no-incremental`, then pack — `dotnet pack` reuses an up-to-date build, which can put a new
      nuspec version around an assembly whose `AssemblyVersion` is still the old one (measured 2026-10-13).
- [ ] Run `dotnet pack src/Components/Element.csproj -c Release -o nuget`.
- [ ] Run `dotnet pack src/Markdown/Element.Markdown.csproj -c Release -o nuget`.
- [ ] Run `dotnet pack src/X/Element.X.csproj -c Release -o nuget`.
- [ ] Verify both numbers per package: nuspec `<version>` and the packaged `lib/net10.0/*.dll` assembly version.
- [ ] If you repacked the **same** version with different content, purge the matching id from the global packages
      folder before consumer restore, otherwise NuGet silently keeps the cached `.nupkg`.
- [ ] Confirm the `.nupkg` contains `LOGO.png`, `LICENSE`, package readme, XML docs, and static web assets.
- [ ] Confirm the `.snupkg` exists.
- [ ] Install the package into a clean sample app if this is a public release.

## Build Validation

- [ ] Run `dotnet restore Element-Blazor.sln`.
- [ ] Run `dotnet build src/Components/Element.csproj -c Release`.
- [ ] Run `dotnet build src/Markdown/Element.Markdown.csproj -c Release`.
- [ ] Run `dotnet build src/X/Element.X.csproj -c Release`.
- [ ] Run `dotnet build Element-Blazor.sln -c Release`.
- [ ] Run tests or document why tests were skipped.

## Publish

- [ ] Publish `.nupkg` and `.snupkg` together.
- [ ] Verify the NuGet package page renders the readme and icon.
- [ ] Create the GitHub release from `RELEASE_TEMPLATE.md`.
- [ ] Link the changelog and migration notes.
