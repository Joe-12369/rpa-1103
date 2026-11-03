# Z-RPA
## 1.安装插件
### 1.1 插件安装
在factory中进行安装

![](./image/factoryinstall.png '描述')

## 2 手动拖入插件
### 2.1chrome插件安装的打开方式
1.	在地址栏输入 chrome://extensions 回车即可打开插件安装页面

![](./image/chromeextension.png '描述')

2.	点击chrome右侧 自定义及控制Google Chrome 按钮 选择 "更多工具" 点击 "扩展程序" 即可打开插件安装界面

3.	点击chrome右侧 自定义及控制Google Chrome 按钮 点击 "工具" 在页面左侧点击 "扩展程序" 即可打开插件安装界面

### 2.2插件安装方式

1.	在插件安装页面 首先在页面右侧开启 "开发者模式" 选项

![](./image/extensions.png '描述')
 
2.	将插件(后缀为.crx文件)鼠标选中文件 拖入插件安装界面 拖入crx 文件(C:\Program Files (x86)\Z-Factory\resources\app\resources\support\chromePlugin\RPAChromeExtension\IIRPAChromeExtension.crx)

![](./image/dragcrx.png '描述')

点击添加扩展程序 即安装成功

![](./image/addextension.png '描述')

## 异常处理
### 1.如果拖入插件后chrome报错

检查开发者模式是否开启

### 2.如果插件安装成功 但是录制操作无法使用

检查任务管理器中是否存在名为 II.RPA.NativeMessagingHost.exe 

![](./image/task.png '描述')

若无: 可尝试重启chrome浏览器
