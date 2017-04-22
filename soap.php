<?php
header("content-type:text/html;charset=utf-8");
try {
    $client = new SoapClient("https://wsuat.acxiom.com.cn/lop/wsappservice/LOPWSAppServiceSOAP?wsdl");

    $param = [
        'UniqueID' => uniqid(),
        'mobile' => '18621872932',// '18616701872',
        'content' => 'code: 123456',
        'SourceTag' => 'MENvolcanoFS',
    ];

    $res = $client->sendCouponSMS($param);
    $res = get_object_vars($res);
    echo '<pre>';
    print_r($res);
    echo '</pre>';
    //print_r($client->__getTypes());  
} catch (SOAPFault $e) {
    print $e;
}