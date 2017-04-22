using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;


[DataContract]
class RsObj
{
    [DataMember(Order = 0)]
    public bool success { get; set; }

    [DataMember(Order = 1)]
    public int code { get; set; }
}


public partial class VerifyCode : System.Web.UI.Page
{


    private string ObjectToJson(Object obj)
    {
        DataContractJsonSerializer serializer = new DataContractJsonSerializer(obj.GetType());
        MemoryStream stream = new MemoryStream();
        serializer.WriteObject(stream, obj);
        byte[] dataBytes = new byte[stream.Length];
        stream.Position = 0;
        stream.Read(dataBytes, 0, (int)stream.Length);
        return Encoding.UTF8.GetString(dataBytes);
    }


    private void OutPut(RsObj obj)
    {
        Response.ContentType = "application/json";
        Response.Charset = "UTF-8";
        Response.Write(ObjectToJson(obj));
        Response.End();
    }

    private String GetDbConfig()
    {
        return @"Data Source=ww6txakxu3.database.chinacloudapi.cn,1433;Initial Catalog=app_sampling_campaign;User ID=mrmuser;Password=dEoCdK=8;MultipleActiveResultSets=true";
        //return @"Data Source=192.168.0.214;Initial Catalog=app_sampling_campaign;User ID=sa;Password=123456;MultipleActiveResultSets=true";
    }


    private int SmsSentTimes(String phone)
    {
        SqlConnection conn = new SqlConnection(GetDbConfig());
        conn.Open();
        String ymd = DateTime.Now.ToString("yyyyMMdd");
        String sql = @"SELECT COUNT(id) AS times FROM sms WHERE phone='" + phone + "' AND created = '" + ymd + "'";
        SqlCommand cmd = new SqlCommand(sql, conn);
        SqlDataReader sdr = cmd.ExecuteReader();
        int i = 0;
        while (sdr.Read())
        {
            i = (int)sdr[0];
        }

        conn.Close();

        return i;
    }


    private int SentSms(String phone, String code)
    {
        SqlConnection conn = new SqlConnection(GetDbConfig());
        conn.Open();
        String ymd = DateTime.Now.ToString("yyyyMMdd");
        String sql = @"INSERT INTO sms(phone, smscode, created) VALUES('" + phone + "','" + code + "','" + ymd + "')";
        SqlCommand cmd = new SqlCommand(sql, conn);
        int rs = cmd.ExecuteNonQuery();
        conn.Close();
        return rs;
    }


    protected void Page_Load(object sender, EventArgs e)
    {
        //**
        RsObj rsObj = new RsObj();
        rsObj.success = false;

        //提取电话号码
        String phone = Request.Params["phone"];

        //验证空参数
        if (null == phone || 11 != phone.Length)
        {
            rsObj.code = 1000;
            OutPut(rsObj);
            return;
        }

        //验证电话号码
        if (!System.Text.RegularExpressions.Regex.IsMatch(phone, @"^[1]+\d{10}"))
        {
            rsObj.code = 1001;
            OutPut(rsObj);
            return;
        }

        //读取电话今日发送短信次数
        if (SmsSentTimes(phone) > 2)
        {
            rsObj.code = 1002;
            OutPut(rsObj);
            return;
        }
        //*/

        //生成验证码
        Random rd = new Random();
        int code = rd.Next(1111, 9999);

        String smsContent = @"您的验证码为：" + code.ToString() + "。请在10分钟内填写。欧莱雅男士火山岩控油清痘洁面膏，火力全开，抗痘到底！ 回0退订。";


        //发送短信
        //**
        AcxiomUat.LOPWSAppServiceSOAPClient soapClient = new AcxiomUat.LOPWSAppServiceSOAPClient();
        //soapClient.Endpoint.Address = new System.ServiceModel.EndpointAddress("https://wsuat.acxiom.com.cn/lop/wsappservice/LOPWSAppServiceSOAP?wsdl");
        //soapClient.Open();
        soapClient.sendCouponSMS("20170421", phone, smsContent, "MENvolcanoFS");
        //soapClient.Close();
        //Response.Write(res);
        //Response.End();
        //*/

        //**
        //保存数据库记录
        SentSms(phone, code.ToString());
        
        //返回结果
        rsObj.success = true;
        rsObj.code = 0;// code;

        OutPut(rsObj);
        //*/
    }
}