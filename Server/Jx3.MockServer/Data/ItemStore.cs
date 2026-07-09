namespace Jx3.MockServer.Data;

public class ItemDef
{
    public uint Id { get; set; }
    public string Name { get; set; } = "";
    public int Quality { get; set; }
    public uint BasePrice { get; set; }
}

public class InventoryItem
{
    public ulong Uid { get; set; }
    public uint ItemId { get; set; }
    public int Count { get; set; }
    public ItemDef? Def => ItemStore.Instance.GetDef(ItemId);
}

public class ItemStore
{
    private static readonly Lazy<ItemStore> _instance = new(() => new ItemStore());
    public static ItemStore Instance => _instance.Value;
    private readonly List<ItemDef> _defs = new();
    private readonly Dictionary<ulong, List<InventoryItem>> _inventories = new();
    private ulong _nextUid = 1;
    private readonly Random _rng = new();

    public ItemStore()
    {
        string[] names = ["铁矿石", "粗布", "木材", "皮革", "铜锭",
            "丝缘", "精铁", "灵玉", "玄铁", "天蚕丝",
            "紫晶石", "龙鳞", "凤羽", "星雕铁", "冰蚕丝",
            "天外隅铁", "凤凰翎", "龙血石", "星辰砂", "九天玄铁",
            "长生草", "凝神露", "培元丹", "大还丹"];
        for (uint i = 0; i < 24; i++)
            _defs.Add(new ItemDef { Id = i + 1, Name = names[i], Quality = (int)(i / 5) + 1, BasePrice = (uint)((i / 5 + 1) * 100) });
    }

    public ItemDef? GetDef(uint id) => _defs.FirstOrDefault(d => d.Id == id);
    public List<ItemDef> AllDefs() => _defs.ToList();

    public List<InventoryItem> GetInventory(ulong pid)
    {
        lock (_inventories)
        {
            if (!_inventories.ContainsKey(pid))
            {
                var inv = _defs.Select(d => new InventoryItem { Uid = _nextUid++, ItemId = d.Id, Count = _rng.Next(0, 20) }).ToList();
                _inventories[pid] = inv;
            }
            return _inventories[pid].ToList();
        }
    }

    public bool AddItem(ulong pid, uint itemId, int count)
    {
        lock (_inventories)
        {
            if (!_inventories.ContainsKey(pid)) _inventories[pid] = new();
            var inv = _inventories[pid];
            var existing = inv.FirstOrDefault(i => i.ItemId == itemId);
            if (existing != null) existing.Count += count;
            else inv.Add(new InventoryItem { Uid = _nextUid++, ItemId = itemId, Count = count });
            return true;
        }
    }

    public bool RemoveItem(ulong pid, uint itemId, int count)
    {
        lock (_inventories)
        {
            if (!_inventories.ContainsKey(pid)) return false;
            var inv = _inventories[pid];
            var existing = inv.FirstOrDefault(i => i.ItemId == itemId);
            if (existing == null || existing.Count < count) return false;
            existing.Count -= count;
            return true;
        }
    }
}
