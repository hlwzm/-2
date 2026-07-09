namespace Jx3.AdminWeb;

record BanRequest(string Reason);
record CreateAnnouncement(string Title, string Content);
record ProfileUpdate(string? PlayerName, int? Level, ulong? Gold, ulong? Gem);
record SendMailRequest(string Title, string Content, string PlayerIds, ulong Gold, uint ItemId, int ItemCount);
record GrantItemRequest(ulong PlayerId, uint ItemId, int Count, string Reason);
record CurrencyUpdateRequest(ulong PlayerId, string Currency, int Amount, string Reason);
record ReportActionRequest(string Action, string Reason);
record Announcement { public ulong Id { get; set; } public string Title { get; set; } = ""; public string Content { get; set; } = ""; public DateTime CreatedAt { get; set; } public string Author { get; set; } = ""; }
record MailRecord { public ulong Id { get; set; } public string Title { get; set; } = ""; public string Content { get; set; } = ""; public string PlayerIds { get; set; } = ""; public ulong Gold { get; set; } public uint ItemId { get; set; } public int ItemCount { get; set; } public DateTime SentAt { get; set; } }
record ReportItem { public ulong Id { get; set; } public string ReporterName { get; set; } = ""; public string TargetName { get; set; } = ""; public string ReportType { get; set; } = ""; public string ReportTypeName { get; set; } = ""; public string Detail { get; set; } = ""; public string Status { get; set; } = ""; public string CreatedAt { get; set; } = ""; public string ProcessedBy { get; set; } = ""; public string ProcessedAt { get; set; } = ""; }
