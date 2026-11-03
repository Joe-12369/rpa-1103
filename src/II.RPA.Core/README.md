# 须知

## 启动参数
* -client 以何种模式启动CoreServer, 常见值为[Z-Admin, Z-Factory、Z-Bot], 默认值为Z-Admin, 非Z-Admin模式会检查设计器或机器人进程, 失败退出
* -brain-path[ZBrainPath] 指定Z-Brain程序路径, 可以指向程序安装目录下[Version]\resources\z-brain\Z-Brain.exe
* -engine-path[EnginePath] 指定rpa-engine程序路径, 可以指向程序安装目录下[Version]\resources\rpa-engine\rpa-engine.exe
* -brain-proj[ZBrainProject] 指定Z-Brain项目目录, 跨项目联调使用
* -engine-proj[EngineProject] 指定rpa-engine项目目录, 跨项目联调使用
* -python-venv[PythonVenv] 指向python程序依赖的虚拟环境, 跨项目联调使用
* -brain-num[ZBrainNum] ZBrain进程实例数量
* -executor-num[ExecutorNum] Engine工作进程实例数量
注: 括号内是与之对应的配置项, CoreServer会再次尝试从配置项中获取值
