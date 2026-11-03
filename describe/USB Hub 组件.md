## USB Hub 组件



### 本地版

#### 一.加载方式

##### 引用DLL：II.RPA.dll

#####  引用类： UsbSDK

##### 其他注意点：需要安装驱动：PL23XX-M_LogoDriver_Setup_v202_20200527.exe；

##### 驱动位置：\\192.168.1.251\shared\RPA-dll-C#\元素拾取及操作\主线\common

#### 二.接口

##### 1. 获取串口列表

```
public string GetComs()
```

获取本机的串口列表

返回值：

```
{"status":1,"message":"","info":"[\"COM3\",\"COM4\"]"}
```

status:接口操作结果，1：成功，0：失败

message：接口操作失败时，该字段会记录错误信息

info：返回的串口数组；

##### 2.打开通道

```
 public string Open(string portName,string channel)
```

接口说明：当需要用Usb Hub上某一个端口的USB设备信息时，需要打开该通道；

入参说明：

portName：第一步中返回的有效的串口名称，如：“COM4”；

channel：通道号，返回从1-24；

返回值：

```
{"status":1,"message":"","info":"true"}
```

status:接口操作结果，1：成功，0：失败

message：接口操作失败时，该字段会记录错误信息

info：true:打开成功，false：打开失败；

##### 3.关闭通道

```
 public string Close(string portName,string channel)
```

接口说明：使用完成后需要关闭通道；

入参说明：

portName：第一步中返回的有效的串口名称，如：“COM4”；

channel：通道号，返回从1-24；

返回值：

```
{"status":1,"message":"","info":"true"}
```

status:接口操作结果，1：成功，0：失败

message：接口操作失败时，该字段会记录错误信息

info：true:关闭成功，false：关闭失败；



### 网络版

#### 一.加载方式

##### 引用DLL：II.RPA.dll

#####  引用类： NetworkUsbSDK

##### 其他注意点：需要安装驱动：NetworkUSB.zip

##### 驱动位置：\\192.168.1.251\shared\RPA-dll-C#\元素拾取及操作\主线\common

#### 二.接口

##### 1.打开通道

```
 public string Open(string ipAddress,int port ,string channel)
```

接口说明：当需要用Usb Hub上某一个端口的USB设备信息时，需要打开该通道；

入参说明：

ipAddress：网络版USB Hub 主机 IP地址；

port：网络版USB Hub  端口号，默认3240；

channel：通道号，返回从1-16；

返回值：

```
{"status":1,"message":"","info":"true"}
```

status:接口操作结果，1：成功，0：失败

message：接口操作失败时，该字段会记录错误信息

info：true:打开成功，false：打开失败；

##### 2.关闭通道

```
 public string Close(string ipAddress,int port ,string channel)
```

接口说明：使用完成后需要关闭通道；

入参说明：

ipAddress：网络版USB Hub 主机 IP地址；

port：网络版USB Hub  端口号，默认3240；

channel：通道号，返回从1-16；

返回值：

```
{"status":1,"message":"","info":"true"}
```

status:接口操作结果，1：成功，0：失败

message：接口操作失败时，该字段会记录错误信息

info：true:关闭成功，false：关闭失败；









