﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Text;
using System.Net;
using System.IO;
using System.Web.Script.Serialization;
using System.Web.Services;

namespace WebApplication1
{
    public class MyObject
    {
        public string success { get; set; }
        public List<string> ErrorMessage { get; set; }
    }
    public partial class login : System.Web.UI.Page
    { 
        string MYDBConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["MYDBConnection"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {

        }


        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            lbl_comments.Text = HttpUtility.HtmlEncode(tb_email.Text);
            lbl_comments.Text = HttpUtility.HtmlEncode(tb_pwd.Text);
            string pwd = tb_pwd.Text.ToString().Trim();
            string email = tb_email.Text.ToString().Trim();

            SHA512Managed hashing = new SHA512Managed();
            string dbHash = getDBHash(email);
            string dbSalt = getDBSalt(email);

            try
            {
                if (dbSalt != null && dbSalt.Length > 0 && dbHash != null && dbHash.Length > 0)
                {
                    string pwdWithSalt = pwd + dbSalt;
                    byte[] hashWithSalt = hashing.ComputeHash(Encoding.UTF8.GetBytes(pwdWithSalt));
                    string userHash = Convert.ToBase64String(hashWithSalt);

                    if (userHash.Equals(dbHash))
                    {
                        if (ValidateCaptcha())
                        {
                            Session["LoggedIn"] = tb_email.Text.Trim();
                            string guid = Guid.NewGuid().ToString();
                            Session["AuthToken"] = guid;

                            Response.Cookies.Add(new HttpCookie("AuthToken", guid));
                            Response.Redirect("Homepage.aspx?Info=" + HttpUtility.UrlEncode(tb_email.Text + tb_pwd.Text), false);
                        }
                        else
                        {
                            lblMsg.Text = "Userid or password is not valid. Please try again.";
                            Response.Redirect("Login.aspx", false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
            finally { }
            
        }
            
        protected string getDBHash(string email)
        {
            string h = null;

            SqlConnection connection = new SqlConnection(MYDBConnectionString);
            string sql = "select PasswordHash FROM Account WHERE Email=@USERID";
            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@USERID", email);

            try
            {
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader["PasswordHash"] != null)
                        {
                            if (reader["PasswordHash"] != DBNull.Value)
                            {
                                h = reader["PasswordHash"].ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());

            }
            finally { connection.Close(); }
            return h;
        }
        protected string getDBSalt(string email)
        {
            string s = null;

            SqlConnection connection = new SqlConnection(MYDBConnectionString);
            string sql = "select PASSWORDSALT FROM ACCOUNT WHERE Email=@USERID";
            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@USERID", email);

            try
            {
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader["PASSWORDSALT"] != null)
                        {
                            if (reader["PASSWORDSALT"] != DBNull.Value)
                            {
                                s = reader["PASSWORDSALT"].ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
            finally { connection.Close(); }
            return s;
        }

        public bool ValidateCaptcha()
        {
            bool result = true;
            string captchaResponse = Request.Form["g-recaptcha-response"];
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create
                (" https://www.google.com/recaptcha/api/siteverify?secret=6LdgV0gaAAAAAI0kVzxi6lUDISye2_PMSVaiJgS9 &response=" + captchaResponse);

            try
            {
                using (WebResponse wResponse = req.GetResponse())
                {
                    using (StreamReader readStream = new StreamReader(wResponse.GetResponseStream()))
                    {
                        string jsonResponse = readStream.ReadToEnd();
                        lbl_gScore.Text = jsonResponse.ToString();

                        JavaScriptSerializer js = new JavaScriptSerializer();

                        MyObject jsonObject = js.Deserialize<MyObject>(jsonResponse);

                        result = Convert.ToBoolean(jsonObject.success);
                    }
                }
                return result;
            }
            catch (WebException ex)
            {
                throw ex;
            }
        }
    }

}
