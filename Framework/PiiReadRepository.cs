using Basses.SimpleDocumentStore;

namespace UnderstandingEventsourcingExample.Framework;

public class PiiReadRepository(IDocumentStore documentStore)
{
    public async Task<T?> TryGetPiiData<T>(Guid id, int version)
    {
        var piiDataVersions = await documentStore.GetByIdAsync<List<PiiDataWrapper<T>>>(id);
        var piiData = piiDataVersions?
            .OrderBy(x => x.Version)
            .LastOrDefault(x => x.Version <= version);

        if (piiData != null && piiData.Data != null)
        {
            return (T?)piiData.Data;
        }

        return default;
    }
}
