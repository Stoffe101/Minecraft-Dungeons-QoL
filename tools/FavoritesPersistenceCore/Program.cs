using FavoritesPersistenceCore;

if (args.SequenceEqual(new[] { "--self-test" })) { PersistenceTests.Run(); return 0; }
if (args.Length == 3 && args[0] == "--inspect-decoded-json") {
    var snapshot = SaveSnapshot.Parse(File.ReadAllBytes(args[1]), args[2]);
    Console.WriteLine("[OK] Save schema accepted. " + string.Join(", ", Enum.GetValues<ItemLocation>().Select(x => x + "=" + snapshot.Records.Keys.Count(k => k.Location == x))));
    Console.WriteLine("No identity binding or game/save writes performed."); return 0;
}
Console.Error.WriteLine("Source-only save/sidecar contracts. No game integration. Usage: --self-test OR --inspect-decoded-json <private decoded copy> <original file SHA256>");
return 2;
