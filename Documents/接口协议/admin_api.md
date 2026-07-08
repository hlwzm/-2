# 后台管理系统 API (RESTful)
> 基础URL: https://admin.jx3.com/api
> 认证: Bearer Token

## 仪表盘
| Method | Path | 说明 |
|--------|------|------|
| GET | /api/admin/dashboard | 看板数据 |

## 玩家管理
| Method | Path | 说明 |
|--------|------|------|
| GET | /api/admin/player/search?keyword=&page=1 | 搜索 |
| GET | /api/admin/player/:id | 玩家详情 |
| POST | /api/admin/player/:id/ban | 封号 {reason} |
| POST | /api/admin/player/:id/unban | 解封 |
| POST | /api/admin/player/:id/mute | 禁言 {duration} |
| POST | /api/admin/player/:id/mail | 发邮件 {title,content,items} |
| POST | /api/admin/player/:id/modify | 修改数据 {field,value} |

## 交易审核
| Method | Path | 说明 |
|--------|------|------|
| GET | /api/admin/trade/list | 拍卖行列表 |
| POST | /api/admin/trade/:id/remove | 强制下架 |

## 公告与活动
| Method | Path | 说明 |
|--------|------|------|
| POST | /api/admin/notice | 发布公告 |
| POST | /api/admin/activity | 创建/修改活动 |
| POST | /api/admin/activity/:id/start | 开启活动 |
| POST | /api/admin/activity/:id/stop | 关闭活动 |

## 举报与服务器
| Method | Path | 说明 |
|--------|------|------|
| GET | /api/admin/reports | 举报列表 |
| POST | /api/admin/report/:id/handle | 处理举报 |
| GET | /api/admin/server/status | 服务器状态 |
| GET | /api/admin/server/logs | 日志查询 |
| GET | /api/admin/statistics/dau | DAU数据 |
| GET | /api/admin/statistics/revenue | 收入数据 |