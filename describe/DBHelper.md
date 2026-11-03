# 数据库组件
### 一. 加载方式
####   引用DLL：II.RPA.dll
####   引用类：DbSdk
---
### 二. 接口说明
#### 1. 创建数据库连接
   `public string CreateDbConnection(int dbtype, string connectStr)`
<br>参数说明： **dbType**：数据库类型； 
<br>&nbsp; 0：MySQL，1：SqlServer 2: Oracle 3: Pgsql 4: Sqlite 5: Access 
<br>&nbsp; **connectStr**:数据库连接字符串,用JSON格式来传入数据库连接字符串，不同的数据库连接字符串不同；格式如下：<br>
```
{"DataSource":"127.0.0.1","DbName":"linxi","UserName":"root","Pwd":"88888888","Charset":"utf8","Port":"3306","Sid":"","Filepath":""}
```
<br>返回值：包含执行结果的JSON字符串,status：表示执行结果，1成功，0失败；失败原因会在message字段中注明；info包含要返回给接口调用方的信息。在本接口中返回数据库标识，用于后续使用。<br> 
```
{"status":1,"message":"","info":"1e2e7865-33be-4dae-acfa-7059d306e5eb"}
```
#### 2. 执行单SQL查询
 `  public string QuerySingle(string connectionID, string sql, string paras, int showdic)`
<br>查询结果集的第一条记录
<br>参数说明： **connectionID**：创建数据库成功后返回的标识； 
<br>&nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;  **sql**：SQL语句
 <br>&nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;  **paras**：SQL语句参数
<br>&nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;   **showdic**:是否显示字典,这里的字典其实是数据库表的列头名称<br>
返回值：
- 不返回字典
```
{"status":1,"message":"","info":"[\"23\",\"23\"]"}
```
- 返回字典
```
{"status":1,"message":"","info":"{\"id\": \"23\", \"name\": \"23\"}"}
```
#### 3. 执行全SQL查询
` public string QueryAll(string connectionID, string sql, string paras, int showdic)`
<br> 查询结果集的全部记录
<br>参数说明： **connectionID**：创建数据库成功后返回的标识； 
<br>&nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;   **sql**：SQL语句
 <br>&nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;  **paras**：SQL语句参数
<br>&nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;   **showdic**:是否显示字典<br>
返回值：
- 不返回字典
```
{"status":1,"message":"","info":"[[\"123\",\"123\"],[\"124\",\"124\"],[\"125\",\"125\"],[\"21\",\"21\"],[\"22\",\"22\"],[\"2232\",\"2232\"],[\"2233\",\"2233\"],[\"23\",\"23\"],[\"24\",\"24\"],[\"3\",\"3\"],[\"4\",\"4\"],[\"5\",\"55\"]]"}
```
- 返回字典
```
{"status":1,"message":"","info":"[{\"id\":\"123\",\"name\":\"123\"},{\"id\":\"124\",\"name\":\"124\"},{\"id\":\"125\",\"name\":\"125\"},{\"id\":\"21\",\"name\":\"21\"},{\"id\":\"22\",\"name\":\"22\"},{\"id\":\"2232\",\"name\":\"2232\"},{\"id\":\"2233\",\"name\":\"2233\"},{\"id\":\"23\",\"name\":\"23\"},{\"id\":\"24\",\"name\":\"24\"},{\"id\":\"3\",\"name\":\"3\"},{\"id\":\"4\",\"name\":\"4\"},{\"id\":\"5\",\"name\":\"55\"}]"}
```
#### 4. 执行SQL语句
` public string ExecuteSql(string connectionID, string sql, string paras)`
<br>参数说明： **connectionID**：创建数据库成功后返回的标识； 
<br>&nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;   **sql**：SQL语句
 <br>&nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;  **paras**：SQL语句参数<br>
返回值：
返回受影响的行数
```
{"status":1,"message":"","info":"1"}
```
#### 5. 批量执行SQL语句
` public string BatchExecuteSql(string connectionID, string sql, string paras)`    
<br>参数说明： **connectionID**：创建数据库成功后返回的标识； 
<br>&nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;   **sql**：SQL语句
 <br>&nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;  **paras**：SQL语句参数<br>
返回值：
返回受影响的行数
```
{"status":1,"message":"","info":"5"}
```
#### 6. 关闭连接
`public string CloseConnection(string connectionID)`
<br>参数说明： **connectionID**：创建数据库成功后返回的标识； <br>
返回值：
```
{"status":1,"message":"","info":""}
```

### 三. 应用举例
id|name
--|:--:
1|刘备
2|关羽
3|张飞
- **创建数据库连接**<br>
  成功后返回<br>
`{"status":1,"message":"","info":"1e2e7865-33be-4dae-acfa-7059d306e5eb"}`
- **执行单SQL查询**<br>
   入参：<br>
   connectionID: 1e2e7865-33be-4dae-acfa-7059d306e5eb <br>
   sql: <br>
    不带参数的SQL语句： select * from test; <br>
    带参数的SQL语句：select * from  test where id=@id <br>
   paras:<br>
当SQL语句带参数时，需要配置SQL参数字段,格式如下：<br>
单参数：['1']，多参数：['1','2','3']
- **批量执行SQL语句**<br>
  入参：<br>
   connectionID: 1e2e7865-33be-4dae-acfa-7059d306e5eb <br>
   sql: <br>
    不带参数的SQL语句： insert into  test values('4','赵云'); <br>
    带参数的SQL语句：insert into  test values(@id,@name);<br>
   paras:<br>
单条执行：[['4','赵云']]，多条执行：[['5','黄忠'],['6','马超'],['7','魏延']],此例中的多条执行，共插入三条记录。



