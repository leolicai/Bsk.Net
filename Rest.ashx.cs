using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.IO;
using MyWeb.Entity.DB;
using System.Security.Cryptography;
using System.Text;
namespace MyWeb
{
    /// <summary>
    /// Rest 的摘要说明
    /// </summary>
    public class Rest : IHttpHandler
    {
        string campaignCode = "mnycrmcampaign201738";
        string sourceTag = "WECHAT";
        string key = "maybelline";
        MyWeb.Service.UserLuckService luckLogService = new MyWeb.Service.UserLuckService();
        MyWeb.Service.UserTrackingService trackService = new MyWeb.Service.UserTrackingService();

        //string api_url = "https://mblws.acxiom.com.cn";
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";
            if (context.Request["uat"] == "1")
            {
                context.Response.Write("suc");
            }
            else
            {
                AshxHelper helper = new AshxHelper(context);
                string action = helper.GetParam("action");
                switch (action)
                {
                    case "tracking":
                        Tracking(context, helper);
                        break;
                    case "getsegment"://获取是否绑定，会员等级
                        GetSegment(context, helper);
                        break;
                    case "sendsms":
                        SendSms(context, helper);
                        break;
                    case "getuserlucklog":
                        GetUserLuckLog(context, helper);
                        break;
                    case "ApplyUser":
                        ApplyUser(context, helper);
                        break;
                    case "AddLuckUser":
                        AddLuckUser(context, helper);
                        break;
                }
                helper.ResponseResult();
            }
        }
        #region tracking
        private void Tracking(HttpContext context, AshxHelper helper)
        {
            string openId = helper.GetParam("openid");
            string UserMoblie = helper.GetParam("UserMoblie");
            string utm_source = helper.GetParam("utm_source");
            string utm_medium = helper.GetParam("utm_medium");
            string utm_campaign = helper.GetParam("utm_campaign");
            if (utm_source != "" || utm_medium != "" || utm_campaign != "")
            {
                UserTrackingLog model = new UserTrackingLog();
                model.Openid = openId;
                model.UserMoblie = UserMoblie;
                model.utm_source = utm_source;
                model.utm_medium = utm_medium;
                model.utm_campaign = utm_campaign;
                model.CreateTime = DateTime.Now;
                trackService.insert(model);
            }
        }
        #endregion

            /// <summary>
            /// 获取用户身份
            /// </summary>
            /// <param name="context"></param>
            /// <param name="helper"></param>
        private void GetSegment(HttpContext context, AshxHelper helper)
        {
            string apiurl = "https://mblws.acxiom.com.cn/mbl/member/getSegment";
            string openId = helper.GetParam("openid");
            string unionId = helper.GetParam("unionId");
            string mobile = helper.GetParam("mobile");
            string signature = GetMD5_32(openId + unionId + mobile + DateTime.Now.ToString("yyyyMMdd") + key);
            string responseMsg = HttpGet(apiurl, $"openId={openId}&unionId={unionId}&mobile={mobile}&campaignCode={campaignCode}&sourceTag={sourceTag}&signature={signature}");

            Pixysoft.Json.CommonJsonModel model = new Pixysoft.Json.CommonJsonModel(responseMsg);
            string segment = model.GetValue("segment");
            helper.Add("errorCode", model.GetValue("errorCode"));
            helper.Add("segment", segment);//是否绑定0否

            if (segment != "0")
            {
                apiurl = "https://mblws.acxiom.com.cn/mbl/member/getCustomer";
                responseMsg = HttpGet(apiurl, $"openId={openId}&unionId={unionId}&mobile={mobile}&campaignCode={campaignCode}&sourceTag={sourceTag}&signature={signature}");

                Pixysoft.Json.CommonJsonModel model1 = new Pixysoft.Json.CommonJsonModel(responseMsg);
                helper.Add("memberLevel", model1.GetValue("memberLevel"));
                helper.Add("mobile", model1.GetModel("mobile"));
            }
        }

        #region 申请发送短信验证码
        private void SendSms(HttpContext context, AshxHelper helper)
        {
            string mobile = helper.GetParam("mobile");
            string MxtCode = new Random().Next(10000, 99999).ToString();
            string postUrl = "https://mblws.acxiom.com.cn/mbl/cce/sendCampaignSMS";

            string postData = "{\"mobile\":\"" + mobile + "\",\"sourceTag\":\"" + sourceTag + "\",\"campaignCode\":\"" + campaignCode + "\",\"content\":\"验证码：" + MxtCode + "，回复0退订\"}";
            string returnValue = string.Empty;
            try
            {
                byte[] byteData = Encoding.UTF8.GetBytes(postData);
                Uri uri = new Uri(postUrl);
                HttpWebRequest webReq = (HttpWebRequest)System.Net.WebRequest.Create(uri);
                webReq.Method = "POST";
                webReq.ContentType = "application/json";
                webReq.ContentLength = byteData.Length;
                //定义Stream信息
                Stream stream = webReq.GetRequestStream();
                stream.Write(byteData, 0, byteData.Length);
                stream.Close();
                //获取返回信息
                HttpWebResponse response = (HttpWebResponse)webReq.GetResponse();
                StreamReader streamReader = new StreamReader(response.GetResponseStream(), Encoding.Default);
                returnValue = streamReader.ReadToEnd();
                //关闭信息
                streamReader.Close();
                response.Close();
                stream.Close();

                Pixysoft.Json.CommonJsonModel model = new Pixysoft.Json.CommonJsonModel(returnValue);
                helper.Add("errorCode", model.GetValue("errorCode"));
                helper.Add("errorMessage", MxtCode);
            }
            catch (Exception ex)
            {
                helper.Add("errorCode", "-1");
                helper.Add("errorMessage", ex.Message);
            }

        }
        #endregion

        #region 判断是否参与过抽奖
        /// <summary>
        /// 获取用户是否参与过抽奖
        /// </summary>
        /// <param name="context"></param>
        /// <param name="helper"></param>
        private void GetUserLuckLog(HttpContext context, AshxHelper helper)
        {
            string openId = helper.GetParam("openid");
            string mobile = helper.GetParam("mobile");
            UserLuckLog luckLog = luckLogService.getUserLuck(openId);
            int isLuck = 0;//是否参与过抽奖
            if (luckLog != null && luckLog.Id > 0)
            {
                isLuck = 1;
            }
            helper.Add("isLuck", isLuck);
        }


        #endregion

        #region 申请抽奖
        private void ApplyUser(HttpContext context, AshxHelper helper)
        {
            int luckid = 0;
            string openId = helper.GetParam("openid");
            string unionId = helper.GetParam("unionId");
            string mobile = helper.GetParam("mobile");
            int rand = new Random().Next(1, 100);
            if (rand < 30)
            {
                System.Data.DataTable dt = luckLogService.getUserLuckCount();
                List<UserLuckLog> luckLogs = luckLogService.getUserLuckList();
                if (luckLogs.Count < 5 && Convert.ToInt32(dt.Rows[0][0].ToString()) < 100)
                {
                    luckid = 1;
                }
            }
            UserLuckLog model = new UserLuckLog();
            model.OpenId = openId;
            model.UserMoblie = mobile;
            model.LuckId = luckid;
            model.CreateTime = DateTime.Now;
            luckLogService.insert(model);
            helper.Add("luckid", luckid);
        }
        #endregion

        #region 提交中奖信息
        private void AddLuckUser(HttpContext context, AshxHelper helper)
        {
            string openid = helper.GetParam("openid");
            int luckid = helper.GetParamInt("luckid");
            if (luckid > 0)
            {
                UserLuckLog luckLog = luckLogService.getUserLuck(openid, luckid);
                if (luckLog != null && luckLog.Id > 0)
                {
                    string username = helper.GetParam("username");
                    string usertel = helper.GetParam("tel");
                    string prov = helper.GetParam("prov");
                    string city = helper.GetParam("city");
                    string address = helper.GetParam("address");
                    luckLog.UserName = username;
                    luckLog.UserMoblie = usertel;
                    luckLog.UserProv = prov;
                    luckLog.UserCity = city;
                    luckLog.UserAddress = address;
                    luckLogService.update(luckLog);
                    helper.Add("Code", "200");
                    helper.Add("CodeMsg", "");
                }
                else
                {
                    helper.Add("Code", "1002");
                    helper.Add("CodeMsg", "未找到中奖信息，请确认");
                }
            }
            else
            {
                helper.Add("Code", "1001");
                helper.Add("CodeMsg", "未找到中奖信息，请确认");
            }
        }
        #endregion
        public static string GetMD5_32(string str)
        {
            byte[] b = Encoding.Default.GetBytes(str);
            b = new MD5CryptoServiceProvider().ComputeHash(b);
            string ret = "";
            for (int i = 0; i < b.Length; i++)
                ret += b[i].ToString("x").PadLeft(2, '0');
            return ret;
        }
        /// <summary>  
        /// GET请求与获取结果  
        /// </summary>  
        public static string HttpGet(string Url, string postDataStr)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url + (postDataStr == "" ? "" : "?") + postDataStr);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.UTF8);
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();

            return retString;
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}