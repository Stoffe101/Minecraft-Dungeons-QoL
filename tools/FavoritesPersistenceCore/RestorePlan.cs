namespace FavoritesPersistenceCore;

sealed record SavedBinding(SavedLocator Locator, Guid ItemId, bool Favorite, string StateDigest);
sealed record FavoriteJournal(int Version, Guid Hero, string SaveGeneration, SavedBinding[] Bindings);
sealed record RestorePlan(bool CanRestoreSavedBindings, IReadOnlyDictionary<SavedLocator, SavedBinding> Bindings, string? Reason)
{
    // No recovery by similar item state, matching names/power, or a previous index.
    // This result never authorizes live salvage or proves runtime identity.
    // The native adapter MUST independently prove the runtime->saved locator mapping.
    internal static RestorePlan Verify(SaveSnapshot snapshot, FavoriteJournal journal) {
        RestorePlan Refuse(string reason) => new(false, new System.Collections.ObjectModel.ReadOnlyDictionary<SavedLocator, SavedBinding>(new Dictionary<SavedLocator, SavedBinding>()), reason);
        if (journal.Version != 1 || journal.Hero != snapshot.Hero || journal.Hero == Guid.Empty) return Refuse("Journal version or hero mismatch.");
        if (!SaveSnapshot.DigestValid(journal.SaveGeneration) || !string.Equals(journal.SaveGeneration, snapshot.Generation, StringComparison.OrdinalIgnoreCase)) return Refuse("Save generation mismatch.");
        if (journal.Bindings == null || journal.Bindings.Length != snapshot.Records.Count) return Refuse("Incomplete item mapping.");
        var bindings = new Dictionary<SavedLocator, SavedBinding>(); var ids = new HashSet<Guid>();
        foreach (var binding in journal.Bindings) {
            if (binding == null || binding.ItemId == Guid.Empty || !ids.Add(binding.ItemId) || !bindings.TryAdd(binding.Locator, binding)
                || !snapshot.Records.TryGetValue(binding.Locator, out var record) || binding.StateDigest != record.StateDigest)
                return Refuse("Duplicate, missing, or changed item binding.");
        }
        return new(true, new System.Collections.ObjectModel.ReadOnlyDictionary<SavedLocator, SavedBinding>(bindings), null);
    }
}
