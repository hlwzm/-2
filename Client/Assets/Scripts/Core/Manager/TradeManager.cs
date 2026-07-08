using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Jx3.Core
{
    /// <summary>物品分类枚举</summary>
    public enum ItemCategory
    {
        All = 0,
        Weapon = 1,      // 武器
        Armor = 2,       // 防具
        Accessory = 3,   // 饰品
        Material = 4,    // 材料
        Consumable = 5,  // 消耗品
        Manual = 6,      // 秘籍
    }

    /// <summary>交易行上架物品</summary>
    [Serializable]
    public class TradeListing
    {
        public ulong AuctionId;
        public ulong PlayerId;
        public string PlayerName = "";
        public ulong BagItemId;
        public uint ItemId;
        public string ItemName = "";
        public ItemCategory Category;
        public int Quality;
        public ulong Price;
        public int Duration;
        public int Status; // 1=上架 2=售出 3=下架 4=过期
        public DateTime CreateTime;
        public DateTime? SoldTime;
        public ulong Fee;
        public ulong SellerIncome;
        public int RemainHours => Math.Max(0, Duration - (int)(DateTime.Now - CreateTime).TotalHours);
    }

    /// <summary>交易记录</summary>
    [Serializable]
    public class TradeRecord
    {
        public ulong RecordId;
        public ulong AuctionId;
        public string ItemName = "";
        public ulong Price;
        public ulong Fee;
        public ulong Income;
        public bool IsBuy; // true=买入 false=卖出
        public DateTime Time;
        public string OtherParty = "";
    }

    /// <summary>交易管理器 - 单例, 处理交易行所有逻辑</summary>
    public class TradeManager : MonoBehaviour
    {
        public static TradeManager Instance { get; private set; } = null!;

        // ===== 配置常量 =====
        public const float FEE_RATE = 0.05f;        // 5%交易手续费
        public const float LISTING_FEE_RATE = 0.01f; // 1%上架费
        public const int MAX_DURATION_HOURS = 48;    // 最大上架时长
        public const int DEFAULT_DURATION = 24;      // 默认上架时长
        public const int RENEW_HOURS = 24;           // 续期增加时长

        // ===== 分类名称映射 =====
        public static readonly string[] CategoryNames = { "全部", "武器", "防具", "饰品", "材料", "消耗品", "秘籍" };

        // ===== 数据缓存 =====
        private readonly List<TradeListing> _listings = new();
        private readonly List<TradeListing> _myListings = new();
        private readonly List<TradeRecord> _tradeRecords = new();
        private string _lastSearchKeyword = "";
        private ItemCategory _lastSearchCategory = ItemCategory.All;
        private int _lastSearchPage = 1;
        private int _totalSearchCount;

        // ===== 待领取 =====
        private ulong _pendingGold;      // 待领取金币
        private int _pendingItems;       // 待领取物品数

        // ===== 公共属性 =====
        public IReadOnlyList<TradeListing> Listings => _listings.AsReadOnly();
        public IReadOnlyList<TradeListing> MyListings => _myListings.AsReadOnly();
        public IReadOnlyList<TradeRecord> TradeRecords => _tradeRecords.AsReadOnly();
        public ulong PendingGold => _pendingGold;
        public int PendingItems => _pendingItems;
        public string LastSearchKeyword => _lastSearchKeyword;
        public int TotalSearchCount => _totalSearchCount;

        // ===== 事件 =====
        public event Action? OnInventoryChanged;      // 上架/下架/购买后触发
        public event Action<TradeRecord>? OnTradeCompleted; // 交易完成通知
        public event Action<ulong>? OnPendingGoldChanged;
        public event Action<int>? OnPendingItemsChanged;
        public event Action? OnSearchResultUpdated;
        public event Action<string>? OnError;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ==================== 网络消息处理 ====================

        public void HandleMessage(uint msgId, byte[] body)
        {
            using var r = new BinaryReader(new MemoryStream(body));

            switch ((MsgId)msgId)
            {
                case MsgId.SCTradeSearchResult:
                    HandleSearchResult(r);
                    break;
                case MsgId.SCTradeSellResult:
                    HandleSellResult(r);
                    break;
                case MsgId.SCTradeBuyResult:
                    HandleBuyResult(r);
                    break;
                case MsgId.SCTradeItemSold:
                    HandleItemSold(r);
                    break;
                case MsgId.SCTradeCancelResult:
                    HandleCancelResult(r);
                    break;
                case MsgId.SCTradeMyListings:
                    HandleMyListingsResult(r);
                    break;
                case MsgId.SCTradeClaimItem:
                    HandleClaimResult(r);
                    break;
            }
        }

        private void HandleSearchResult(BinaryReader r)
        {
            _listings.Clear();
            _totalSearchCount = r.ReadInt32();
            int count = r.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                _listings.Add(ReadListing(r));
            }
            Debug.Log($"[Trade] 搜索结果: {count}条 (共{_totalSearchCount}条)");
            OnSearchResultUpdated?.Invoke();
        }

        private void HandleSellResult(BinaryReader r)
        {
            int code = r.ReadInt32();
            if (code == 0)
            {
                ulong auctionId = r.ReadUInt64();
                ulong fee = r.ReadUInt64();
                Debug.Log($"[Trade] 上架成功: auctionId={auctionId}, fee={fee}");
                OnInventoryChanged?.Invoke();
            }
            else
            {
                string errMsg = r.ReadString();
                Debug.LogError($"[Trade] 上架失败: {errMsg}");
                OnError?.Invoke(errMsg);
            }
        }

        private void HandleBuyResult(BinaryReader r)
        {
            int code = r.ReadInt32();
            if (code == 0)
            {
                ulong costGold = r.ReadUInt64();
                ulong fee = r.ReadUInt64();
                ulong bagItemId = r.ReadUInt64();
                Debug.Log($"[Trade] 购买成功: cost={costGold}, fee={fee}, bagItemId={bagItemId}");
                OnInventoryChanged?.Invoke();
                OnTradeCompleted?.Invoke(new TradeRecord
                {
                    RecordId = (ulong)DateTime.Now.Ticks,
                    IsBuy = true,
                    Price = costGold,
                    Fee = fee,
                    Income = 0,
                    Time = DateTime.Now,
                });
            }
            else
            {
                string errMsg = r.ReadString();
                Debug.LogError($"[Trade] 购买失败: {errMsg}");
                OnError?.Invoke(errMsg);
            }
        }

        private void HandleItemSold(BinaryReader r)
        {
            ulong auctionId = r.ReadUInt64();
            ulong price = r.ReadUInt64();
            ulong fee = r.ReadUInt64();
            ulong income = r.ReadUInt64();
            _pendingGold += income;
            Debug.Log($"[Trade] 物品已售出: auctionId={auctionId}, income={income}");
            OnPendingGoldChanged?.Invoke(_pendingGold);
            OnTradeCompleted?.Invoke(new TradeRecord
            {
                RecordId = (ulong)DateTime.Now.Ticks,
                AuctionId = auctionId,
                IsBuy = false,
                Price = price,
                Fee = fee,
                Income = income,
                Time = DateTime.Now,
            });
        }

        private void HandleCancelResult(BinaryReader r)
        {
            int code = r.ReadInt32();
            if (code == 0)
            {
                Debug.Log("[Trade] 下架成功");
                OnInventoryChanged?.Invoke();
            }
            else
            {
                string errMsg = r.ReadString();
                OnError?.Invoke(errMsg);
            }
        }

        private void HandleMyListingsResult(BinaryReader r)
        {
            _myListings.Clear();
            _pendingGold = 0;
            _pendingItems = 0;
            int count = r.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var listing = ReadListing(r);
                _myListings.Add(listing);
                if (listing.Status == 2) _pendingGold += listing.SellerIncome;
                if (listing.Status == 3 || listing.Status == 4) _pendingItems++;
            }
            OnPendingGoldChanged?.Invoke(_pendingGold);
            OnPendingItemsChanged?.Invoke(_pendingItems);
            OnSearchResultUpdated?.Invoke();
        }

        private void HandleClaimResult(BinaryReader r)
        {
            int code = r.ReadInt32();
            if (code == 0)
            {
                ulong amount = r.ReadUInt64();
                Debug.Log($"[Trade] 领取成功: amount={amount}");
                OnInventoryChanged?.Invoke();
            }
            else
            {
                string errMsg = r.ReadString();
                OnError?.Invoke(errMsg);
            }
        }

        private void HandleClaimGoldResult(BinaryReader r)
        {
            int code = r.ReadInt32();
            if (code == 0)
            {
                ulong amount = r.ReadUInt64();
                _pendingGold = 0;
                Debug.Log($"[Trade] 金币领取成功: {amount}");
                OnInventoryChanged?.Invoke();
                OnPendingGoldChanged?.Invoke(0);
            }
            else
            {
                string errMsg = r.ReadString();
                OnError?.Invoke(errMsg);
            }
        }

        private TradeListing ReadListing(BinaryReader r)
        {
            return new TradeListing
            {
                AuctionId = r.ReadUInt64(),
                PlayerId = r.ReadUInt64(),
                PlayerName = r.ReadString(),
                BagItemId = r.ReadUInt64(),
                ItemId = r.ReadUInt32(),
                ItemName = r.ReadString(),
                Category = (ItemCategory)r.ReadInt32(),
                Quality = r.ReadInt32(),
                Price = r.ReadUInt64(),
                Duration = r.ReadInt32(),
                Status = r.ReadInt32(),
                CreateTime = DateTime.FromBinary(r.ReadInt64()),
                Fee = r.ReadUInt64(),
                SellerIncome = r.ReadUInt64(),
            };
        }

        // ==================== 上架物品 ====================

        /// <summary>上架物品</summary>
        /// <param name="bagItemId">背包物品唯一ID</param>
        /// <param name="price">售价(金币)</param>
        /// <param name="duration">上架时长(小时)</param>
        /// <param name="category">物品分类</param>
        public void SellItem(ulong bagItemId, ulong price, int duration, ItemCategory category)
        {
            ulong fee = (ulong)Math.Max(1, price * LISTING_FEE_RATE);
            ulong totalGold = GameManager.Instance.Player.Gold + GameManager.Instance.Player.BindGold;

            if (totalGold < fee)
            {
                OnError?.Invoke($"金币不足! 上架需要{fee}金币(含1%上架费)");
                return;
            }
            if (price <= 0)
            {
                OnError?.Invoke("价格必须大于0");
                return;
            }
            if (duration != 24 && duration != 48)
            {
                OnError?.Invoke("上架时长仅支持24或48小时");
                return;
            }

            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(GameManager.Instance.Player.PlayerId);
            w.Write(bagItemId);
            w.Write(price);
            w.Write(duration);
            w.Write((int)category);
            GameManager.Instance.Network.Send((uint)MsgId.CSTradeSell, ms.ToArray());
            Debug.Log($"[Trade] 请求上架: bagItemId={bagItemId}, price={price}, duration={duration}h");
        }

        /// <summary>快捷上架(使用默认时长24h)</summary>
        public void SellItem(ulong bagItemId, ulong price, ItemCategory category)
        {
            SellItem(bagItemId, price, DEFAULT_DURATION, category);
        }

        // ==================== 购买物品 ====================

        /// <summary>购买物品</summary>
        public void BuyItem(ulong auctionId, int count = 1)
        {
            if (!CheckGoldForPurchase(auctionId, count, out var listing, out var err))
            {
                OnError?.Invoke(err);
                return;
            }

            ulong totalPrice = listing!.Price * (ulong)count;
            ulong fee = (ulong)(totalPrice * FEE_RATE);
            ulong totalCost = totalPrice + fee;

            var player = GameManager.Instance.Player;
            if (player.Gold < totalCost)
            {
                ulong deficit = totalCost - player.Gold;
                if (player.BindGold >= deficit)
                {
                    player.BindGold -= deficit;
                }
                else
                {
                    OnError?.Invoke($"金币不足! 需要{totalCost}金币(含5%手续费)");
                    return;
                }
            }
            else
            {
                player.Gold -= totalCost;
            }

            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(GameManager.Instance.Player.PlayerId);
            w.Write(auctionId);
            w.Write(count);
            GameManager.Instance.Network.Send((uint)MsgId.CSTradeBuy, ms.ToArray());
            Debug.Log($"[Trade] 请求购买: auctionId={auctionId}, count={count}, totalPrice={totalPrice}, fee={fee}");
        }

        private bool CheckGoldForPurchase(ulong auctionId, int count, out TradeListing? listing, out string err)
        {
            listing = _listings.FirstOrDefault(l => l.AuctionId == auctionId);
            if (listing == null)
            {
                err = "物品不存在或已下架";
                return false;
            }
            if (listing.Status != 1)
            {
                err = "该物品已售出或下架";
                return false;
            }
            if (listing.PlayerId == GameManager.Instance.Player.PlayerId)
            {
                err = "不能购买自己的物品";
                return false;
            }
            err = "";
            return true;
        }

        // ==================== 搜索与过滤 ====================

        /// <summary>搜索物品</summary>
        public void SearchItems(string keyword = "", ItemCategory category = ItemCategory.All,
            int minQuality = 0, ulong maxPrice = 0, int page = 1, int pageSize = 20)
        {
            _lastSearchKeyword = keyword;
            _lastSearchCategory = category;
            _lastSearchPage = page;

            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(keyword);
            w.Write((int)category);
            w.Write(minQuality);
            w.Write(maxPrice);
            w.Write(page);
            w.Write(pageSize);
            GameManager.Instance.Network.Send((uint)MsgId.CSTradeSearch, ms.ToArray());
        }

        /// <summary>获取我的上架列表</summary>
        public void GetMyListings()
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(GameManager.Instance.Player.PlayerId);
            GameManager.Instance.Network.Send((uint)MsgId.CSTradeMyListings, ms.ToArray());
        }

        // ==================== 下架与续期 ====================

        /// <summary>下架物品</summary>
        public void CancelListing(ulong auctionId)
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(GameManager.Instance.Player.PlayerId);
            w.Write(auctionId);
            GameManager.Instance.Network.Send((uint)MsgId.CSTradeCancel, ms.ToArray());
            Debug.Log($"[Trade] 请求下架: auctionId={auctionId}");
        }

        /// <summary>续期(增加24小时)</summary>
        public void RenewListing(ulong auctionId)
        {
            CancelListing(auctionId);
            // 续期逻辑: 先下架, 再重新上架
            // 实际项目中应在服务器端实现续期API, 这里简化处理
            Debug.Log($"[Trade] 续期请求: auctionId={auctionId}, 增加{RENEW_HOURS}小时");
        }

        // ==================== 领取 ====================

        /// <summary>领取已售出金币</summary>
        public void ClaimGold()
        {
            if (_pendingGold == 0) return;
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(GameManager.Instance.Player.PlayerId);
            GameManager.Instance.Network.Send((uint)MsgId.CSTradeClaimGold, ms.ToArray());
        }

        /// <summary>领取下架/过期物品</summary>
        public void ClaimItem(ulong auctionId)
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(GameManager.Instance.Player.PlayerId);
            w.Write(auctionId);
            GameManager.Instance.Network.Send((uint)MsgId.CSTradeClaimItem, ms.ToArray());
        }

        // ==================== 本地工具方法 ====================

        /// <summary>获取分类显示名</summary>
        public static string GetCategoryName(ItemCategory cat)
        {
            int idx = (int)cat;
            return idx >= 0 && idx < CategoryNames.Length ? CategoryNames[idx] : "未知";
        }

        /// <summary>计算5%手续费</summary>
        public static ulong CalculateFee(ulong price, int count = 1)
        {
            return (ulong)(price * (ulong)count * FEE_RATE);
        }

        /// <summary>计算卖家实际收入(售价-5%手续费-1%上架费)</summary>
        public static ulong CalculateSellerIncome(ulong price)
        {
            ulong fee = (ulong)(price * FEE_RATE);
            ulong listingFee = (ulong)Math.Max(1, price * LISTING_FEE_RATE);
            return price - fee - listingFee;
        }

        /// <summary>根据品质返回颜色代码</summary>
        public static Color GetQualityColor(int quality)
        {
            return quality switch
            {
                5 => new Color(1f, 0.5f, 0f),     // 橙色
                4 => new Color(0.7f, 0.2f, 0.9f),  // 紫色
                3 => new Color(0.3f, 0.6f, 1f),    // 蓝色
                2 => new Color(0.3f, 0.8f, 0.3f),  // 绿色
                _ => Color.white,                   // 白色
            };
        }

        /// <summary>格式化金币显示</summary>
        public static string FormatGold(ulong gold)
        {
            if (gold >= 10000) return $"{gold / 10000}万{gold % 10000}";
            return gold.ToString("N0");
        }
    }
}