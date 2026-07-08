using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Jx3.Core
{
    /// <summary>商城物品</summary>
    [Serializable]
    public class ShopItem
    {
        public uint ShopItemId;
        public string Name = "";
        public uint ItemId;
        public uint ItemCount = 1;
        public uint PriceType; // 0=金币 1=绑定金币 2=通宝
        public ulong Price;
        public int Stock = -1;
        public int DailyLimit = -1;
        public int WeeklyLimit = -1;
        public float Discount = 1.0f;
        public uint Category;
        public ulong DiscountedPrice => (ulong)(Price * Discount);
        public bool IsOnSale => Discount < 1.0f;
    }

    /// <summary>限时折扣商品</summary>
    [Serializable]
    public class DiscountShopItem : ShopItem
    {
        public DateTime StartTime;
        public DateTime EndTime;
        public string DiscountTag = "";
        public bool IsActive => DateTime.Now >= StartTime && DateTime.Now <= EndTime;
        public float RemainingHours => (float)(EndTime - DateTime.Now).TotalHours;
    }

    /// <summary>每日特惠商品</summary>
    [Serializable]
    public class DailySpecialItem : ShopItem
    {
        public string DayOfWeek = ""; // "周一","周二"...或"每日"
        public bool IsClaimed;
        public int PurchasedToday;
        public int MaxPerDay = 1;
    }

    /// <summary>货币兑换配置</summary>
    [Serializable]
    public class CurrencyExchangeConfig
    {
        public float TongbaoToGoldRate = 100f;  // 1通宝=100金币
        public ulong MinExchange = 10;           // 最低10通宝
        public ulong MaxExchange = 10000;        // 最高10000通宝
        public float ExchangeFeeRate = 0.02f;    // 2%兑换手续费

        public ulong CalculateGold(ulong tongbao)
        {
            return (ulong)(tongbao * TongbaoToGoldRate);
        }

        public ulong CalculateFee(ulong tongbao)
        {
            return (ulong)(tongbao * TongbaoToGoldRate * ExchangeFeeRate);
        }

        public ulong CalculateNetGold(ulong tongbao)
        {
            return CalculateGold(tongbao) - CalculateFee(tongbao);
        }
    }

    /// <summary>商城管理器 - 单例, 处理商城所有逻辑</summary>
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; } = null!;

        // ===== 数据缓存 =====
        private readonly List<ShopItem> _shopItems = new();
        private readonly List<DiscountShopItem> _discountItems = new();
        private readonly List<DailySpecialItem> _dailySpecials = new();
        private CurrencyExchangeConfig _exchangeConfig = new();
        private string _lastGiftCode = "";

        // ===== 公共属性 =====
        public IReadOnlyList<ShopItem> ShopItems => _shopItems.AsReadOnly();
        public IReadOnlyList<DiscountShopItem> DiscountItems => _discountItems.AsReadOnly();
        public IReadOnlyList<DailySpecialItem> DailySpecials => _dailySpecials.AsReadOnly();
        public CurrencyExchangeConfig ExchangeConfig => _exchangeConfig;

        // ===== 事件 =====
        public event Action? OnShopDataChanged;
        public event Action<string>? OnError;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            InitializeMockDiscounts();
        }

        // ==================== 初始化模拟限时折扣数据 ====================

        private void InitializeMockDiscounts()
        {
            _discountItems.Add(new DiscountShopItem
            {
                ShopItemId = 5001, Name = "凤凰于飞·衣", ItemId = 4003, ItemCount = 1,
                PriceType = 2, Price = 28800, Discount = 0.6f, Category = 2,
                StartTime = DateTime.Now.AddDays(-1), EndTime = DateTime.Now.AddDays(2),
                DiscountTag = "限时6折·剩",
                DailyLimit = 1, Stock = 50,
            });
            _discountItems.Add(new DiscountShopItem
            {
                ShopItemId = 5002, Name = "玄晶碎片·大", ItemId = 5005, ItemCount = 5,
                PriceType = 0, Price = 50000, Discount = 0.75f, Category = 3,
                StartTime = DateTime.Now.AddDays(-1), EndTime = DateTime.Now.AddDays(1),
                DiscountTag = "限时7.5折·剩",
                DailyLimit = 3, Stock = 200,
            });
            _discountItems.Add(new DiscountShopItem
            {
                ShopItemId = 5003, Name = "修为丹·超", ItemId = 5006, ItemCount = 1,
                PriceType = 1, Price = 20000, Discount = 0.5f, Category = 3,
                StartTime = DateTime.Now.AddDays(-3), EndTime = DateTime.Now.AddDays(3),
                DiscountTag = "半价·剩",
                DailyLimit = 1, Stock = 100,
            });

            // 每日特惠
            var today = DateTime.Now.DayOfWeek;
            for (int i = 0; i < 7; i++)
            {
                _dailySpecials.Add(new DailySpecialItem
                {
                    ShopItemId = (uint)(6001 + i),
                    Name = i switch
                    {
                        0 => "周一特惠·强化材料包",
                        1 => "周二特惠·修为丹礼包",
                        2 => "周三特惠·精炼石礼包",
                        3 => "周四特惠·英雄碎片箱",
                        4 => "周五特惠·金币礼包",
                        5 => "周六特惠·双倍经验符",
                        6 => "周日特惠·随机宝箱",
                    },
                    ItemId = (uint)(8001 + i),
                    ItemCount = 1,
                    PriceType = 1, Price = 8888, Discount = 0.3f, Category = 4,
                    DayOfWeek = ((DayOfWeek)i).ToString(),
                    MaxPerDay = 1,
                    Stock = 100,
                });
            }
        }

        // ==================== 网络消息处理 ====================

        public void HandleMessage(uint msgId, byte[] body)
        {
            using var r = new BinaryReader(new MemoryStream(body));

            switch ((MsgId)msgId)
            {
                case MsgId.SCShopList:
                    HandleShopList(r);
                    break;
                case MsgId.SCShopBuyResult:
                    HandleBuyResult(r);
                    break;
                case MsgId.SCCurrencyUpdate:
                    HandleCurrencyUpdate(r);
                    break;
                case MsgId.SCShopMonthlyUpdate:
                    HandleMonthlyUpdate(r);
                    break;
                case MsgId.SCShopCurrencyExchange:
                    HandleExchangeResult(r);
                    break;
            }
        }

        private void HandleShopList(BinaryReader r)
        {
            _shopItems.Clear();
            int count = r.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                _shopItems.Add(new ShopItem
                {
                    ShopItemId = r.ReadUInt32(),
                    Name = r.ReadString(),
                    PriceType = r.ReadUInt32(),
                    Price = r.ReadUInt64(),
                    DailyLimit = r.ReadInt32(),
                    Discount = r.ReadSingle(),
                    Category = r.ReadUInt32(),
                });
            }
            Debug.Log($"[Shop] 商城列表更新: {count}件商品");
            OnShopDataChanged?.Invoke();
        }

        private void HandleBuyResult(BinaryReader r)
        {
            int code = r.ReadInt32();
            if (code == 0)
            {
                Debug.Log("[Shop] 购买成功");
                OnShopDataChanged?.Invoke();
            }
            else
            {
                string err = r.ReadString();
                OnError?.Invoke(err);
            }
        }

        private void HandleCurrencyUpdate(BinaryReader r)
        {
            uint currencyType = r.ReadUInt32(); // 0=金币 1=绑定金币 2=通宝
            ulong amount = r.ReadUInt64();
            var player = GameManager.Instance.Player;
            switch (currencyType)
            {
                case 0: player.Gold = amount; break;
                case 1: player.BindGold = amount; break;
                case 2: player.Tongbao = amount; break;
            }
            OnShopDataChanged?.Invoke();
        }

        private void HandleMonthlyUpdate(BinaryReader r)
        {
            int days = r.ReadInt32();
            int claimedToday = r.ReadInt32();
            Debug.Log($"[Shop] 月卡更新: {days}天, 今日已领取={claimedToday}");
            OnShopDataChanged?.Invoke();
        }

        private void HandleExchangeResult(BinaryReader r)
        {
            int code = r.ReadInt32();
            if (code == 0)
            {
                ulong goldReceived = r.ReadUInt64();
                ulong fee = r.ReadUInt64();
                uint newTongbao = r.ReadUInt32();
                GameManager.Instance.Player.Tongbao = newTongbao;
                GameManager.Instance.Player.Gold += goldReceived;
                Debug.Log($"[Shop] 货币兑换成功: +{goldReceived}金币, 手续费={fee}");
                OnShopDataChanged?.Invoke();
            }
            else
            {
                string err = r.ReadString();
                OnError?.Invoke(err);
            }
        }

        // ==================== 商城操作 ====================

        /// <summary>请求商城商品列表</summary>
        public void RequestShopList(uint category = 0)
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(category);
            GameManager.Instance.Network.Send((uint)MsgId.CSShopList, ms.ToArray());
        }

        /// <summary>购买普通商城物品</summary>
        public void BuyItem(uint shopItemId, int count = 1)
        {
            var item = _shopItems.FirstOrDefault(i => i.ShopItemId == shopItemId)
                    ?? (ShopItem?)_discountItems.FirstOrDefault(i => i.ShopItemId == shopItemId)
                    ?? _dailySpecials.FirstOrDefault(i => i.ShopItemId == shopItemId);

            if (item == null)
            {
                OnError?.Invoke("商品不存在");
                return;
            }

            ulong cost = item.DiscountedPrice * (ulong)count;
            var player = GameManager.Instance.Player;

            // 检查货币
            bool canAfford = item.PriceType switch
            {
                0 => player.Gold >= cost,
                1 => player.BindGold >= cost,
                2 => player.Tongbao >= cost,
                _ => false,
            };

            if (!canAfford)
            {
                string currencyName = item.PriceType switch { 0 => "金币", 1 => "绑定金币", 2 => "通宝", _ => "未知" };
                OnError?.Invoke($"{currencyName}不足! 需要{cost}{currencyName}");
                return;
            }

            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(GameManager.Instance.Player.PlayerId);
            w.Write(shopItemId);
            w.Write(count);
            GameManager.Instance.Network.Send((uint)MsgId.CSShopBuy, ms.ToArray());
            Debug.Log($"[Shop] 请求购买: itemId={shopItemId}, count={count}");
        }

        // ==================== 充值 ====================

        /// <summary>充值(元宝)</summary>
        public void Recharge(uint tierId)
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(GameManager.Instance.Player.PlayerId);
            w.Write(tierId);
            GameManager.Instance.Network.Send((uint)MsgId.CSShopRecharge, ms.ToArray());
        }

        // ==================== 兑换码 ====================

        /// <summary>使用兑换码</summary>
        public void UseGiftCode(string code)
        {
            _lastGiftCode = code;
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(GameManager.Instance.Player.PlayerId);
            w.Write(code);
            GameManager.Instance.Network.Send((uint)MsgId.CSShopGiftCode, ms.ToArray());
        }

        // ==================== 月卡 ====================

        /// <summary>领取月卡奖励</summary>
        public void ClaimMonthly()
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(GameManager.Instance.Player.PlayerId);
            GameManager.Instance.Network.Send((uint)MsgId.CSShopMonthlyClaim, ms.ToArray());
        }

        // ==================== 货币兑换(元宝→金币) ====================

        /// <summary>通宝兑换金币</summary>
        /// <param name="tongbao">要兑换的通宝数量</param>
        public void ExchangeCurrency(ulong tongbao)
        {
            if (tongbao < _exchangeConfig.MinExchange)
            {
                OnError?.Invoke($"最低兑换{_exchangeConfig.MinExchange}通宝");
                return;
            }
            if (tongbao > _exchangeConfig.MaxExchange)
            {
                OnError?.Invoke($"单次最多兑换{_exchangeConfig.MaxExchange}通宝");
                return;
            }
            if (GameManager.Instance.Player.Tongbao < tongbao)
            {
                OnError?.Invoke($"通宝不足! 需要{tongbao}通宝");
                return;
            }

            // 本地预估(实际以服务器为准)
            ulong estimatedGold = _exchangeConfig.CalculateNetGold(tongbao);
            Debug.Log($"[Shop] 请求兑换: {tongbao}通宝→约{estimatedGold}金币(含2%手续费)");

            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(GameManager.Instance.Player.PlayerId);
            w.Write(tongbao);
            GameManager.Instance.Network.Send((uint)MsgId.CSShopCurrencyExchange, ms.ToArray());
        }

        // ==================== 限时折扣查询 ====================

        /// <summary>获取当前活跃的限时折扣商品</summary>
        public List<DiscountShopItem> GetActiveDiscounts()
        {
            return _discountItems.Where(d => d.IsActive && (d.Stock == -1 || d.Stock > 0)).ToList();
        }

        /// <summary>获取今日特惠商品</summary>
        public List<DailySpecialItem> GetTodaySpecials()
        {
            string today = DateTime.Now.DayOfWeek.ToString();
            return _dailySpecials.Where(d =>
                d.DayOfWeek == today || d.DayOfWeek == "每日").ToList();
        }

        // ==================== 工具方法 ====================

        /// <summary>格式化货币显示</summary>
        public static string FormatCurrency(ulong amount, uint priceType)
        {
            string suffix = priceType switch
            {
                0 => "金",
                1 => "绑金",
                2 => "通宝",
                _ => "",
            };
            if (amount >= 10000) return $"{amount / 10000}万{amount % 10000}{suffix}";
            return $"{amount}{suffix}";
        }

        /// <summary>格式化为可读的剩余时间</summary>
        public static string FormatRemainingTime(float hours)
        {
            if (hours <= 0) return "已结束";
            if (hours < 1) return $"{(int)(hours * 60)}分钟";
            if (hours < 24) return $"{(int)hours}小时";
            return $"{(int)(hours / 24)}天{(int)(hours % 24)}小时";
        }
    }
}