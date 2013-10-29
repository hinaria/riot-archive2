# riot-archive2

`riot-archive2` is the reference library for Riot Archive Files (RAFs), the file storage format used by League of Legends.

It's used in astralfoxy's Wintermint client, as well as in many other third party programs.

## Example usage

#### Opening and reading from an archive

```csharp
var archive = await Archive.FromFileAsync("archive.raf");
var stream = archive["file.dat"].GetStream();
```

#### Opening and reading from a collection of archives

League of Legends splits its pseudo-filesystem into multiple files. The `ArchiveCollection` offers
easy access to any file within the entire RAF collection. It shares the same API as an `Archive`,
but allows you to operate on an entire collection of archives.

```csharp
var files = Directory.GetFiles("rads", "*.raf", SearchOption.AllDirectories);
var archive = await ArchiveCollection.FromFilesAsync(files);
var stream = archive["file.dat"].GetStream();
```

#### Searching for a file (using a regular expression)

```csharp
// Search for all files that begin with `astralfoxy` (using a regular expression)
var files = archive.GetFiles(@"^astralfoxy");

// Alternatively, you can iterate through the list of files...
var files = archive.Files.Where(x => x.Name == "astralfoxy.dat");
```

#### Creating an archive

```csharp
var archive = new WriteOnlyArchive();
archive.SetOutput("output.raf");

// write some files to the archive
await archive.WriteAsync("astralfoxy.png", stream);
await archive.WriteAsync("wintermint.exe", stream2);
await archive.WriteAsync("riot-games.png", stream3);

await archive.CommitAsync();
```

## Extensions

`riot-archive2` allows you to optionally store an MD5 checksum of each file in the archive, which
may be useful in some use cases.

To enable this, set `WriteOnlyArchive.CreateChecksumFile` to `true` when making an archive. When
reading from one, use the `Archive.FromFileAsync(string path, bool readChecksumFile)` overload.

## License

- `riot-archive2` MIT licensed.
- Feel free to use it however you want.
- But I'd love to hear about how you're using it! Email me at `foxy::astralfoxy:com`.
- Please contribute any improvements you make back to this repository.
