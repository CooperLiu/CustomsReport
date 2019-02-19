# 海关上报支付数据

## 技术栈

- net461
- Topshelf
- Newtonsoft.Json

# 避坑指南

## 官方文档

[海关总署公告2018年第165号（关于实时获取跨境电子商务平台企业支付相关原始数据有关事宜的公告）](http://www.customs.gov.cn/customs/302249/302266/302269/2087562/index.html) 

[海关总署公告2018年第179号（关于实时获取跨境电子商务平台企业支付相关原始数据接入有关事宜的公告）](http://www.customs.gov.cn/customs/302249/302266/302269/2125253/index.html)

[中国电子口岸客户端控件](http://patchdownload.chinaport.gov.cn/EportClient/EportClientSetup_V1.5.5.exe)

[附件3：电子口岸安全组件及使用手册](https://pan.baidu.com/s/1zNa42LDPdDHLJceNodZVQw)  提取码：cmm5

## 对接流程



## 企业返回实时数据接口（部署在通关服务系统）

进行加签之前的报文  

``` text
"sessionID":"fe2374-8fnejf97-55616242"||"payExchangeInfoHead":"{"guid":"9D55BA71-55DE-41F4-8B50-C36C83B3B419","initalRequest":"https://openapi.alipay.com/gateway.do?timestamp=2013-01-0108:08:08&method=alipay.trade.pay&app_id=13580&sign_type=RSA2&sign=ERITJKEIJKJHKKKKKKKHJEREEEEEEEEEEE&version=1.0&charset=GBK","initalResponse":"ok","ebpCode":"3301963K69","payCode":"312226T001","payTransactionId":"2018121222001354081010726129","totalAmount":100,"currency":"142","verDept":"3","payType":"1","tradingTime":"20181212041803","note":"批量订单，测试订单优化,生成多个so订单"}"||"payExchangeInfoLists":"[{"orderNo":"SO1710301150602574003","goodsInfo":[{"gname":"lhy-gnsku3","itemLink":"http://m.yunjiweidian.com/yunjibuyer/static/vue-buyer/idc/index.html#/detail?itemId=999761&shopId=453"},{"gname":"lhy-gnsku2","itemLink":"http://m.yunjiweidian.com/yunjibuyer/static/vue-buyer/idc/index.html#/detail?itemId=999760&shopId=453"}],"recpAccount":"OSA571908863132601","recpCode":"","recpName":"YUNJIHONGKONGLIMITED"}]"||"serviceTime":"1544519952469"
```


上传给海关的报文
``` json

{"sessionID":"fe2374-8fnejf97-55616242","payExchangeInfoHead":{"guid":"9D55BA71-55DE-41F4-8B50-C36C83B3B419","initalRequest":"https://openapi.alipay.com/gateway.do?timestamp=2013-01-0108:08:08&method=alipay.trade.pay&app_id=13580&sign_type=RSA2&sign=ERITJKEIJKJHKKKKKKKHJEREEEEEEEEEEE&version=1.0&charset=GBK","initalResponse":"ok","ebpCode":"3301963K69","payCode":"312226T001","payTransactionId":"2018121222001354081010726129","totalAmount":100,"currency":"142","verDept":"3","payType":"1","tradingTime":"20181212041803","note":"批量订单，测试订单优化,生成多个so订单"},"payExchangeInfoLists":[{"orderNo":"SO1710301150602574003","goodsInfo":[{"gname":"lhy-gnsku3","itemLink":"http://m.yunjiweidian.com/yunjibuyer/static/vue-buyer/idc/index.html#/detail?itemId=999761&shopId=453"},{"gname":"lhy-gnsku2","itemLink":"http://m.yunjiweidian.com/yunjibuyer/static/vue-buyer/idc/index.html#/detail?itemId=999760&shopId=453"}],"recpAccount":"OSA571908863132601","recpCode":"","recpName":"YUNJIHONGKONGLIMITED"}],"serviceTime":"1544519952469","certNo":"01010000000019f1","signValue":"J1shnr986MzgvwOBIMD0QMpkTTTARsGgwM9RkRAAmZOWA1ZAi8KNR+h5WtqXy6qdiW9KTfLyx9kgseWX/udghOOMVJrYlGelhwg26L7bq5gj72AU40zXq69bNoOgH/ccSQzHFRvbGug2gJ4Pv8dSNVVY8rFzX+8AMNnHTdIWo74="}

```

### 深坑 报文特殊之处

1. guid 的值必须为大写字母

2. totalAmount 的值 零尾巴截掉，而且不能带双引号。

    错误格式 | 正确格式
    --------|----------
    100.0   | 100
    100.10  | 100.1

3. 加签字符串形式:  注意格式 字段顺序 二级字段顺序 必须和固定格式一致 表头表体和时间的value必须添加双引号，totalAmount不带引号  

4. 报文中的 ***certNo*** ，***ebpCode*** 需要替换为 自己企业的对应信息

    > certNo 、ebpCode 不对应，错误提示：企业实时数据获取验签证书未在服务系统注册的

5. 报文中的 ***sessionID***，来自于海关请求电商平台接口的 ***sessionID*** ，如果每次请求的 ***sessionID*** 相同，错误提示：上传失败，入库失败  
    > 起码走到这一步，证明验签问题解决了

### 上报海关请求报文的请求形式 
HTTP POST 请求，Content-Type: application/x-www-form-urlencoded


### 加签方法

1. 咨询当地【跨境电商公共服务平台】是否能辅助加签。
2. 本地加签方式  
    - Js + WebStocket 
    - JAVA/C#/PHP + WebStocket
    - JAVA/C# + COM组件  [JAVA示例](https://blog.csdn.net/qq_28024699/article/details/85099953)  [C#示例](https://github.com/CooperLiu/CustomsReport/blob/master/src/Boss.Scm.CustomsReportHost/SDK/SignSdkApi.cs#L34)

官方提供的加签为JS + WebStocket示例，这种方式比较绕，需要定期轮询待加签的列表。

建议下 使用COM组件的形式，可以以同步的方式上报数据，结构比较清晰，[示例代码](https://github.com/CooperLiu/CustomsReport/blob/master/src/Boss.Scm.CustomsReportHost/SDK/SignSdkApi.cs#L34)
## 企业实时数据获取接口（部署在电商平台）

## 深坑 海关请求方式

1. 海关请求方式HTTP POST请求， Content-Type: application/x-www-form-urlencoded
    > 企业直接获取openReq参数即可 openReq={"sessionID":"fe2374-8fnejf97-55616242","orderNo":"XXXX","serviceTime":"1544519952469"}

2. 接口端口必须是80端口

3. 必须成功响应数据

    > {"code":"10000","message":"XXXX","serviceTime":"1544519952469"}

4. 审核数据为近三天订单数据

5. 通关系统注册接口地址 [互联网+海关](http://customs.chinaport.gov.cn/deskcus/cus/deskIndex?menu_id=cebimp&ticket=ST-832543-KHmT5D0pdNQiIbkZAePE-cas#)
    > 访问注册地址，需要插入操作人IC卡   
    > 证书注册时，需要注意证书编号，一定要小写


