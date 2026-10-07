using System.Text;
using System.Text.Json;

namespace FavoritesPersistenceCore;

static class PersistenceTests
{
    internal static void Run() {
        int count = 0;
        const string generation = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        void Check(bool value, string name) { if (!value) throw new Exception(name); count++; }
        void Reject(string json, string name) {
            try { SaveSnapshot.Parse(Encoding.UTF8.GetBytes(json), generation); }
            catch (Exception ex) when (ex is InvalidDataException or JsonException) { count++; return; }
            throw new Exception("Accepted " + name);
        }
        var hero = Guid.Parse("11111111-2222-3333-4444-555555555555");
        string Save(string items, string storage = "[]") => "{\"uniqueSaveId\":\"" + hero + "\",\"items\":" + items + ",\"storageChestItems\":" + storage + "}";
        string item0 = "{\"inventoryIndex\":0,\"type\":\"Sword\",\"power\":1.25,\"enchantments\":[]}";
        var bytes = Encoding.UTF8.GetBytes(Save("[" + item0 + "," + item0.Replace(":0,", ":1,") + ", {\"equipmentSlot\":\"ArmorGear\",\"type\":\"Armor\"}]", "[" + item0 + "]"));
        var snapshot = SaveSnapshot.Parse(bytes, generation);
        Check(snapshot.Records.Count == 4, "All three location domains");
        var i0 = new SavedLocator(ItemLocation.Inventory, 0, ""); var i1 = new SavedLocator(ItemLocation.Inventory, 1, "");
        var s0 = new SavedLocator(ItemLocation.Storage, 0, "");
        var journal = new FavoriteJournal(1, hero, generation, snapshot.Records.Values.Select(x => new SavedBinding(x.Locator, Guid.NewGuid(), x.Locator == i0, x.StateDigest)).ToArray());
        var restored = JsonSerializer.Deserialize<FavoriteJournal>(JsonSerializer.Serialize(journal))!;
        var plan = RestorePlan.Verify(snapshot, restored);
        Check(plan.CanRestoreSavedBindings, "Matching generation sidecar roundtrip");
        Check(plan.Bindings[i0].Favorite && !plan.Bindings[i1].Favorite, "Same type/state items retain independent favorites");
        Check(plan.Bindings[i0].ItemId != plan.Bindings[i1].ItemId, "Duplicate gear keeps distinct project GUIDs");
        Check(plan.Bindings[i0].ItemId != plan.Bindings[s0].ItemId, "Inventory/storage index zero are distinct");
        restored = restored with { Bindings = restored.Bindings.Select(x => x.Locator == i0 ? x with { Favorite = false } : x).ToArray() };
        var unfavorite = RestorePlan.Verify(snapshot, JsonSerializer.Deserialize<FavoriteJournal>(JsonSerializer.Serialize(restored))!);
        Check(unfavorite.CanRestoreSavedBindings && !unfavorite.Bindings[i0].Favorite && unfavorite.Bindings[i0].ItemId == plan.Bindings[i0].ItemId, "Explicit unfavorite preserves identity after journal roundtrip");
        void Block(FavoriteJournal candidate, string name) { var refusal = RestorePlan.Verify(snapshot, candidate); Check(!refusal.CanRestoreSavedBindings && refusal.Bindings.Count == 0, name); }
        Block(journal with { Hero = Guid.NewGuid() }, "Hero switch cannot inherit locks");
        Block(journal with { Version = 2 }, "Unknown journal schema");
        Block(journal with { SaveGeneration = new string('b', 64) }, "Stale save cannot restore by index");
        Block(journal with { SaveGeneration = "" }, "Invalid generation");
        Block(journal with { Bindings = journal.Bindings[..^1] }, "Partial ledger cannot restore saved bindings");
        Block(journal with { Bindings = journal.Bindings.Append(journal.Bindings[0]).ToArray() }, "Excess binding");
        var duplicateId = journal.Bindings.ToArray(); duplicateId[1] = duplicateId[1] with { ItemId = duplicateId[0].ItemId };
        Block(journal with { Bindings = duplicateId }, "Duplicated physical ID");
        var duplicateLocation = journal.Bindings.ToArray(); duplicateLocation[1] = duplicateLocation[1] with { Locator = duplicateLocation[0].Locator };
        Block(journal with { Bindings = duplicateLocation }, "Duplicated location");
        var changedState = journal.Bindings.ToArray(); changedState[0] = changedState[0] with { StateDigest = new string('0', 64) };
        Block(journal with { Bindings = changedState }, "Changed/replaced item state");
        var missingId = journal.Bindings.ToArray(); missingId[0] = missingId[0] with { ItemId = Guid.Empty };
        Block(journal with { Bindings = missingId }, "Empty physical ID");
        Reject(Save("[" + item0 + "," + item0 + "]"), "duplicate inventory indices");
        Reject(Save("[{\"inventoryIndex\":0,\"equipmentSlot\":\"ArmorGear\"}]"), "conflicting locations");
        Reject(Save("[{\"type\":\"Sword\"}]"), "missing locator");
        Reject(Save("[{\"inventoryIndex\":-1}]"), "negative index");
        Reject(Save("[{\"inventoryIndex\":300}]"), "capacity index");
        Reject(Save("[{\"inventoryIndex\":0.5}]"), "fractional index");
        Reject(Save("[{\"inventoryIndex\":\"0\"}]"), "string index");
        Reject(Save("[{\"equipmentSlot\":\"Unknown\"}]"), "unknown equipment");
        Reject(Save("[]", "[{\"equipmentSlot\":\"ArmorGear\"}]"), "equipment in storage");
        Reject(Save("[{\"equipmentSlot\":\"ArmorGear\"},{\"equipmentSlot\":\"ArmorGear\"}]"), "duplicate equipment");
        Reject(Save("[" + item0.Replace("\"type\":\"Sword\"", "\"type\":\"Sword\",\"type\":\"Axe\"") + "]"), "duplicate hidden JSON field");
        Reject(Save("[]").Replace(hero.ToString(), Guid.Empty.ToString()), "empty internal hero ID");
        var reordered = SaveSnapshot.Parse(Encoding.UTF8.GetBytes(Save("[{\"enchantments\":[],\"power\":1.25,\"type\":\"Sword\",\"inventoryIndex\":0}]")), generation);
        Check(reordered.Records[i0].StateDigest == snapshot.Records[i0].StateDigest, "Object property order/whitespace independent state validation");
        var extended = SaveSnapshot.Parse(Encoding.UTF8.GetBytes(Save("[" + item0.Replace("[]}", "[],\"newNativeField\":5}") + "]")), generation);
        Check(extended.Records[i0].StateDigest != snapshot.Records[i0].StateDigest, "Unknown item state retained in validation");
        Check(SaveSnapshot.Parse(Encoding.UTF8.GetBytes(Save("[]")), generation).Records.Count == 0, "Empty inventories accepted");
        Console.WriteLine($"[OK] {count} save/sidecar contract checks; no native runtime integration tested.");
    }
}
