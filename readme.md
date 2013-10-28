# riot-archive2

`riot-archive2` is the reference library for Riot Archive Files (RAFs), the storage format used by the League of Legends game.

It used in astralfoxy's Wintermint client in addition to many other large scale sites.

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

#### Searching for a file

```csharp
// Search for all files that begin with `astralfoxy`
var matches = archive.GetFiles(@"^astralfoxy");
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