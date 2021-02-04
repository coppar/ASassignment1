using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace WebApplication1
{
    public partial class Registration : System.Web.UI.Page
    {

        string MYDBConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["MYDBConnection"].ConnectionString;
        static string finalHash;
        static string salt;
        byte[] Key;
        byte[] IV;
        protected void Page_Load(object sender, EventArgs e)
        {
            
        }
        private int checkPassword(string password)
        {
            int score = 0;

            //score 0: very weak!
            //if length of password is < 8 char:
            if (password.Length < 8)
            {
                return 1;
            }
            else
            {
                score = 1;
            }
            // Score 2: weak (only lowercase)
            if (Regex.IsMatch(password, "[a-z]"))
            {
                score++;
            }
            //Score 3: medium (contains uppercase letter)
            if (Regex.IsMatch(password, "[A-Z]"))
            {
                score++;
            }
            //Score 4: strong (contains numeral(s))
            if (Regex.IsMatch(password, "[0-9]"))
            {
                score++;
            }
            //Score 5: Excellent (contains special char)
            if (Regex.IsMatch(password, "[@^+$]"))
            {
                score++;
            }

            return score;
        }
        public void createAccount()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(MYDBConnectionString))
                {
                    string sqlQuery = string.Format("INSERT INTO Account VALUES(@Fname, @Lname, @CCinfo, @Email, @PasswordHash, @PasswordSalt, @Dob)");
                    using (SqlCommand cmd = new SqlCommand(sqlQuery))
                    {
                        using (SqlDataAdapter sda = new SqlDataAdapter())
                        {
                            cmd.CommandType = CommandType.Text;
                            cmd.Parameters.AddWithValue("@Fname", HttpUtility.HtmlEncode(tb_fname.Text.Trim()));
                            cmd.Parameters.AddWithValue("@Lname", HttpUtility.HtmlEncode(tb_lname.Text.Trim()));
                            cmd.Parameters.AddWithValue("@CCinfo", encryptData(tb_ccinfo.Text.Trim()));
                            cmd.Parameters.AddWithValue("@Email", tb_email.Text.Trim());
                            cmd.Parameters.AddWithValue("@PasswordHash", finalHash);
                            cmd.Parameters.AddWithValue("@PasswordSalt", salt);
                            cmd.Parameters.AddWithValue("@Dob", tb_dob.Text.Trim());
                            cmd.Connection = con;
                            con.Open();
                            cmd.ExecuteNonQuery();
                            con.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }

        protected byte [] encryptData(String data)
        {
            byte[] cipherText = null;
            try
            {
                RijndaelManaged cipher = new RijndaelManaged();
                cipher.IV = IV;
                cipher.Key = Key;
                ICryptoTransform encryptTransform = cipher.CreateEncryptor();
                byte[] plainText = Encoding.UTF8.GetBytes(data);
                cipherText = encryptTransform.TransformFinalBlock(plainText, 0, plainText.Length);

            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
            finally { }
            return cipherText;
        }

        protected void btn_pwdcheck_Click(object sender, EventArgs e)
        {
            
            //implement codes for button event
            //Extract data from textbox
            int scores = checkPassword(tb_pwd.Text);
            string status = "";
            switch (scores)
            {
                case 1:
                    status = "Very Weak";
                    break;
                case 2:
                    status = "Weak";
                    break;
                case 3:
                    status = "Medium";
                    break;
                case 4:
                    status = "Strong";
                    break;
                case 5:
                    status = "Excellent!";
                    break;
                default:
                    break;
            }
            lbl_pwdcheck.Text = "Status: " + status;
            if (scores < 4)
            {
                lbl_pwdcheck.ForeColor = Color.Red;
                return;
            }
            lbl_pwdcheck.ForeColor = Color.Green;

        }

        protected void btn_register_Click(object sender, EventArgs e)
        {
            
        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        { 
            if (tb_fname.Text == "")
            {
                lblMsg.Text += "First name is required!" + "<br/>"; ;
                lblMsg.ForeColor = System.Drawing.Color.Red;
            }
            if (tb_lname.Text == "")
            {
                lblMsg.Text += "Last name is required!" + "<br/>"; ;
                lblMsg.ForeColor = System.Drawing.Color.Red;
            }
            if (tb_ccinfo.Text == "")
            {
                lblMsg.Text += "Credit card info is required!" + "<br/>";
                lblMsg.ForeColor = System.Drawing.Color.Red;
            }
            if (tb_email.Text == "")
            {
                lblMsg.Text += "Email is required!" + "<br/>"; ;
                lblMsg.ForeColor = System.Drawing.Color.Red;
            }
            if (tb_dob.Text == "")
            {
                lblMsg.Text += "Date of birth is required!" + "<br/>"; ;
                lblMsg.ForeColor = System.Drawing.Color.Red;
            }
            else
            {
                string pwd = tb_pwd.Text.ToString().Trim();
                //Generate random "salt"
                RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
                byte[] saltByte = new byte[8];
                // Fills array of bytes with a cryptographically strong sequence of random values
                rng.GetBytes(saltByte);
                salt = Convert.ToBase64String(saltByte);
                SHA512Managed hashing = new SHA512Managed();
                string pwdWithSalt = pwd + salt;
                byte[] plainHash = hashing.ComputeHash(Encoding.UTF8.GetBytes(pwd));
                byte[] hashWithSalt = hashing.ComputeHash(Encoding.UTF8.GetBytes(pwdWithSalt));
                finalHash = Convert.ToBase64String(hashWithSalt);

                RijndaelManaged cipher = new RijndaelManaged();
                cipher.GenerateKey();
                Key = cipher.Key;
                IV = cipher.IV;

                createAccount();
                Response.Redirect("login.aspx?Info=" + HttpUtility.UrlEncode(tb_fname.Text));
            }
        }
    }
}