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
class JsonObj
{
    [DataMember(Order = 0)]
    public bool success { get; set; }

    [DataMember(Order = 1)]
    public int code { get; set; }
}


public partial class Apply : System.Web.UI.Page
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


    private void OutPut(JsonObj obj)
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

    public string StringClear(string strMessage)
    {
        string[] aryReg = { "'", "<", ">", "%", "\"\"", ",", ".", ">=", "=<", "-", "_", ";", "||", "[", "]", "&", "/", "-", "|", " " };
        for (int i = 0; i < aryReg.Length; i++)
        {
            strMessage = strMessage.Replace(aryReg[i], string.Empty);
        }
        return strMessage;
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        String phone = Request.Params["phone"];
        String name = Request.Params["name"];
        String province = Request.Params["province"];
        String city = Request.Params["city"];
        String address = Request.Params["address"];
        String smscode = Request.Params["smscode"];
        String postcode = Request.Params["postcode"];

        JsonObj rsObj = new JsonObj();
        rsObj.success = false;

        //验证空参数
        if (null == phone || 11 != phone.Length)
        {
            rsObj.code = 1000; //电话号码非法
            OutPut(rsObj);
            return;
        }

        //验证电话号码
        if (!System.Text.RegularExpressions.Regex.IsMatch(phone, @"^[1]+\d{10}"))
        {
            rsObj.code = 1001; //电话号码非法
            OutPut(rsObj);
            return;
        }

        // 验证名字
        if(null == name)
        {
            name = "";
        }
        name = StringClear(name);
        if(name.Length < 1)
        {
            rsObj.code = 1002; //名字非法
            OutPut(rsObj);
            return;
        }

        if(null == province)
        {
            province = "";
        }
        province = StringClear(province);
        if (province.Length < 1)
        {
            rsObj.code = 1003; //省份非法
            OutPut(rsObj);
            return;
        }

        if (null == city)
        {
            city = "";
        }
        city = StringClear(city);
        if (city.Length < 1)
        {
            rsObj.code = 1004; //城市非法
            OutPut(rsObj);
            return;
        }

        if (null == address)
        {
            address = "";
        }
        address = StringClear(address);
        if (address.Length < 1)
        {
            rsObj.code = 1005; //地址非法
            OutPut(rsObj);
            return;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(smscode, @"^\d+"))
        {
            rsObj.code = 1006; //短语验证码非法
            OutPut(rsObj);
            return;
        }
        
        
        if (!System.Text.RegularExpressions.Regex.IsMatch(postcode, @"^\d+"))
        {
            postcode = "000000";
            //rsObj.code = 1007;　//邮编非法
            //OutPut(rsObj);
            //return;
        }

        //数据库连接
        SqlConnection conn = new SqlConnection(GetDbConfig());
        conn.Open();

        //已申请验证
        String sql = @"SELECT COUNT(phone) AS counts FROM proposer WHERE phone='" + phone + "'";
        SqlCommand cmd = new SqlCommand(sql, conn);
        SqlDataReader sdr = cmd.ExecuteReader();
        int i = 0;
        while (sdr.Read())
        {
            i = (int)sdr[0];
        }

        if(i > 0)
        {
            conn.Close();
            rsObj.code = 1008; //已经申请
            OutPut(rsObj);
            return;
        }

        //短信验证
        String ymd = DateTime.Now.ToString("yyyyMMdd");
        sql = @"SELECT smscode FROM sms WHERE phone='" + phone + "' AND created='" + ymd + "'";
        cmd = new SqlCommand(sql, conn);
        sdr = cmd.ExecuteReader();
        bool codeValid = false;
        int _code = 0;
        int.TryParse(smscode, out _code);
        while (sdr.Read())
        {
            int _code2 = -1;
            int.TryParse(sdr[0].ToString(), out _code2);
            if (_code == _code2)
            {
                codeValid = true;
                break;
            }
        }
        if (!codeValid)
        {
            conn.Close();
            rsObj.code = 1009; //短信验证码无效
            OutPut(rsObj);
            return;
        }
        
        //统计总量
        sql = @"SELECT COUNT(phone) AS counts FROM proposer";
        cmd = new SqlCommand(sql, conn);
        sdr = cmd.ExecuteReader();
        int total = 0;
        while (sdr.Read())
        {
            total = (int)sdr[0];
        }

        int applySuccess = 0;
        if(total < 590000) //限制59万
        {
            applySuccess = 1;
        }


        //保存数据
        String created = DateTime.Now.ToString("yyyyMMdd HH:mm:ss");
        sql = @"INSERT INTO proposer(phone, name, province, city, address, smscode, postcode, success, created) VALUES('" + phone + "','" + name + "','" + province + "','" + city + "','" + address + "','" + smscode + "','" + postcode + "','" + applySuccess.ToString() + "','" + created + "')";
        cmd = new SqlCommand(sql, conn);
        int rs = cmd.ExecuteNonQuery();
        if (rs < 1)
        {
            conn.Close();
            rsObj.code = 1010; //申请失败
            OutPut(rsObj);
            return;
        }
        conn.Close();

        rsObj.success = true;
        rsObj.code = applySuccess;

        OutPut(rsObj);
    }
}