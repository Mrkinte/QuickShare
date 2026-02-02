# QuickShare API 文档

## 概述

本文档描述了 QuickShare 项目后端 API 接口规范，供前端开发者参考使用。项目基于 ASP.NET Core 构建，采用 RESTful API 设计风格。具体示例请参照该项目配套的前端代码[QuickShare-WebUI](https://github.com/mrkinte/QuickShare-WebUI)

## 基础信息

- **基础路径**: `/api`
- **认证方式**: Cookie 认证
- **数据格式**: JSON

## TransmitController - 文件传输模块

文件传输控制器，提供文件上传、下载、浏览等功能。

**基础路径**: `/api/Transmit`

### 1.1 健康检查

**接口**: `GET /api/Transmit/alive/{uuid}`

**描述**: 检查服务器是否在线，用于心跳检测

**认证**: 允许匿名访问

**参数**:

| 名称 | 类型 | 位置 | 必填 | 说明 |
|------|------|------|------|------|
| uuid | string | path | 是 | 客户端唯一标识 |

**响应示例**:

```json
{
  "status": "alive"
}
```

### 1.2 用户登录

**接口**: `POST /api/Transmit/login`

**描述**: 用户登录验证

**认证**: 允许匿名访问

**请求参数** (Form Data):

| 名称 | 类型 | 必填 | 说明 |
|------|------|------|------|
| password | string | 是 | 登录密码 |

**成功响应 (200)**:

```json
{
  "message": "Login successfully."
}
```

**失败响应 (401)**:

```json
{
  "error": "Login failure."
}
```

### 1.3 登录状态检查

**接口**: `GET /api/Transmit/logged`

**描述**: 检查当前用户是否已登录

**认证**: 允许匿名访问

**响应示例**:

```json
{
  "isAuthenticated": true
}
```

### 1.4 获取传输参数

**接口**: `GET /api/Transmit/parameter`

**描述**: 获取文件传输相关配置参数

**认证**: 需要登录

**响应示例**:

```json
{
  "maxFileSize": 1024
}
```

### 1.5 获取根目录文件列表

**接口**: `GET /api/Transmit/files`

**描述**: 获取根目录下的文件和文件夹列表

**认证**: 需要登录

**响应示例**:

```json
[
  {
    "name": "documents",
    "size": 0,
    "type": "folder",
    "url": "documents",
    "createTime": "2024-01-15 10:30:00"
  },
  {
    "name": "example.pdf",
    "size": 2048576,
    "type": "file",
    "url": "example.pdf",
    "createTime": "2024-01-15 10:30:00"
  }
]
```

**字段说明**:

| 字段 | 类型 | 说明 |
|------|------|------|
| name | string | 文件或文件夹名称 |
| size | long | 文件大小（字节），文件夹为 0 |
| type | string | 类型：`"file"` 或 `"folder"` |
| url | string | 文件相对路径 |
| createTime | string | 创建时间，格式：`yyyy-MM-dd HH:mm:ss` |

### 1.6 获取指定目录文件列表

**接口**: `GET /api/Transmit/files/{*path}`

**描述**: 获取指定路径下的文件和文件夹列表

**认证**: 需要登录

**参数**:

| 名称 | 类型 | 位置 | 必填 | 说明 |
|------|------|------|------|------|
| path | string | path | 是 | 目录相对路径，支持多级目录 |

**响应格式**: 同 1.5

### 1.7 文件上传

**接口**: `POST /api/Transmit/upload`

**描述**: 上传文件到服务器，支持大文件分块上传

**认证**: 需要登录

**Content-Type**: `multipart/form-data`

**请求体**: 表单文件字段

**成功响应 (200)**:

```json
{
  "successFiles": ["file1.txt", "file2.pdf"],
  "successCount": 2
}
```

**失败响应**:

- 400: `{"error": "Request content type must be multipart/form-data."}`
- 400: `{"error": "Uploaded file is empty."}`
- 413: `{"error": "File upload failed: Total size exceeds the limit."}`
- 500: `{"error": "Upload cancelled."}` 或 `{"error": "File upload failed: {error_message}"}`

**说明**:

- 单次请求总文件大小不能超过 `maxFileSize` 配置
- 如果文件名已存在，会自动在文件名后添加数字后缀
- 支持大文件上传，不限制请求体大小

### 1.8 文件下载

**接口**: `GET /api/Transmit/download/{*path}`

**描述**: 下载指定文件

**认证**: 需要登录

**参数**:

| 名称 | 类型 | 位置 | 必填 | 说明 |
|------|------|------|------|------|
| path | string | path | 是 | 文件相对路径 |

**失败响应 (404)**:

```json
{
  "error": "File does not exist."
}
```

**失败响应 (500)**:

```json
{
  "error": "File download failed: {error_message}"
}
```

**说明**: 返回文件流，Content-Type 根据文件扩展名自动识别

---

## ShareController - 分享模块

分享控制器，提供文件分享、下载等功能。

**基础路径**: `/api/Share`

### 2.1 检查分享是否需要密码

**接口**: `GET /api/Share/is_private/{shareId}`

**描述**: 检查指定分享是否需要提取码

**认证**: 允许匿名访问

**参数**:

| 名称 | 类型 | 位置 | 必填 | 说明 |
|------|------|------|------|------|
| shareId | string | path | 是 | 分享 ID（加密字符串） |

**成功响应 (200)**:

```json
{
  "isPrivate": true
}
```

**失败响应 (404)**:

```json
{
  "error": "Invalid sharing link."
}
```

### 2.2 获取分享信息

**接口**: `POST /api/Share/info/{shareId}`

**描述**: 获取分享详情，如果需要提取码则验证提取码

**认证**: 允许匿名访问

**参数**:

| 名称 | 类型 | 位置 | 必填 | 说明 |
|------|------|------|------|------|
| shareId | string | path | 是 | 分享 ID（加密字符串） |
| verifyCode | string | form | 否 | 提取码（私密分享必填） |

**成功响应 (200)**: 返回分享历史记录对象

**失败响应 (404)**:

```json
{
  "error": "Invalid sharing link."
}
```

**失败响应 (401)**:

```json
{
  "error": "Incorrect verification code."
}
```

### 2.3 下载分享文件

**接口**: `GET /api/Share/download/{shareId}/{fileId}`

**描述**: 下载分享的文件

**认证**: 允许匿名访问

**参数**:

| 名称 | 类型 | 位置 | 必填 | 说明 |
|------|------|------|------|------|
| shareId | string | path | 是 | 分享 ID（加密字符串） |
| fileId | long | path | 是 | 文件 ID |

**失败响应 (404)**:

```json
{
  "error": "Invalid sharing link."
}
```

**失败响应 (500)**:

```json
{
  "error": "File download failed."
}
```

**说明**: 返回文件流，Content-Type 根据文件扩展名自动识别

---

## 通用错误响应

所有接口的错误响应统一格式：

```json
{
  "error": "错误描述信息"
}
```