using System.Security.Cryptography;
using System.Text.Json;

namespace FavoritesPersistenceCore;

// A locator identifies a record WITHIN ONE save generation. It is never an item ID.
enum ItemLocation { Inventory, Storage, Equipment }
readonly record struct SavedLocator(ItemLocation Location, int Index, string EquipmentSlot);
sealed record SavedRecord(SavedLocator Locator, string StateDigest);

sealed class SaveSnapshot
{
    internal Guid Hero { get; }
    internal string Generation { get; }
    internal IReadOnlyDictionary<SavedLocator, SavedRecord> Records { get; }
    SaveSnapshot(Guid hero, string generation, Dictionary<SavedLocator, SavedRecord> records) {
        Hero = hero; Generation = generation;
        Records = new System.Collections.ObjectModel.ReadOnlyDictionary<SavedLocator, SavedRecord>(records);
    }
    internal static bool DigestValid(string? value) => value != null && value.Length == 64 && value.All(Uri.IsHexDigit);
    internal static SaveSnapshot Parse(ReadOnlyMemory<byte> decodedJson, string sourceFileSha256) {
        if (!DigestValid(sourceFileSha256) || decodedJson.Length > 8 * 1024 * 1024) throw new InvalidDataException("Invalid save generation or document bound.");
        using var json = JsonDocument.Parse(decodedJson, new JsonDocumentOptions { MaxDepth = 64 });
        ValidateJson(json.RootElement);
        var root = json.RootElement;
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("uniqueSaveId", out var id)
            || id.ValueKind != JsonValueKind.String || !Guid.TryParse(id.GetString(), out var hero) || hero == Guid.Empty)
            throw new InvalidDataException("A nonempty internal hero GUID is required; a filename is not a fallback.");
        var records = new Dictionary<SavedLocator, SavedRecord>();
        Read("items", ItemLocation.Inventory); Read("storageChestItems", ItemLocation.Storage);
        return new(hero, sourceFileSha256.ToLowerInvariant(), records);
        void Read(string name, ItemLocation domain) {
            if (!root.TryGetProperty(name, out var array) || array.ValueKind != JsonValueKind.Array || array.GetArrayLength() > 306)
                throw new InvalidDataException("Missing or excessive inventory/storage record array.");
            int bagCount = 0;
            foreach (var item in array.EnumerateArray()) {
                if (item.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Item record must be an object.");
                bool indexed = item.TryGetProperty("inventoryIndex", out var index), equipped = item.TryGetProperty("equipmentSlot", out var slot);
                if (indexed == equipped) throw new InvalidDataException("Each record needs exactly one location.");
                SavedLocator locator;
                if (equipped) {
                    if (domain != ItemLocation.Inventory || slot.ValueKind != JsonValueKind.String || !Equipment.Contains(slot.GetString()!))
                        throw new InvalidDataException("Unknown equipment location or equipped storage record.");
                    locator = new(ItemLocation.Equipment, -1, slot.GetString()!);
                } else {
                    if (index.ValueKind != JsonValueKind.Number || !index.TryGetInt32(out var number) || number < 0 || number >= 300 || ++bagCount > 300)
                        throw new InvalidDataException("Inventory/storage index outside this parser's bounded supported range.");
                    locator = new(domain, number, "");
                }
                if (!records.TryAdd(locator, new(locator, CanonicalDigest(item)))) throw new InvalidDataException("Duplicate record locator.");
            }
        }
    }
    static readonly HashSet<string> Equipment = ["ArmorGear", "MeleeGear", "RangedGear", "HotbarSlot1", "HotbarSlot2", "HotbarSlot3"];
    static void ValidateJson(JsonElement element) {
        if (element.ValueKind == JsonValueKind.Object) {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in element.EnumerateObject()) {
                if (!names.Add(field.Name)) throw new InvalidDataException("Duplicate JSON property.");
                ValidateJson(field.Value);
            }
        } else if (element.ValueKind == JsonValueKind.Array) foreach (var item in element.EnumerateArray()) ValidateJson(item);
    }
    // State checks detect corruption or the wrong binding, not physical identity.
    // Preserve every field, nested array order and numeric lexeme, including unknown fields.
    static string CanonicalDigest(JsonElement item) {
        using var bytes = new MemoryStream();
        using (var writer = new Utf8JsonWriter(bytes)) Write(item, writer);
        return Convert.ToHexString(SHA256.HashData(bytes.ToArray())).ToLowerInvariant();
    }
    static void Write(JsonElement value, Utf8JsonWriter writer) {
        if (value.ValueKind == JsonValueKind.Object) {
            writer.WriteStartObject();
            foreach (var p in value.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal)) { writer.WritePropertyName(p.Name); Write(p.Value, writer); }
            writer.WriteEndObject();
        } else if (value.ValueKind == JsonValueKind.Array) {
            writer.WriteStartArray(); foreach (var x in value.EnumerateArray()) Write(x, writer); writer.WriteEndArray();
        } else value.WriteTo(writer);
    }
}
