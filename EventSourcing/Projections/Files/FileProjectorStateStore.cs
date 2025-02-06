using System.Text.Json;

namespace Basses.SimpleEventStore.Projections.Files;

public class FileProjectorStateStore : IProjectorStateStore
{
    private readonly string _fileDirectoryPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public FileProjectorStateStore(string fileDirectoryPath)
    {
        _fileDirectoryPath = fileDirectoryPath;
        Directory.CreateDirectory(_fileDirectoryPath);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };
    }

    public async Task<ProjectorProcessingState> GetProcessingState(IProjector projector)
    {
        var filename = GenerateFilename(projector);

        if (!File.Exists(filename))
        {
            throw new NotFoundException($"Projector state not found, ID: {projector.Id}, Name: {projector.Name}");
        }

        var json = await File.ReadAllTextAsync(filename);
        var state = JsonSerializer.Deserialize<ProjectorProcessingState>(json) ?? new ProjectorProcessingState(DateTimeOffset.MinValue);

        return state;
    }

    public async Task SaveProcessingState(IProjector projector, ProjectorProcessingState state)
    {
        var json = JsonSerializer.Serialize(state, _jsonOptions);
        var filename = GenerateFilename(projector);
        await File.WriteAllTextAsync(filename, json);
    }

    public async Task UpsertProjector(IProjector projector)
    {
        var existingFilename = Directory.EnumerateFiles(_fileDirectoryPath).FirstOrDefault(f => f.Contains(projector.Id.ToString()));
        var newFilename = GenerateFilename(projector);

        if (existingFilename == null)
        {
            var state = new ProjectorProcessingState(DateTimeOffset.MinValue);
            var json = JsonSerializer.Serialize(state, _jsonOptions);
            await File.WriteAllTextAsync(newFilename, json);
            return;
        }

        if (newFilename != existingFilename)
        {
            var existingState = await File.ReadAllTextAsync(existingFilename);
            await File.WriteAllTextAsync(newFilename, existingState);
            File.Delete(existingFilename);
        }
    }

    private string GenerateFilename(IProjector projector)
    {
        var filename = $"{projector.Id}_{projector.Name}.json".Replace(' ', '-');
        return Path.Combine(_fileDirectoryPath, filename);
    }
}
