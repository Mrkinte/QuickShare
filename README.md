
## 📖项目简介

一款局域网文件传输工具，仅需在 Windows 端部署该应用，即可通过浏览器在手机、平板等设备间快速上传与下载文件。无需在每台设备安装应用，实现“一次部署，多端即用”，让文件传输更轻量、更高效。

## 🔐安全性

- 上传/下载文件需要登录，避免未经允许的恶意上传。

- 分享文件时支持创建分享验证码，确保隐私文件不被泄露。

- 所有内容经过Https加密传输。

## ⚒️自定义QuickShare前端界面

- [QuickShare-WebUI](https://github.com/Mrkinte/QuickShare/blob/main/QuickShare-WebUI/README.md)

- 自定义前端界面，请将编译后的前端代码保存到QuickShare项目的``Assets/wwwroot/``目录下，VS编译时会自动将``wwwroot``目录复制到构建目录（``bin/Debug/net10.0-windows/``）中。

- 对于已经编译好的QuickShare项目，直接复制前端代码到软件所在路径的``wwwroot``目录即可。

## 📺视频演示
- [Bilibili](https://www.bilibili.com/video/BV1GUFAz7ES8/)

## [📷前端截图](https://github.com/Mrkinte/QuickShare/blob/main/QuickShare-WebUI/Docs/)

![Login](https://github.com/Mrkinte/QuickShare/blob/main/QuickShare-WebUI/Docs/Login.png?raw=true)


## [📷后端截图](https://github.com/Mrkinte/QuickShare/blob/main/QuickShare/Docs/)

- 支持右键菜单->发送到QuickShare，快速分享文件。

![TransmitScan](https://github.com/Mrkinte/QuickShare/blob/main/QuickShare/Docs/TransmitScan.png?raw=true)