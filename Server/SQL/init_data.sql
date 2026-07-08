-- ===== 初始数据 =====
USE jx3;

-- 英雄模板配置
INSERT INTO hero_template (template_id, name, quality, role_type, attack_type) VALUES
(1001, '李复',   5, 1, 1),
(1002, '秋叶青', 4, 1, 2),
(1003, '陈月',   4, 3, 2),
(1004, '裴元',   4, 3, 2),
(1005, '李承恩', 5, 2, 1),
(1006, '叶英',   5, 1, 1),
(1007, '玄正',   5, 2, 2),
(1008, '渡会',   4, 1, 1);

-- 物品配置(部分示例)
INSERT INTO item_template (item_id, name, quality, item_type, sell_price) VALUES
(2001, '青铜剑',   2, 1, 100),
(2002, '精铁甲',   2, 2, 150),
(2003, '回血丹',   1, 3, 50),
(2004, '经验药水', 1, 3, 200);