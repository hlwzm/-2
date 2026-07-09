namespace Jx3.MockServer.Data;

public class TradeListing
{
    public ulong Id { get; set; }
    public ulong SellerPid { get; set; }
    public string SellerName { get; set; } = "";
    public uint ItemId { get; set; }
    public string ItemName { get; set; } = "";
    public int ItemQuality { get; set; }
    public int Count { get; set; }
    public ulong UnitPrice { get; set; }
    public DateTime ListedAt { get; set; }
    public bool Sold { get; set; }
}

public class TradeStore
{
    private static readonly Lazy<TradeStore> _instance = new(() => new TradeStore());
    public static TradeStore Instance => _instance.Value;
    private readonly List<TradeListing> _listings = new();
    private ulong _nextId = 1;

    public TradeListing? AddListing(ulong sellerPid, string sellerName, uint itemId, string itemName, int itemQuality, int count, ulong unitPrice)
    {
        lock (_listings)
        {
            var l = new TradeListing { Id = _nextId++, SellerPid = sellerPid, SellerName = sellerName, ItemId = itemId, ItemName = itemName, ItemQuality = itemQuality, Count = count, UnitPrice = unitPrice, ListedAt = DateTime.UtcNow };
            _listings.Add(l);
            return l;
        }
    }

    public List<TradeListing> Search(string keyword, int quality, int page, int pageSize)
    {
        lock (_listings)
        {
            var q = _listings.Where(l => !l.Sold);
            if (!string.IsNullOrEmpty(keyword)) q = q.Where(l => l.ItemName.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            if (quality > 0) q = q.Where(l => l.ItemQuality == quality);
            return q.OrderBy(l => l.UnitPrice).Skip((page - 1) * pageSize).Take(pageSize).ToList();
        }
    }

    public int SearchCount(string keyword, int quality)
    {
        lock (_listings)
        {
            var q = _listings.Where(l => !l.Sold);
            if (!string.IsNullOrEmpty(keyword)) q = q.Where(l => l.ItemName.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            if (quality > 0) q = q.Where(l => l.ItemQuality == quality);
            return q.Count();
        }
    }

    public TradeListing? GetById(ulong id) { lock (_listings) { return _listings.FirstOrDefault(l => l.Id == id); } }

    public bool CancelListing(ulong id, ulong sellerPid)
    {
        lock (_listings)
        {
            var l = _listings.FirstOrDefault(x => x.Id == id && x.SellerPid == sellerPid && !x.Sold);
            if (l == null) return false;
            l.Sold = true; return true;
        }
    }

    public List<TradeListing> GetBySeller(ulong sellerPid)
    {
        lock (_listings) { return _listings.Where(l => l.SellerPid == sellerPid).OrderByDescending(l => l.ListedAt).ToList(); }
    }

    public bool TryBuy(ulong id)
    {
        lock (_listings)
        {
            var l = _listings.FirstOrDefault(x => x.Id == id && !x.Sold);
            if (l == null) return false;
            l.Sold = true; return true;
        }
    }

    public ulong ClaimGold(ulong sellerPid)
    {
        lock (_listings)
        {
            ulong total = 0; foreach (var l in _listings.Where(x => x.SellerPid == sellerPid && x.Sold)) total += l.UnitPrice * (ulong)l.Count;
            _listings.RemoveAll(l => l.SellerPid == sellerPid && l.Sold);
            return total;
        }
    }
}
